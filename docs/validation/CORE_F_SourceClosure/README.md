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
- `audit-source/`:the exact analysis source and project, saved with `.txt` suffixes
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

The generator reads existing source and metadata; it does not import Unity or
advance a world. Compare all recorded reference hashes, parse symbols, source
blobs and graph outputs. Rebuilt assemblies have truthful new build identity;
identical source alone is not a claim of identical metadata. The published CSVs
and manifest contain no private directory paths. Error output from a local rerun
may contain local diagnostics and must be reviewed before publication.

No source-only graph proves profile admission, reflection reachability, live owner
binding, complete writer/epoch coverage, call ordering, parity or persistence
continuation. Those retain their separate canonical tests and reviewed matrices.
