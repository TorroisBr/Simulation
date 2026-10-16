using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

const string ExpectedCompilerResponseSha256 = "1f490838bbb8443791f7ccd5eb856e98ae9c6993ca961517f345fbd37747c555";
const string PinnedSourceCommit = "ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d";
const string PinnedSourceTree = "e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5";

if (args.Length != 6)
    throw new ArgumentException("Expected: <compiler-response> <repository-root> <library-root> <sdk-root> <source-inventory.csv> <output-directory>.");

string responsePath = Path.GetFullPath(args[0]);
string repositoryRoot = Path.GetFullPath(args[1]);
string libraryRoot = Path.GetFullPath(args[2]);
string sdkRoot = Path.GetFullPath(args[3]);
string sourceInventoryPath = Path.GetFullPath(args[4]);
string outputDirectory = Path.GetFullPath(args[5]);
Directory.CreateDirectory(outputDirectory);

string responseHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(responsePath))).ToLowerInvariant();
if (!StringComparer.Ordinal.Equals(responseHash, ExpectedCompilerResponseSha256))
    throw new InvalidOperationException("Compiler response identity differs from the pinned closure snapshot.");

static List<string> ParseCsv(string line)
{
    var fields = new List<string>();
    var value = new StringBuilder();
    bool quoted = false;
    for (int i = 0; i < line.Length; i++)
    {
        char c = line[i];
        if (c == '"')
        {
            if (quoted && i + 1 < line.Length && line[i + 1] == '"')
            {
                value.Append('"');
                i++;
            }
            else
                quoted = !quoted;
        }
        else if (c == ',' && !quoted)
        {
            fields.Add(value.ToString());
            value.Clear();
        }
        else
            value.Append(c);
    }
    fields.Add(value.ToString());
    return fields;
}

static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
static string Rel(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

var inventoryLines = File.ReadAllLines(sourceInventoryPath);
var inventoryHeader = ParseCsv(inventoryLines[0]);
int Column(string name) => inventoryHeader.IndexOf(name) is int i && i >= 0 ? i : throw new InvalidDataException("Missing source-inventory column: " + name);
int sourcePathColumn = Column("source_path");
int sourceBlobColumn = Column("git_blob");
int sourceSccColumn = Column("scc_id");
int runtimeColumn = Column("runtime_closure");
int restoreColumn = Column("restore_closure");
var inventory = new Dictionary<string, (string Blob, string Scc, bool Runtime, bool Restore)>(StringComparer.Ordinal);
foreach (string line in inventoryLines.Skip(1).Where(line => line.Length > 0))
{
    List<string> values = ParseCsv(line);
    inventory.Add(values[sourcePathColumn], (
        values[sourceBlobColumn],
        values[sourceSccColumn],
        Boolean.Parse(values[runtimeColumn]),
        Boolean.Parse(values[restoreColumn])));
}

string Resolve(string value)
{
    if (Path.IsPathRooted(value)) return value;
    string normalized = value.Replace('/', '\\');
    if (normalized.StartsWith("Library\\", StringComparison.OrdinalIgnoreCase))
        return Path.GetFullPath(Path.Combine(libraryRoot, normalized["Library\\".Length..]));
    return Path.GetFullPath(Path.Combine(repositoryRoot, normalized));
}

var parsed = CSharpCommandLineParser.Default.Parse(new[] { "@" + responsePath }, repositoryRoot, sdkRoot, null);
if (parsed.Errors.Length != 0)
    throw new InvalidOperationException("Pinned Unity compiler response has parse errors: " + parsed.Errors.Length.ToString(CultureInfo.InvariantCulture));

string[] sourcePaths = parsed.SourceFiles.Select(source => Resolve(source.Path)).ToArray();
if (sourcePaths.Any(path => !File.Exists(path)))
    throw new FileNotFoundException("A pinned Unity compiler source file is unavailable.");
var relativeSources = sourcePaths.Select(path => Rel(repositoryRoot, path)).ToHashSet(StringComparer.Ordinal);
if (!relativeSources.SetEquals(inventory.Keys))
    throw new InvalidOperationException("Current Unity compiler source set differs from the pinned source inventory.");

var currentSourceBlobs = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (string relativePath in relativeSources)
{
    string actualBlob = GitOutput(repositoryRoot, "hash-object", "--", relativePath);
    if (!StringComparer.Ordinal.Equals(actualBlob, inventory[relativePath].Blob))
        throw new InvalidOperationException("Current compiler source bytes differ from the pinned inventory: " + relativePath);
    currentSourceBlobs.Add(relativePath, actualBlob);
}
Console.WriteLine($"pinned_source_blobs={currentSourceBlobs.Count} compiler_response=PASS");

var metadataReferences = new List<MetadataReference>();
var metadataReferencePaths = new List<(string Path, MetadataReferenceProperties Properties)>();
foreach (CommandLineReference reference in parsed.MetadataReferences)
{
    string path = Resolve(reference.Reference);
    if (File.Exists(path))
    {
        metadataReferences.Add(MetadataReference.CreateFromFile(path, reference.Properties));
        metadataReferencePaths.Add((path, reference.Properties));
    }
}

var trees = sourcePaths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parsed.ParseOptions, path)).ToArray();
var compilation = CSharpCompilation.Create(parsed.CompilationName, trees, metadataReferences, parsed.CompilationOptions);
Console.WriteLine($"semantic_model_ready sources={trees.Length} metadata_references={metadataReferences.Count}");

