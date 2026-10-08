# P12-E ArmedForce / Manpower / Position Snapshot — Current-Base Exact-Tip Review

**Verdict: VALIDATED_CANDIDATE.** Independent review of the exact current-base candidate passed. This review does not promote code, complete P12-E, or change P12-A/P13 status.

## Exact refs and review basis

- Repository: `TorroisBr/Simulation`; origin: `https://github.com/TorroisBr/Simulation`.
- P12 canonical branch/base at preflight: `ed3aad0bcf98fc1b709b6bc632452689448b8803`.
- Candidate branch: `codex/phase12/P12ECurrentBaseRevalidation`.
- Exact validation-record candidate tip: `e161093a56a0308314895eebe7e89b251eea7092`.
- Exact tested code/test tip: `99302cffec8cda35945ed191bd3dba98beb7bd57`; implementation code commit: `d57120cc8e7876910caec6f2a134231a8a23bfee).
- Tested repository tree: `2d025424512e4a8c9cda10b840714b444ebbcdfa`; tested `Assets` tree: `811f8018c5722a6cf5a0bbf54ec04a9f73f77397`.
- GitHub compare confirms P12 canonical is the candidate's clean merge base; tested tip is 6 commits ahead, and final candidate tip is 7 commits ahead with no base-only commits. The one commit after the tested tip adds validation XML/log artifacts and the manifest only. There is no post-test executable or `Assets` change.
- Current architecture: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Current P12-E umbrella design at the exact P12 base: `docs/design/PHASE12_E_TECHNICAL_DESIGN.md`, blob `15aaee09d3295cff81a48e166b620c89f5156346`.
- Accepted owner-specific design: commit `3ba6516bf6bcf081445f9c2c7fea192423487851`, blob `863c90329f1756bccd5135d255e8761891682843); exact-content independent design review PASS is `903bd26fc31f015f1e0d89fdf436ec8d240ab5df` on `codex/review/phase12/P12EArmedForceManpowerPositionSnapshotDesignReviewR2`.
- Current P12-D City/NPC assembly is already canonical at this base. Its State records the promoted City/NPC relation-order assembly; the current-base design revalidation is PASS at `c0da9fa01aa6aab6db58b34a314ad98c45aef362`.

## Diff and contract review

The full base-to-tested-tip diff is limited to the three owner authorities (`ArmedForceStore.cs`, `MilitaryManpowerFoundation.cs`, `ArmedForceSpatialPosition.cs`), the detached P12-E snapshot/private stager and its focused tests, the exact reviewed design, and paired Unity metadata for the two new C# files. The later validation-only commit adds the retained run records. No `ProjectSettings` path or existing `.meta` file is changed. The two newly added `.meta` files belong only to the new snapshot source and test assets.

The current P12-D implementation changes separate City/NPC owner files; there is no changed-path overlap with this E candidate. The E code does not edit `SimulationRuntime`, bootstrap, census/admission, mutation-epoch or quiescence wiring, D assembly, Battle storage, P16/P17 implementation, or P12-G publication.

The reviewed slice matches the accepted three-owner boundary:

- Capture binds all five existing required P12-B sections to the same token-carried owner vector and checks exact section/schema/role, owner identity, cardinality, and local revision. It reads detached owner values and checks for stale owner stamps before returning.
- The exact token-bound manpower owner must have immutable `SourceProvider == null`; source-bound rows reject. P10 LocalTopology remains `NOT_COMPOSED`; P16 extension state and P17 provenance reject.
- DTOs contain detached scalar/string/enum values, copy nested read-only rows, preserve owner-local and per-contingent revisions, stable reference IDs, ordered characteristics/cohorts, and typed P8 spatial references. No token, live owner, provider, Unity object, or process-specific owner identity is serialized.
- Staging validates schema, cardinality, row ordering/uniqueness, hierarchy/cycles/lifecycle, Person references, one-to-one contingent/manpower coverage and Amount mirrors, cohort rules/overflow, source-null policy, and supported P8 position resolution. Private factories directly restore values and revisions without public-operation replay; failed staging returns no partial owner set and leaves live inputs unchanged.
- Current-base D code remains additive and compatible: E consumes staged Person/P8 dependencies and does not replace or duplicate D City/NPC ownership. P12-B mutation/epoch limits remain unchanged.

