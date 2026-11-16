# CORE-F Crime/Justice Core-census binding — validation

## Candidate and scope

- Branch: `codex/core/CORE-F-restore-stage-transaction`.
- Code candidate: `2966789cb535bf20a60efaf63774d8fc7f0be893`.
- Code tree: `d70f7dbc5ba79b8436a4d681c9026ba814680470`.
- Parent: `ca976ad92afe5cc8958640ff936b11006f064de9` (tree `a3a52c705eff1eda8d5dc49b394efcd4d8825176`).
- Cached `origin/codex/core/canonical`: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The Core-only selected daily path now derives Crime and Justice opaque identity tokens from the sealed P12 census baseline. If a host binding already exists, Core compares all Crime/Justice operation and receipt identity tokens and both JusticeRecords owner references against those sealed identities. A mismatch faults the selected protocol and census before actor execution or day advancement. This does not change P12 identity semantics, reread census providers during an operation, or add owner families.

| Changed code/test input | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `5a8fedb28847b55b8257ec63afd881a719743e17eab3309946249cdafe9c25e9` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `6c20db7eefce4f481d5f8e43ed58283d1f9cb74df9d2a99a8441d19f45d3f584` |

## Validation

- Focused .NET regression, matching and mismatched prebound Crime identity cases: **2/2 PASS**.
- Official CoreValidation run `550b1760654043a9bb57ec23c37b46ca`: **1,141/1,141 cold and 1,141/1,141 warm PASS**; zero failures, skips, or not-executed cases. The manifest binds commit `2966789c`, tree `d70f7dbc`, and input digest `4385e7aed82f40be9cb6d7ae8f3d0382a6df6ba4ec1d52dcc40da27ad23d28ef`; private manifest SHA-256 `8043e62e33050e59811c0cfad3cceabe0f0fd93f6902ffb9d8998f3684404dd0`. The runner observed an existing Unity `Library`; this is not claimed as a Library-independent .NET run. Original TRX, host traces, and command logs remain in private evidence. No public .NET TRX package is claimed because this run did not provide the separately required `dotnet-trx-source-context-v1`; no context was fabricated.
- Official full Unity EditMode run `ef6e69becf554359b8ff9a1d094b035e`: **3,321/3,321 PASS**, zero failed/skipped/inconclusive; stable input digest `90c3df6e123328b11155de763fa6ca72f119e318b454035ee2d95964a25ed2f3`; private run-manifest SHA-256 `926ce46795296a525703af4416de4e9ab1f720df1e5edf7083167d0f6ff5cead`.
- The official `Smoke` EditMode filter run `56273cf910c740f6a64e24232566b51e`: **5/5 PASS**, zero failed/skipped; same stable input digest. All five passing Smoke identities also occur in the full EditMode report.
- Official privacy-safe full EditMode result package: [manifest](Unity/coref-crime-justice-core-census-2966789c-editmode.manifest.json), [archive](Unity/coref-crime-justice-core-census-2966789c-editmode.zip); manifest SHA-256 `615b9e2204e94fe116287209d6942b35d53f2ec5a7258a512d9ddfa56a5da387`, archive SHA-256 `8275caced89161f313a9219f49db25c9aa82e21fbdbedfd1bd4cd0229b5c6bf5` (176,327 bytes). The official verifier passed against the protected original run manifest.
- Packaging the filtered five-case Smoke XML by itself was rejected because the partial NUnit suite's declared `testcasecount` does not match its descendants. No check was bypassed. The full verified EditMode package retains those same five passing cases. A separate two-case class filter was exploratory and is not used as the Smoke gate.
- `git diff --check` for the implementation commit: PASS.

The official CoreValidation native manifest and original cold/warm reports stay private and unchanged. The Unity result-only ZIP and manifest are committed with this record; raw Unity XML/logs remain private. Protected ProjectSettings edits, a separate local validation document, and unrelated untracked/private evidence remain outside the candidate commit.

## Independent review and disposition

Independent exact-tip code review **PASS** for commit `2966789cb535bf20a60efaf63774d8fc7f0be893` against `ca976ad92afe5cc8958640ff936b11006f064de9`; see [review record](../../reviews/CORE_F_CrimeJusticeCoreCensusBinding_2966789_INDEPENDENT_REVIEW.md). Review noted that the negative unit case invokes the binding preflight directly rather than driving the complete preparation method; source call order was checked and rejects before actor selection or day advance.

This is a local recoverable CORE-F candidate increment. It is not pushed or promoted to Core canonical, and it does not establish complete shared-runtime composition, public `DailyV1Application.TryStart`, detached .NET restoration/hosting, T/T+1/T+2 continuation, or full CORE-F acceptance. It adds no new claim of complete P12 owner coverage.