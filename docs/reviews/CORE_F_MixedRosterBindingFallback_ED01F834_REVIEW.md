# CORE-F Mixed-Roster Binding Fallback — Exact-Tip Review

- Review result: **VALIDATED_CANDIDATE** — independent implementation review passed; no actionable findings.
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Candidate tip: `33491d0ab0a312f8b3b84f82de3b28780b9f172f`.
- Candidate tree: `acebca05b092cc0a81fd09d700c4e15f4fa9ffb9`.
- Reviewed code commit: `ed01f834f314ca29d35406ab31efe29a98fba322`.
- Reviewed code tree: `acebca05b092cc0a81fd09d700c4e15f4fa9ffb9`.
- Previous reviewed candidate: `73fbd3ecead257e69f893364fac19a44a431f5b0`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Independent reviewer: `/root/review_npc_materialization`.
- Review branch: `codex/review/coref-mixed-roster-binding-fallback-ed01f834`.

## Finding and correction

The previous review found that registering a legacy, unselected NPC into a mixed roster could incorrectly attempt selected Daily-v1 actor-roster reconciliation. Such an NPC has no selected owner graph membership, so the Core compare-and-swap failed before Unity's supported compatibility fallback ran.

The correction makes selected actor-roster reconciliation conditional on both the target NPC using selected Daily-v1 definitions and the exact selected owner instance being mapped in the current graph. Otherwise the existing compatibility path remains available. The added `MixedDailyV1RosterBindsLegacyNpcThroughCompatibilityPath` regression verifies successful binding to the exact Person, one PersonStore revision, one shared membership epoch, and a non-faulted runtime.

The independent reviewer verified the complete changed delta from the previous reviewed candidate through the exact candidate tip, including the guard, regression, source identity, validation evidence, and package consistency. No test assertions were weakened. The prior candidate's review remains applicable to its unchanged code; this record reviews the follow-up delta and exact resulting tip.

## Validation evidence

- Official CoreValidation: **1,100/1,100 cold and 1,100/1,100 warm PASS**. Run ID `e59d51bd80d94d9fa8fa0f85dc5586d4`; source commit `ed01f834f314ca29d35406ab31efe29a98fba322`; source input digest `fb6c30e3c5f077c8ba7e2bc1005660de604cc514417c5a2033894e8d4edec58b`. Protected manifest SHA-256: `ba54552ccd05770a1b10eafede120733bb535d7617e8d33436f5edfd0cf11f9c`. Cold and warm TRX hashes: `a80370c2ec40b342b8e94184cf7cf7a9dfdc8746037f7f7438c8bbbf6bec718a` and `81b5804689db0111355faf58e6ca9e90aa27368d0b3f41a96ec93d5d3a688ddb`.
- Official Unity EditMode: **3,317/3,317 PASS**; run `01800e8b5d344dc88ca931e8b7847df6`; stable input digest `619dcd0fc693b545b1c04bef1d8bf1cec212446a58e1a739d9731ea1029ec099`; protected run-manifest SHA-256 `a1e8f7dafe31dea75e8195bec5666fe800053f82b9afd8d6802b6a5ddbed537e`.
- Official Smoke: **5/5 PASS**; run `7d504ba8253d4065b92f6f94db5f5346`; same stable input digest; protected run-manifest SHA-256 `631be5042d1fffa665be4d3ca704aeb1462bd63d3f1bacdbfb84d320d55197b3`.
- Privacy-safe Unity result package: `docs/validation/CORE_F_MixedRosterBindingFallback_20261112/Unity/coref-mixed-roster-binding-editmode.zip`, SHA-256 `fb526df2b778db3edc313ad9d5b498f7d1661978800948a2e8e164fb280d3d22`; manifest SHA-256 `5d1d99d89acbca421581199eef7bfe90a8963ed1b30146f0c5f759e1c484be7c`. Independent review verified the package against the protected original and confirmed the new regression occurs once and passes, as do all five Smoke identities in the full report.
- `git diff --check`: PASS. Candidate source/test hashes: `SimulationRuntime.cs` `f42c60828a1e51141d1ac7742ae48f44812e9b311da1ae9297ccc101f58ef10d`; `SimulationBootstrapCompositionTests.cs` `d85cd6b6985aad4d0d44307bee155ca2b2439858d6c568273a58452de1f4fa8a`.

## Evidence boundaries and scope

The Core runner's private manifest uses `simulation-core-validation/v1`, while the separate public TRX packager requires `dotnet-trx-source-context-v1`. No source context was fabricated; Core's original TRX and manifest remain private and unchanged, and this record does not claim a public Core TRX package. The Unity package is the reviewed privacy-safe public artifact. The validation runner reported a Unity Library present; this is not evidence of a Library-independent .NET host.

The ProjectSettings modification was disclosed as part of the Unity validation input and excluded from the candidate commit. Unrelated validation documentation, untracked files, private evidence, and protected originals were not staged or modified.

This is an incremental, unpromoted CORE-F candidate review. It does not establish a complete Core-owned runtime/session, public `DailyV1Application.TryStart`, complete P12 restoration, detached .NET hosting, or separate-process T/T+1/T+2 continuation. No Core canonical promotion is requested or implied.
