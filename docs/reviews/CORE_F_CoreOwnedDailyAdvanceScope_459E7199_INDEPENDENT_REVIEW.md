# CORE-F Core-owned DailyAdvance scope — independent exact-tip review

**Verdict: NEEDS_CHANGES**

The implementation diff has no confirmed code finding. The attached Unity full EditMode result package does not establish that its 3,279 passing tests ran against the committed candidate inputs: eight tracked project inputs in its 1,964-file snapshot mismatch both the candidate and its base, with no declaration that they are fixed or external dependencies. Do not treat this candidate as `VALIDATED_CANDIDATE` until exact-input validation evidence replaces or explains those rows.

## Reviewed identity and scope

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit/tree: `459e71997ac1ebff53f490f0dc82c4088a5c2c60` / `dbe2d4a12f6f25dfb37c69586da789ff8a8482c8`
- Direct base: `8246504413802efe44d6667c41bcd7c71e18e103` (tree `841c22891f7c2abd6f1ea8df59d1ce7885227f97`)
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate and canonical tips were checked and matched those SHAs. The candidate is a direct child of the base, and canonical is an ancestor of the base.
- The full base-to-tip diff is seven files: two production source files and two test files, and the validation note, manifest, and zip. No candidate files were edited during review. No ProjectSettings or protected private evidence was accessed.

## Implementation review

Core now opens the `runtime.advance-day` protocol operation on the exact `DailyV1ExecutionBoundaryState` bound to the selected session. Its Core inventory check rejects unregistered operations, and the new disposable scope closes that same operation. The Unity adapter no longer receives the begin-operation callback; it reports only whether its bound P12 census is healthy and idle.

The Unity preflight uses the existing census instance constructed with `dailyExecutionBoundary`. It requires the admitted owner thread, a sealed/complete P12 operation inventory, a healthy protocol, and zero active P12 operations. Core then checks its own sealed operation inventory before adding `runtime.advance-day`. A mutation-epoch reservation is created only while a P12 operation is active, so the zero-active-operation precondition also excludes a live reservation in a healthy protocol. Capture retains the exact shared Core capture-scope check and P12's reservation/epoch checks.

The operation is entered before selected-stage construction and before the clock changes. It remains held across the selected advance, closes before completed-boundary capture/token publication, and batch advancement holds one operation scope for the batch. Existing Core application scope and advance lease cleanup still run on rejection. The new scope is idempotent via `Interlocked.Exchange`; wrong-thread disposal fails to close the Core operation, invalidates the capture token, and faults the P12 admission. This leaves the operation active, so it cannot appear idle after a failed close.

The added Core tests assert that both Core scopes are active during stage construction, normal disposal is idempotent, off-owner-thread disposal faults and leaves the active operation registered, and a rejected P12 readiness preflight leaves the clock unchanged and does not request a selected stage. I found no implementation defect in the reviewed change. This is a bounded operation-ownership increment; it does not establish complete CORE-F acceptance or canonical promotion.

## Validation and evidence integrity

The four changed source/test files named in `VALIDATION.md` match their recorded SHA-256 values against the committed candidate after the repository's CRLF checkout representation. `git diff --check` passed independently for the complete base-to-tip diff.

The Unity manifest binds one full EditMode run (`dfa9196eb68f433b8a0c36d11367914c`) to input snapshot digest `47b79dfdbcd064bedc513cdbc6933401f51c359d26b2f769a5972764cbefc9f3`, Unity `6000.3.9f1`, and repository HEAD/tree `8246504413802efe44d6667c41bcd7c71e18e103` / `841c22891f7c2abd6f1ea8df59d1ce7885227f97`. The snapshot contains 1,964 file rows, each with only `path`, `bytes`, and `sha256`; it has no dirty-tree inventory, exception sidecar, or fixed/external-input designation.

I compared every input row against the committed base/candidate Git tree, accounting for CRLF checkout representation: 1,956 match, while the following eight do not. Each is a tracked project source, test, or Unity metadata file; each has the same Git blob at base and candidate, but the snapshot's content hash and byte count match neither committed tree. These are uncontrolled source mismatches, not declared external dependencies.

