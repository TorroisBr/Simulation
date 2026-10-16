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
- `owner-current-source-anchors.csv`: current source line/blob anchors for all
  89 Daily-v1 owner families.
- `OWNER_CURRENT_JOIN_CORRECTIONS.md`: additive correction of stale/incorrect
  family, codec, and test anchors; historical validation records are retained.
- `POST_INSTITUTION_OWNER_MEMBER_ACCESS_JOIN.md`: current live-owner identity
  closure, executable join checks, and exact remaining source/access blockers.

## Current selected-profile revalidation

The source graph above remains the preserved historical compiler snapshot. A
later bounded test-only candidate, `cb31d4d6f777a78af25670e3e6d43f01578b8809`,
adds executable initial cardinality/revision-zero assertions for the seven
Required per-NPC site/local-topology/Adventure Intel Knowledge views in the
selected Daily-v1 admission profile. It does not reclassify those rows as
`ExplicitlyEmpty` or assert zero for restored/later runtime states. The refreshed
owner join and current source/blob references are recorded in
[`POST_INSTITUTION_OWNER_MEMBER_ACCESS_JOIN.md`](POST_INSTITUTION_OWNER_MEMBER_ACCESS_JOIN.md).

The exact candidate passed the focused admission test (1/1), complete EditMode
(3,179/3,179), and filtered Smoke (5/5), with stable inputs. The verified full
EditMode package is
[`core-f-owner-current-join-all-editmode-20261016.zip`](Evidence/core-f-owner-current-join-all-editmode-20261016.zip)
with its adjacent manifest. Private original reports and logs remain in
owner-only local evidence retention. The source-join generator now validates
the seven exact-zero assertions as well as its existing 89-family/source-blob
and live-owner identity checks.

## Exact current member-access sites

`audit-source/MemberAccessCut.cs` binds the pinned `Assembly-CSharp.rsp` and
all 315 source blobs to the existing selected symbol projection. It emits
[`selected-member-access-sites.csv.gz`](selected-member-access-sites.csv.gz), with
42,625 source/member/access rows: 39,305 Core references and 3,320 Unity
references. Every row records a repo-relative callsite, enclosing member and
accessibility, target symbol/accessibility, declaration location when present,
and runtime/restore closure. Its manifest records the exact compiler response,
source tree, pair reconciliation and artifact hash. The source graph contains
222 `CoreMove` and 67 `SplitHostDomain` files in the restore closure. Its
adjacent manifest records compressed and uncompressed artifact identities.

The callsite inventory closes the compiler symbol-to-source-member evidence;
it does not itself choose a Core/Unity boundary for each caller. That remaining
mapping follows the already-approved §14 rule: semantic members move together
inside Core, the Unity host uses only `DailyV1Application`, and Unity-specific
null/numeric/serialization behavior remains an adapter/helper concern. The
exact remaining owner and access edges are in the join report.

Artifact identity for the generated access map:

- Generator `audit-source/MemberAccessCut.cs`: SHA-256
  `2931dab3ea6e16fa00d95fae589c67f7f4f7a8873aab746b41b2978b94fc57f7`.
- Project `audit-source/MemberAccessCut.csproj`: SHA-256
  `ba5fb505ce9124c1cbc443051a2d7f06fcf940d13cee4b1639f85c8def67f41c`.
- Manifest `selected-member-access-sites.manifest.txt`: SHA-256
  `c9eaf9b89144d58618b22f2d6d8651547d9d34f63be5de8f991240dc075effad`.
- Compressed map: SHA-256
  `2898b762de97b6134ece33f8cdad035a1aa4f30f3026852850b1913e38919e25`;
  decompressed CSV: SHA-256
  `2702d6d14e33fb7da6a56beda6cdecdbf71f1559fc951e48b2ae975536305d47`.

The final decompression check found 42,625 data rows and matched the manifest's
raw hash. The output scan found no local workstation/home paths or
credential-like markers. These are source/access inventory identities, not an
implementation readiness claim.