// Resolve metadata-bound Simulation.Core symbols back to the exact source declarations
// in the promoted Core project. The retained call-site map is historical evidence;
// this source-backed projection is generated separately and fails closed if a Core
// reference cannot be joined to one of the 53 pinned Core compile inputs.
var coreInputLines = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(sourceInventoryPath)!, "core-build-inputs.csv"));
var coreInputHeader = ParseCsv(coreInputLines[0]);
int corePathColumn = coreInputHeader.IndexOf("source_path");
int coreBlobColumn = coreInputHeader.IndexOf("git_blob");
if (corePathColumn < 0 || coreBlobColumn < 0)
    throw new InvalidDataException("Core compile-input inventory is missing required columns.");
var coreSourcePaths = new List<string>();
var coreSourceBlobs = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (string line in coreInputLines.Skip(1).Where(line => line.Length > 0))
{
    List<string> values = ParseCsv(line);
    string relativePath = values[corePathColumn];
    string expectedBlob = values[coreBlobColumn];
    string path = Resolve(relativePath);
    if (!File.Exists(path))
        throw new FileNotFoundException("A pinned Core compile input is unavailable.");
    string actualBlob = GitOutput(repositoryRoot, "hash-object", "--", relativePath);
    if (!StringComparer.Ordinal.Equals(actualBlob, expectedBlob))
        throw new InvalidOperationException("Current Core compile input differs from the pinned inventory: " + relativePath);
    coreSourcePaths.Add(path);
    coreSourceBlobs.Add(relativePath, actualBlob);
}
if (coreSourcePaths.Count != 53)
    throw new InvalidDataException("The selected Core source inventory must contain exactly 53 compile inputs.");

string[] coreAssemblyReferencePaths = metadataReferencePaths
    .Where(reference => !StringComparer.OrdinalIgnoreCase.Equals(
        Path.GetFileNameWithoutExtension(reference.Path), "Simulation.Core"))
    .Select(reference => reference.Path)
    .ToArray();
var coreReferences = coreAssemblyReferencePaths
    .Select(path => MetadataReference.CreateFromFile(path))
    .ToArray();
var coreTrees = coreSourcePaths
    .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parsed.ParseOptions, path))
    .ToArray();
var coreCompilation = CSharpCompilation.Create("Simulation.Core", coreTrees, coreReferences, parsed.CompilationOptions);
var coreErrors = coreCompilation.GetDiagnostics()
    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
    .ToArray();
if (coreErrors.Length > 0)
    throw new InvalidOperationException("Pinned Core source compilation failed: " + string.Join(" | ", coreErrors.Take(8).Select(diagnostic => diagnostic.ToString())));

string DefinitionKey(ISymbol symbol)
{
    ISymbol definition = symbol.OriginalDefinition;
    return definition.Kind + "\0" + definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}