| Input path | Snapshot SHA-256 | Candidate checkout SHA-256 |
|---|---|---|
| `Assets/_Project/Scripts/DecisionRecords.cs` | `8ad6374e5a83d0cb4e3199e3754dadc0041a405e21c62fc81acb8104ac7a337f` | `79598725078cf4ee8b92406819c83770c577e033f77ba7f0ebb0b8913a19c295` |
| `Assets/_Project/Scripts/DomainEvents.cs` | `f251888ab31e5d47f60a4e5ecb1206a7f05d4a35af929d778fa484c93d03fc1f` | `893a4095e82016974cb36b0c08c4a0b430ab64d8abd63404433d8a383cebc80b` |
| `Assets/_Project/Scripts/P12GDailyV1OwnerVector.cs` | `8093ad38e2bb832595d7ba536d643444a392c76fca471c03c0bc8dd0cafaf35c` | `67224355d28b7cda69a7a6477d5036f947d3a1001da2b6598ba8a78ddf48f63d` |
| `Assets/_Project/Scripts/P9GenesisManifestContinuationSnapshot.cs` | `3e063f95104487cc305388ed912cac93fbe4cae893756ed07f27ea182dd7d072` | `ab8813f9b2d139db7e7bd85f6df8ac15a34ac71e7ca2f50f97c7b6eaade7759d` |
| `Assets/_Project/Tests/EditMode/Editor/CoreRuntimeTests.cs` | `4942aa10041764d3dbc7bd54abb9620e610c7c43dc893c9938ff68f79becc693` | `b6a4ac1530ad4db1900edc6aabc8d6e7534b18842b4b30dfe63b33b4940f9e18` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `a538b2169a811fe45e6bfed64f25dd609045ae7f5187a0dabbf82f3e191295e7` | `ec205e602a17a262a067557a99688d28ec88ef02892c904c18812a0c23f7c11e` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1DecisionOccurrenceReceiptAuthority.cs.meta` | `ba16a1b5e77a12468f5c40dfa1f51a7af8f4feb672f4c94b2a651bb415c0ccbe` | `ab27ba231989837bf51421db3dd11b7276a24236553c8f265b918bf3058ac817` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1RestoredOwnerVectorContract.cs` | `fe6731ae409d62a57b19ee8a99912d9162aa15b3b7a7880184d9cfee1b5156e3` | `7a6b550e78cf189e2684c657ea1f4be4311ab076887190d4417fbb044f170133` |

The normalized Unity package itself is internally consistent: archive size 173,567 bytes and SHA-256 `a82e6009976fa5f5d52ac49d45ca258598ebb540473c11aab08d0ce2592d750f` match the manifest; the manifest's CRLF checkout SHA-256 `f6a061d0385e702003bdd54a15547b472fc18889e2202c915d64ae08552d7722` matches the CRLF representation generated from its committed blob. The archive contains one normalized XML result, with 3,279 total/passed and zero failed, skipped, inconclusive, or aborted. The public manifest and normalized XML contain no absolute local-path matches; the XML contains no `<output>` elements. Package integrity and privacy do not cure the input-tree mismatch, so the full-suite pass cannot be attributed to the exact candidate.

`VALIDATION.md` also reports focused Unity 13/13 and official Smoke 5/5. The standalone Smoke report is retained privately and was not inspected; the record says its five cases are in the full EditMode report. The .NET CoreValidation record reports official cold/warm 922/922 runs and names the input digest and original TRX hashes; its source/test hashes match the candidate where recorded. The raw TRX and private runner manifest remain protected and were not inspected, so those claims are recorded from the public validation note rather than independently re-parsed here.

## Required resolution

Replace the Unity full-suite evidence with a package captured from the exact candidate tree, or provide a reviewed, explicit provenance declaration for each additional input and show why that identity is an accepted dependency. The new package must bind every input row to the candidate commit or a specifically identified immutable dependency. Then refresh exact-tip evidence review. This review did not rerun tests.

This `NEEDS_CHANGES` result is evidence-only. It is not canonical promotion, CORE-F acceptance, or authorization to broaden P12/CORE-F scope.
