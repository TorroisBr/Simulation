# CORE-F source-cut analysis evidence

This is a documentation/evidence snapshot, not a supported Tools capability,
production source extraction, runtime validation or full cross-assembly closure.
The [Master handoff](../../architecture/CORE_F_SHARED_RUNTIME_SOURCE_MASTER_HANDOFF.md)
fixes the normative scope, readiness and remaining work.

- `source-inventory.csv`:329 Unity application assembly source files, canonical
  Git blobs, SCC/reachability and mechanical design dispositions.
- `source-edges.csv`:2,990 deduplicated directed file-pair dependency edges;
  partial co-declaration links are distinguished from semantic references.
- `unity-symbol-edges.csv`:480 application-source-to-UnityEngine symbol links.
- `core-symbol-edges.csv`:application-source-to-existing-Core metadata symbol links.
  These are API references, not source nodes or complete method-level dependencies.
- `core-build-inputs.csv`:explicit current Core project source/link/blob inventory.
- `metadata-references.csv`:332 resolved compilation metadata identities/hashes.
- `analysis-manifest.txt` and `preprocessor-symbols.txt`:Unity/compiler/SDK/options,
  response-file digest, warnings and the sole omitted synthetic parser reference.
- `audit-source/`:the original analysis source and project, saved with `.txt` suffixes
  so this docs-only packet adds no compiled project or supported command surface.

Source snapshot: analyzed application source matches Values promotion
`c69fcf471be7c73c3b63b539d0ae5e659cb9e029` and current Core
`fff5cca0297ce5af346a264b7fa2e2e37be587b0`. Every source blob is explicit.
Raw compiler response text is withheld because it contains local paths; its digest
is not represented as a portable response body. To reproduce locally, restore the
recorded audit-source filenames in a disposable analysis directory, use the pinned
SDK and actual Unity compiler response/cache for the matching source/build, and run:

```text
dotnet run --project SourceClosureDesign.csproj -- <response-file> <source-root> <Library-root> <SDK-Roslyn-directory> <output-directory>
```

The archived generator reads existing source and metadata; it does not import
Unity or advance a world. Its graph/reference/manifest outputs are direct outputs.
Two published companion files have separately recorded derivation:

- The archived source writes literal backslash-n separators into its symbol file.
  The published `preprocessor-symbols.txt` normalizes those separators into actual
  newlines. Compare the137 symbols to the manifest parse-symbol list; do not claim
  its raw bytes are the unmodified analyzer output.
- `core-build-inputs.csv` is independently derived from the explicit ordered
  Compile items of `src/Simulation.Core/Simulation.Core.csproj`, not emitted by
  the archived analyzer. Resolve each Include relative to that project, retain its
  Link and use `git hash-object -- <repository-relative-source>` for the blob.
  The independent review checked all28 rows against current canonical.

For reproducibility, apply these two companion steps after the archived run;
this is evidence preparation, not a supported build/tool capability. A minimal
reference derivation run from the matching source checkout is:

```python
import csv, io, posixpath, subprocess
from pathlib import Path
import xml.etree.ElementTree as ET
output = Path("<analysis-output-directory>")
symbols = output / "preprocessor-symbols.txt"
symbols.write_text(symbols.read_text(encoding="utf-8").replace("\\n", "\n"),
                   encoding="utf-8", newline="\n")
project = Path("src/Simulation.Core/Simulation.Core.csproj")
with (output / "core-build-inputs.csv").open("w", encoding="utf-8", newline="") as stream:
    writer = csv.writer(stream, quoting=csv.QUOTE_ALL, lineterminator="\n")
    stream.write("source_path,link_path,git_blob\n")
    for item in ET.parse(project).findall(".//Compile"):
        source = posixpath.normpath(str(project.parent / item.attrib["Include"]).replace("\\", "/"))
        blob = subprocess.check_output(["git", "hash-object", "--", source], text=True).strip()
        writer.writerow([source, item.attrib.get("Link", ""), blob])
```

Compare all recorded reference hashes, parse symbols, source blobs and graph outputs. Rebuilt assemblies have truthful new build identity;
identical source alone is not a claim of identical metadata. The published CSVs
and manifest contain no private directory paths. Error output from a local rerun
may contain local diagnostics and must be reviewed before publication.

No source-only graph proves profile admission, reflection reachability, live owner
binding, complete writer/epoch coverage, call ordering, parity or persistence
continuation. Those retain their separate canonical tests and reviewed matrices.