var coreDeclarations = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
var coreTypeDeclarations = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
for (int treeIndex = 0; treeIndex < coreTrees.Length; treeIndex++)
{
    SemanticModel coreModel = coreCompilation.GetSemanticModel(coreTrees[treeIndex]);
    foreach (SyntaxNode node in coreTrees[treeIndex].GetRoot().DescendantNodesAndSelf())
    {
        ISymbol? declared = coreModel.GetDeclaredSymbol(node);
        if (declared is null || !SymbolEqualityComparer.Default.Equals(declared.ContainingAssembly, coreCompilation.Assembly))
            continue;
        string key = DefinitionKey(declared);
        if (!coreDeclarations.TryGetValue(key, out SortedSet<string>? sites))
        {
            sites = new SortedSet<string>(StringComparer.Ordinal);
            coreDeclarations.Add(key, sites);
        }
        foreach (SyntaxReference declaration in declared.DeclaringSyntaxReferences)
        {
            string relativePath = Rel(repositoryRoot, declaration.SyntaxTree.FilePath);
            int line = declaration.SyntaxTree.GetLineSpan(declaration.Span).StartLinePosition.Line + 1;
            sites.Add(relativePath + ":" + line.ToString(CultureInfo.InvariantCulture));
        }
        if (declared is INamedTypeSymbol namedType)
        {
            string typeKey = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!coreTypeDeclarations.TryGetValue(typeKey, out SortedSet<string>? typeSites))
            {
                typeSites = new SortedSet<string>(StringComparer.Ordinal);
                coreTypeDeclarations.Add(typeKey, typeSites);
            }
            foreach (SyntaxReference declaration in namedType.DeclaringSyntaxReferences)
            {
                string relativePath = Rel(repositoryRoot, declaration.SyntaxTree.FilePath);
                int line = declaration.SyntaxTree.GetLineSpan(declaration.Span).StartLinePosition.Line + 1;
                typeSites.Add(relativePath + ":" + line.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
Console.WriteLine($"core_source_binding_ready sources={coreTrees.Length} metadata_references={coreReferences.Length} declarations={coreDeclarations.Count} compile_errors=0");

var rows = new Dictionary<string, string[]>(StringComparer.Ordinal);
var corePairs = new HashSet<string>(StringComparer.Ordinal);
var unityPairs = new HashSet<string>(StringComparer.Ordinal);
var unresolvedCoreSymbols = new SortedDictionary<string, string>(StringComparer.Ordinal);

void AddReference(int treeIndex, SemanticModel model, SyntaxNode node, ISymbol? symbol, string referenceKind)
{
    if (symbol is null || symbol is INamespaceSymbol) return;
    string assembly = symbol.ContainingAssembly?.Name ?? string.Empty;
    string targetNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
    bool isCore = StringComparer.Ordinal.Equals(assembly, "Simulation.Core");
    bool isUnity = targetNamespace.StartsWith("UnityEngine", StringComparison.Ordinal);
    if (!isCore && !isUnity) return;

    string sourcePath = Rel(repositoryRoot, trees[treeIndex].FilePath);
    var closure = inventory[sourcePath];
    if (!closure.Runtime && !closure.Restore) return;

    ISymbol? caller = model.GetEnclosingSymbol(node.SpanStart);
    string display = symbol.ToDisplayString();
    string callerDisplay = caller?.ToDisplayString() ?? string.Empty;
    string key = string.Join("\0", sourcePath, node.SpanStart.ToString(CultureInfo.InvariantCulture),
        node.Span.Length.ToString(CultureInfo.InvariantCulture), assembly, display, referenceKind);
    if (rows.ContainsKey(key)) return;

    FileLinePositionSpan lineSpan = node.GetLocation().GetLineSpan();
    string declarationPath = string.Empty;
    string declarationLine = string.Empty;
    string declarationSites = string.Empty;
    string declarationBindingKind = string.Empty;
    string containingSymbol = symbol.ContainingType?.OriginalDefinition
        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty;
    SyntaxReference? declaration = symbol.DeclaringSyntaxReferences.FirstOrDefault();
    if (declaration is not null && declaration.SyntaxTree.FilePath.Length > 0)
    {
        declarationPath = Rel(repositoryRoot, declaration.SyntaxTree.FilePath);
        declarationLine = (declaration.SyntaxTree.GetLineSpan(declaration.Span).StartLinePosition.Line + 1)
            .ToString(CultureInfo.InvariantCulture);
    }
    if (isCore)
    {
        string definitionKey = DefinitionKey(symbol);
        if (coreDeclarations.TryGetValue(definitionKey, out SortedSet<string>? sites) && sites.Count > 0)
        {
            declarationSites = string.Join("|", sites);
            declarationBindingKind = "member-declaration";
            int separator = sites.Min!.LastIndexOf(':');
            declarationPath = sites.Min[..separator];
            declarationLine = sites.Min[(separator + 1)..];
        }
        else if (symbol is IMethodSymbol implicitConstructor
            && implicitConstructor.MethodKind == MethodKind.Constructor
            && implicitConstructor.DeclaringSyntaxReferences.Length == 0
            && implicitConstructor.ContainingType is not null
            && coreTypeDeclarations.TryGetValue(
                implicitConstructor.ContainingType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                out SortedSet<string>? typeSites)
            && typeSites.Count > 0)
        {
            declarationSites = string.Join("|", typeSites);
            declarationBindingKind = "implicit-constructor-from-containing-type";
            int separator = typeSites.Min!.LastIndexOf(':');
            declarationPath = typeSites.Min[..separator];
            declarationLine = typeSites.Min[(separator + 1)..];
        }
        else
        {
            unresolvedCoreSymbols.TryAdd(definitionKey, string.Join(" | ",
                "kind=" + symbol.Kind,
                "display=" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                "metadata=" + symbol.MetadataName,
                "container=" + (symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty),
                "assembly=" + assembly,
                "caller=" + callerDisplay,
                "site=" + sourcePath + ":" + (lineSpan.StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture)));
        }
    }

    rows.Add(key, new[]
    {
        isCore ? "Simulation.Core" : "UnityEngine",
        sourcePath,
        currentSourceBlobs[sourcePath],
        closure.Scc,
        closure.Runtime.ToString().ToLowerInvariant(),
        closure.Restore.ToString().ToLowerInvariant(),
        (lineSpan.StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture),
        node.Span.Start.ToString(CultureInfo.InvariantCulture),
        node.Kind().ToString(),
        caller?.ContainingType?.ToDisplayString() ?? string.Empty,
        callerDisplay,
        caller?.DeclaredAccessibility.ToString() ?? string.Empty,
        symbol.Kind.ToString(),
        display,
        symbol.DeclaredAccessibility.ToString(),
        containingSymbol,
        assembly,
        targetNamespace,
        declarationPath,
        declarationLine,
        declarationSites,
        declarationBindingKind,
        referenceKind
    });

    if (isCore)
        corePairs.Add(sourcePath + "\0" + display);
    else
        unityPairs.Add(sourcePath + "\0" + targetNamespace + "." + symbol.Name);
}

for (int treeIndex = 0; treeIndex < trees.Length; treeIndex++)
{
    SemanticModel model = compilation.GetSemanticModel(trees[treeIndex]);
    foreach (SyntaxNode node in trees[treeIndex].GetRoot().DescendantNodesAndSelf())
    {
        AddReference(treeIndex, model, node, model.GetSymbolInfo(node).Symbol, "bound-symbol");
        if (node is ExpressionSyntax or TypeSyntax)
        {
            TypeInfo type = model.GetTypeInfo(node);
            AddReference(treeIndex, model, node, type.Type, "expression-type");
            AddReference(treeIndex, model, node, type.ConvertedType, "converted-type");
        }
    }
}
Console.WriteLine($"semantic_projection_complete unique_callsite_rows={rows.Count}");

if (unresolvedCoreSymbols.Count > 0)
    throw new InvalidOperationException("Some Simulation.Core call-site symbols do not bind to pinned source declarations: " +
        string.Join(" || ", unresolvedCoreSymbols.Take(20).Select(pair => pair.Key.Replace("\0", " ") + " => " + pair.Value)));

string oldCorePairsPath = Path.Combine(Path.GetDirectoryName(sourceInventoryPath)!, "selected-core-member-access.csv");
var oldCorePairs = File.ReadAllLines(oldCorePairsPath).Skip(1).Where(line => line.Length > 0)
    .Select(ParseCsv).Select(values => values[0] + "\0" + values[6]).ToHashSet(StringComparer.Ordinal);
string oldUnityPairsPath = Path.Combine(Path.GetDirectoryName(sourceInventoryPath)!, "selected-unity-engine-access.csv");
var oldUnityPairs = File.ReadAllLines(oldUnityPairsPath).Skip(1).Where(line => line.Length > 0)
    .Select(ParseCsv).Select(values => values[0] + "\0" + values[5]).ToHashSet(StringComparer.Ordinal);
if (!corePairs.SetEquals(oldCorePairs))
    throw new InvalidOperationException("Method-level map Core symbol projection differs from the retained exact symbol inventory.");
if (!unityPairs.SetEquals(oldUnityPairs))
    throw new InvalidOperationException("Method-level map Unity symbol projection differs from the retained exact symbol inventory.");

string[] columns =
{
    "target_family", "source_path", "source_blob", "source_scc", "runtime_closure", "restore_closure",
    "source_line", "source_span_start", "source_node_kind", "caller_type", "caller_member",
    "caller_accessibility", "target_symbol_kind", "target_symbol", "target_accessibility",
    "target_containing_symbol", "target_assembly", "target_namespace", "declaration_path",
    "declaration_line", "declaration_sites", "declaration_binding_kind", "reference_kind"
};
string csvPath = Path.Combine(outputDirectory, "selected-member-access-sites.csv");
using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(false)))
{
    writer.WriteLine(string.Join(",", columns.Select(Csv)));
    foreach (string[] values in rows.Values.OrderBy(row => row[1], StringComparer.Ordinal)
        .ThenBy(row => Int32.Parse(row[6], CultureInfo.InvariantCulture))
        .ThenBy(row => Int32.Parse(row[7], CultureInfo.InvariantCulture))
        .ThenBy(row => row[13], StringComparer.Ordinal))
        writer.WriteLine(string.Join(",", values.Select(Csv)));
}

string relativeResponse = Rel(libraryRoot, responsePath);
byte[] csvBytes = File.ReadAllBytes(csvPath);
string csvHash = Convert.ToHexString(SHA256.HashData(csvBytes)).ToLowerInvariant();
string compressedPath = Path.Combine(outputDirectory, "selected-member-access-sites.csv.gz");
using (var compressedFile = File.Create(compressedPath))
using (var gzip = new GZipStream(compressedFile, CompressionLevel.Optimal))
    gzip.Write(csvBytes);
string compressedHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(compressedPath))).ToLowerInvariant();
long compressedBytes = new FileInfo(compressedPath).Length;
File.Delete(csvPath);
int runtimeSources = inventory.Values.Count(value => value.Runtime);
int restoreSources = inventory.Values.Count(value => value.Restore);
int coreRows = rows.Values.Count(row => row[0] == "Simulation.Core");
int unityRows = rows.Values.Count(row => row[0] == "UnityEngine");
string manifestPath = Path.Combine(outputDirectory, "selected-member-access-sites.manifest.txt");
File.WriteAllText(manifestPath,
    "CORE-F selected runtime/restore member-access call-site inventory\n" +
    $"Source commit: {PinnedSourceCommit}; source tree: {PinnedSourceTree}.\n" +
    $"Unity compiler response: {relativeResponse}; SHA-256 {responseHash}; {new FileInfo(responsePath).Length} bytes.\n" +
    $"Unity editor: {File.ReadAllText(Path.Combine(repositoryRoot, "ProjectSettings", "ProjectVersion.txt")).Split('\n')[0].Replace("m_EditorVersion: ", string.Empty).Trim()}.\n" +
    $"Roslyn semantic projection: {trees.Length} Unity source files; {metadataReferences.Count} resolved metadata references; the editor's official compile/test result is the Unity compilation authority.\n" +
    $"Core source declaration binding: {coreTrees.Length} exact compile inputs; {coreReferences.Length} metadata references excluding the prior Simulation.Core binary; compile errors 0; {coreDeclarations.Count} declared source symbols; all selected Simulation.Core call sites must resolve.\n" +
    $"Closure inventory: {runtimeSources} runtime sources; {restoreSources} restore sources.\n" +
    $"Exact prior symbol-pair reconciliation: Simulation.Core {corePairs.Count}/{oldCorePairs.Count}; UnityEngine {unityPairs.Count}/{oldUnityPairs.Count}.\n" +
    $"Call-site rows: Simulation.Core {coreRows}; UnityEngine {unityRows}; total {rows.Count}.\n" +
    $"CSV.GZ: selected-member-access-sites.csv.gz; SHA-256 {compressedHash}; {compressedBytes} bytes.\n" +
    $"Uncompressed CSV: SHA-256 {csvHash}; {csvBytes.Length} bytes.\n" +
    "Raw compiler response and machine paths are not emitted. Call-site rows identify the concrete caller member, access level, symbol, target containing type, target assembly, exact source declaration sites for Simulation.Core, and runtime/restore closure. Implicit Core constructors bind to their declaring type location and are labeled; UnityEngine metadata declarations remain external by design. These are evidence for the approved same-assembly/Unity-adapter cut, not a new public API.\n",
    new UTF8Encoding(false));

Console.WriteLine($"core_pairs={corePairs.Count} unity_pairs={unityPairs.Count} core_callsites={coreRows} unity_callsites={unityRows} total_callsites={rows.Count} unresolved_core_declarations={unresolvedCoreSymbols.Count} csv_sha256={csvHash} gzip_sha256={compressedHash} gzip_bytes={compressedBytes}");

static string GitOutput(string workingDirectory, params string[] arguments)
{
    var start = new ProcessStartInfo("git") { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    foreach (string argument in arguments) start.ArgumentList.Add(argument);
    using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Git for source identity verification.");
    string output = process.StandardOutput.ReadToEnd().Trim();
    process.WaitForExit();
    if (process.ExitCode != 0) throw new InvalidOperationException("Git source identity verification failed.");
    return output;
}