The ten focused E tests cover exact-empty and populated round trips, retained typed position for terminated registered forces, detachment, malformed/injected inputs, exact five-section token binding, negative values/revisions, and factory failure/no-side-effect cases. The code and test blobs for the three owner files, snapshot, and focused test source are identical to the prior reviewed R3 candidate; this review independently rechecked the exact current-base composition and its D integration boundary.

## Exact-tree validation

The committed manifest is `docs/validation/P12ECurrentBaseRevalidation/VALIDATION.md`. I independently fetched the committed artifacts and verified every XML/log hash pair; the XML hashes match after restoring the runner's recorded Windows CRLF bytes, and compressed logs decompress to the recorded raw-log hashes.

| Suite | Result | XML SHA-256 | Raw log SHA-256 | Gzip SHA-256 |
|---|---:|---|---|---|
| P12EMilitaryOwnerSnapshotTests | 10/10 | `49D6F9345680F7627CB31363BFE07016AE1EE068CCADDE596B2CA864A56045CC` | `64E40271C159BA279DBA473D66660A35D9B187F136A3FE34219391ACE0A62AAD` | `13848E5F19176E9377921776823E6FB3973E0917C3053A265C949B95E11F10EF` |
| ContinuationCensusProtocolTests | 24/24 | `EF7479CC543B3647B148D09E6884CDC4207ED45C8EB60A0B3F01B4587AD3BBFE` | `BC52779555D11F0E72B739CC300325279F423E06E280804E71FBDE67FC2DEFCC` | `11CF38CE389EF71085FEFEC3B2E4460EE9D55DBB457702B8F4824D7F47699DCF` |
| P12DNpcReceiptOwnerCensusTests | 13/13 | `44F368E9B95E67CFE3792356820F2C32B1DBC6AD28490E990CC0B4D28331F1EF` | `A45059B2C8B1A3327CDA1925832B152CD8B9AB99C10360A08FA966F3B167D963` | `1085E5BF0A35ED874E4CE85F2487245B825E57F3C016A1D04E5D3DE032EBEB0E` |
| P12DCityRootOwnerSnapshotTests | 17/17 | `3FE0C525C22DF1F82619D1A2C30C033D108DC80E36F5748CA6BF188B2F745690` | `5997269510AE3151074A025D6EBA984EB11904A9EAF1165B36E8BF5A825B35CC` | `6A24461FFAA32B4D7B5F9E9D7FE908F570DACE08746AD88AD6E5515B62EE78A2` |
| SimulationBootstrapCompositionTests | 26/26 | `46BF9BE9A00E57DC96A0698F4646D3CE91CF64F5244D9161B1898208AACFB75D` | `89F382AA85FE2B869D1537F9FD555190639A6B39EE176C21CB67675BF7E4A5B0` | `134593159A717F00C119F7C5FC67DF8B3D141D31BFD29B3D70522CEB229F275A` |
| ALL EditMode | 2602/2602 | `B9E4B3CEFE5025B6832FB5DCB28D634A7C99C969830645C53F3E9094729EE52D` | `86D8AEA0E12E3721C3E50BEFFC41F669182CCA23E66042B981C61DF650948F05` | `671861D7B6810AEB894DECC2290B05739878EF5F4263CE0B59ACF340039FC38E` |
| Official Smoke | 5/5 | `3787FC797332F145DB5026048750F69C6D4C07D3CE30B1E3CE22AD09E3929EE1` | `8873B3E90C6E88FDEEB648C5BB29CACC91F5AC95BE6D34C2309F481CAA5B6173` | `22748D40BF6F1F78E98145EF0F2F424A2F2ECB73106D8CA63656999669311BBB` |

The validation manifest records `git diff --check origin/codex/phase12/canonical...99302cffec8cda35945ed191bd3dba98beb7bd57` PASS.

## Limits and integration constraints

This candidate is one P12-E ArmedForce/manpower/baseline-position snapshot and private-staging capability. P12-D remains in progress; P12-E remains in progress. It does not complete P12-D/E, P12-A, P12-B, P12-G, or Phase 12; it does not claim complete owner or shared-epoch coverage, global quiescence, capture eligibility, profile-wide export/hydration, P12-A readiness, P13 readiness, or Phase closure. P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.

This exact review is not canonical promotion. Before promotion, refresh the canonical ref and verify the candidate remains a clean fast-forward with the same reviewed code tree and current-base compatibility.