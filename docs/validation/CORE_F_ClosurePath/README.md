# Portable CORE-F source closure evidence (0094)

These files are path-portable source graph inventories for the exact production source commit `0094febf7f97333d0653eaf9688fe1d24d73327f`. Paths in CSVs are repository-relative; Git blob IDs bind each source input. No Unity response file, machine path, raw compiler reference path, DLL, or Unity cache is included.

Compiler provenance: Unity 6000.3.9f1, C# 9, SDK/Roslyn 9.0.315, response input SHA-256 `cf4be5e167041136a8f5b12bd7035397de0ec636e1f9a973cd77d20043076b35` (61,634 bytes; 329 source entries; 332 metadata refs). The cached Core metadata was stale for the P18D provider; the exact 0094 Core DLL was built under SDK 9.0.315 and substituted only for semantic binding, hash `36ce257e40439184fd742f4abf7f31cd8f58afeb17d6cfa68b15064c9e80495c`. This is analytic source-graph binding, not Unity-run or migration evidence.

Bound result: 329 source files, 2,990 directed semantic source-file edges, 50 partial co-declaration links, 128 SCCs (largest 191 files with 1,324 internal edges and 43 outgoing SCCs), 235 runtime closure files, 303 restore closure files, 26 outside both, 477 Unity symbol edges, 1,753 Core symbol edges, zero compiler errors and eight warnings (CS0108/CS0618). The graph is declaration-reference reachability, not method, mutation, restore, or behavior proof.

`source-inventory.csv`, `source-edges.csv`, `core-symbol-edges.csv`, and `unity-symbol-edges.csv` are copied unchanged from the bound graph audit. `aggregate-population-files.csv` selects the proposed seven-file package rows. `metadata-references.csv` contains only safe assembly labels, hashes and byte counts; no resolved machine paths.

Published safe-label reference inventory hash: SHA-256 DBEEC3FEF4BD36709D188C4C1076BF50EF13571EBEEC16739FBBEE5E57C052EE; 332 safe-label/hash rows plus header.


Audit-source snapshot: the existing Roslyn declaration auditor from the production CORE_F_SourceClosure evidence was adapted only for exact source/reference binding. This invocation's Program.cs SHA-256 is7c6c9028cba609ea89e14ff094eae41cd144ed8cac91580957fbe5d6e6f05bc5. Private reference/cache inputs remain diagnostic provenance; the published source blobs and edge tables are the reviewable planning evidence. Final migration still requires actual Unity/SDK compilation and semantic validation, not this analytic substitution.

The frozen auditor source/project are retained as documentation under audit-source/ (.txt), with parse/compiler options and symbols in analysis-manifest.txt and preprocessor-symbols.txt. They are a diagnostic snapshot, not a production tool or new executable capability. The assembly/reference bytes remain supplied by the pinned SDK/Unity installation; nothing requires publishing Unity DLLs or raw workstation response paths.

The retained auditor text normalizes terminal whitespace/Git line endings; the invocation hash above describes the original diagnostic input, not the normalized text bytes. No algorithm change or rerun is claimed by this retention step.
