# CORE-F restored PoliticalKnowledge owner identity review

**Status:** VALIDATED_CANDIDATE for the bounded restored P12-F PoliticalKnowledge identity integration only. This record does not promote Core canonical or close CORE-F.

## Exact identity

- Repository: TorroisBr/Simulation (https://github.com/TorroisBr/Simulation).
- Candidate branch: codex/core/CORE-F-core-owned-runtime-application.
- Final candidate commit: 7cd9fd97e6b0acdce966b1cee84a7c437d1c82bf.
- Final candidate tree: f163c61c6a95539e2afb3f692159c9fc09d97839.
- Candidate evidence commit parent: 94b124c4c1e9881c66ccfb0179501304249ec50e, tree d201f74b101f2c4af601885f7e87a02dcc3a7064.
- Code-bearing implementation commit: 78d3bffa527b7599167e5c84b6315ed95f708439, tree d2ac164d0f532dd52d8041af7d751b14f5b84b5b.
- Implementation base: 7894e637ceff192e76a425d6e7a5179b8fdc8433, tree f0ee03489984116e5f6c6c4a8d39a7dbc3ca8403.
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- Final candidate tip is synchronized with origin/codex/core/CORE-F-core-owned-runtime-application.
- Review-record branch: codex/core/CORE-F-P12FPoliticalKnowledgeExactOwnerReview_7cd9fd97.
- Reviewer: independent read-only review agent; implementation and candidate branches were not modified.

The final candidate adds only the validation record, normalized .NET manifest, and normalized .NET archive after 94b124c4. The production/test source remains exactly the code-bearing 78d3bffa tree. No canonical promotion is claimed.

## Scope reviewed

The prior finding on candidate 7894e637 was that Core accepted a restored session while its selected owner graph did not retain the staged P12-F PoliticalKnowledgeStore. The implementation at 78d3bffa resolves it:

- DailyV1Application requires the staged P12-F store to be bound to the exact P12-D PersonStore, binds that exact store to the restored owner graph, and checks reference identity before accepting the Core session.
- The Unity SimulationRuntime projection retains the graph-bound store and rejects a different supplied instance. If no Core-bound store exists, the pre-existing clone path remains.
- The restored continuation regression checks that the graph and Unity projection use the same store before T/T+1/T+2 parity. The test only adds an assertion; it does not weaken prior assertions.

Restored Core composition remains private through owner staging, graph composition, projection, target owner-vector checks, and admission. Active-session publication remains behind the existing commit gate. The application-null route keeps the old clone behavior. The Core application route also applies exact owner binding when used by capture-bound composition; this preserves the selected authority instance and does not publish early.

No actionable implementation or test-integrity finding remains in this bounded slice.

## Validation evidence

The Unity result package and the exact-source .NET TRX package were independently checked.

| Gate | Result |
|---|---:|
| CoreValidation cold | 988/988 passed; 0 failed/error/not-executed |
| CoreValidation warm | 988/988 passed; 0 failed/error/not-executed |
| Focused restored military/site-knowledge continuation | 1/1 passed |
| Focused restored target-owner-vector atomicity | 1/1 passed |
| Restored image atomicity cases | 2/2 passed |
| Full Unity EditMode | 3294/3294 passed; 0 failed/skipped/inconclusive |
| Official Unity Smoke | 5/5 passed |
| git diff --check | PASS (recorded for implementation commit) |

The .NET package manifest binds its run to candidate commit 94b124c4c1e9881c66ccfb0179501304249ec50e, tree d201f74b101f2c4af601885f7e87a02dcc3a7064; inputStable is true. Its Core application source SHA-256 (8581a66e8118b11dfd8e69437433fa812fd2d23bf5508d060341c63d1a0159a1) matches the reviewed implementation and current candidate. The official read-only verifier was run with the retained private source-context manifest and returned:

- IsValid=true
- OriginalComparison=originalComparisonVerified
- PackageIntegrityVerified=true
- GatePassed=true
- 1 run, 2 result entries

The cold and warm result counters each show 988 executed and passed with zero failed, errors, timeouts, aborted, inconclusive, or not-executed cases. The private originals and source context were only read by the verifier; they were not modified or republished.

The public normalized TRX archive contains cold and warm reports only. The archive SHA-256 is 6b130e34d53395f38c6a7e91970321c47b77562d0cf40a9c2c80f9c70fc3ee0f; the manifest SHA-256 is c960c995437d33e556ff93cf14c6b73998b4a89e54f0c3e2c0eee097d93b20d3. Both match the validation record and committed bytes. The archive and manifest contain no absolute workstation paths. The package tool file hashes match the manifest.

The full EditMode Unity package verifies as VALID. Its input snapshot is the exact implementation commit 78d3bffa527b7599167e5c84b6315ed95f708439, tree d2ac164d0f532dd52d8041af7d751b14f5b84b5b. All three production/test source SHA-256 values match the recorded and current source:

- SimulationRuntime.cs: 978b2d100955c762d347d70ed7b0f84edb859cc692e9d2600ca9050c09e46af3
- DailyV1Application.cs: 8581a66e8118b11dfd8e69437433fa812fd2d23bf5508d060341c63d1a0159a1
- P12GOwnerEntryQualificationTests.cs: afc30f5592bd92dc257440f578ce93b680584c8dd43613c57e177aa4ce4ae637

The Unity archive SHA-256 is ec1e2ecdfab127660d4f4f72444e9c9c244b4da6b168f958fa32aa92618f6eab; its manifest SHA-256 is f308df79cc9fe18e5b3abe019b901785fb08f174cd7d2e728cceb28db8a46ba4.

## Verdict and limitations

Independent exact-tip verdict: PASS for the bounded restored PoliticalKnowledge owner identity integration. The earlier code finding and .NET evidence limitation are both resolved. No tests were rerun during this follow-up review; the verifier checked preserved evidence only.

This does not establish complete CORE-F acceptance, a detached .NET host, or canonical promotion. The remaining CORE-F application/host requirements stay open. No Phase 12/13 status change is inferred.
