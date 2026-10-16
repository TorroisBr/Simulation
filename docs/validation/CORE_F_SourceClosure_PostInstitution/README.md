# CORE-F post-owner source closure snapshot

This is a refreshed, privacy-reviewed Unity/Core compile-source graph for the
promoted CORE-F owner migrations. It is planning and access evidence only. It
does not prove live owner binding, complete writer/epoch coverage, profile
admission, method-level reachability, restore behavior, or .NET host readiness.

## Exact inputs

- Promoted Core canonical and State: `2764b50b49c0773328ebcc56b2c668a2c609b7ff`.
- Production/source identity used by the Unity compiler response: commit
  `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`, tree
  `e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`.
- Architecture canonical: `797eb02f06fc5ca759a7e47283589d97a5efe73d`.
- Unity: `6000.3.9f1`; .NET SDK/Roslyn: `9.0.315`.
- Unity compiler response: SHA-256
  `1f490838bbb8443791f7ccd5eb856e98ae9c6993ca961517f345fbd37747c555`,
  60,771 bytes. The raw response is not published because it contains local
  filesystem paths.
- Current `Simulation.Core.csproj`: SHA-256
  `5dda7dd39d9c13fbdbb2d0a318c8cb3e5020802200b497343a8cbbae86e8ee26`.
- Archived graph analyzer source: SHA-256
  `2e9fc5c6a4409ad7c5c8f41721efbda646d63d33f6d8f7bc6780ad88be78fe57`.

All 315 Unity source blob identities and all 53 explicit Core `Compile` input
blob identities match the production/source commit. The Core input inventory is
derived from the ordered explicit project `Compile` items, resolving each path
relative to `src/Simulation.Core/Simulation.Core.csproj` and retaining its Link.

## Output corrections and privacy

The archived analyzer emits the source/edge/metadata CSVs directly. Its console
result and the 315-row source inventory report 315 source files, but the
generated summary contains one stale hard-coded `329` file-count literal. The
published `analysis-manifest.txt` corrects only that count to 315. The raw
generated manifest SHA-256 is
`ca0c0170ddc0857a56321deaf66c8810e0b7c6194ff46fcca2c0f40e7bd8aff8` and is
retained in the task's private temporary evidence. The analyzer also writes
preprocessor separators as literal `\\n`; `preprocessor-symbols.txt` normalizes
those separators to actual newlines using the derivation documented in the
prior `CORE_F_SourceClosure/README.md`. `core-build-inputs.csv` is independently
derived from the explicit project compile list, as before.

The package contains repo-relative source paths, safe metadata labels, source
blob identities and content hashes. A scan for workstation paths, home paths,
and credential-like strings passed. The raw compiler response, Unity Library,
private logs and temporary compiler outputs are not included.

## Files

- `source-inventory.csv`: all current Assembly-CSharp source blobs, SCCs,
  runtime/restore reachability and Unity symbol references.
- `source-edges.csv`: directed source declaration-reference edges.
- `source-migration-crosswalk.csv`: exact 15-file source path migrations from
  the earlier Unity application graph into the current shared Core project,
  with prior/current blob identities and Git rename similarity.
- `unity-symbol-edges.csv` and `core-symbol-edges.csv`: separate external API
  reference inventories; these are not source nodes or method-level call maps.
- `core-build-inputs.csv`: all 53 explicit shared-Core source inputs and Links.
- `metadata-references.csv`: resolved compiler references by safe assembly
  label, hash and byte length.
- `preprocessor-symbols.txt` and `analysis-manifest.txt`: normalized parse
  context and graph summary.
