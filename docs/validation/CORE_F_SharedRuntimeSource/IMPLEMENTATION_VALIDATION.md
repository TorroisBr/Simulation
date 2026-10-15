# CORE-F compatibility extraction — validation record

**Status:** implementation candidate; exact-tip independent implementation review pending. No provider callsite has switched and no CORE-F closure is claimed.

## Source identity and scope

- Core canonical base: `b8962cbc8a390c735858ac77b6fdf8e50cfaea8b`.
- Code candidate: `6431fad3bc94263d2db5699706bdf185683b0f76`; source tree: `4cdae296cc2fa84e26d48b38636ed9a9647cab06`.
- Approved design SHA-256: `36137D97DDBB633ECDE6C82E5EE1F7B60ED98F5457781B506531E254DECDE2FE`.
- Scope is the shared typed P18-D payload codec, Unity-compatible numeric helpers, and their .NET/Unity source and tests. It does not switch `P18DCommercialSharingBoundaryStepProvider`, move owners, modify runtime or P12 payload/persistence, or implement host/restore behavior.

## .NET validation

- `CoreValidation` private run `d0a9dc1973254568bbe687d3ae3ed18e`: cold 171/171, warm 171/171, zero failed, skipped, inconclusive, timeout, or aborted cases; source/input snapshots stable.
- Focused parity tests: 2/2. The same two test identities also pass in the complete Unity ALL EditMode report below.
- TRX-EVIDENCE-v1 package: [`core-f-compat-6431fad3-cold-warm.manifest.json`](core-f-compat-6431fad3-cold-warm.manifest.json), SHA-256 `A52BC16FA63FE1C6ED7A3A328E450B68E03A72B40C19962846CFB077D059D67A`; archive [`core-f-compat-6431fad3-cold-warm.zip`](core-f-compat-6431fad3-cold-warm.zip), SHA-256 `6917029217d029e4bf7c8f752aced893bdf0a4a476cd242eec5ab539d05ec878` (77,103 bytes). The package binds source commit/tree and input digest to this candidate.

## Unity validation

- Unity `6000.3.9f1`; Test Framework `1.6.0`; evidence packager `1.1.0` on its pinned Windows PowerShell Core 7.6.5 / .NET 10.0.11 runtime.
- Stable unfiltered ALL EditMode run `6896dae1a7b847cea43056a568490a36`: 3,175/3,175 passed, zero failed/skipped/inconclusive; Unity executable stable. Exact source commit/tree are the candidate above; stable complete input snapshot digest is `8b09f676668e5ebcf87ffdc7da0b20ac23648bb92ff7edcddf62c8ac170b9689` (1,644 inputs).
- Durable normalized result package: [`core-f-compat-6431fad3-unity-all.manifest.json`](core-f-compat-6431fad3-unity-all.manifest.json), SHA-256 `B95452B2C0E93B62FC8233CF55482DF5777BE2928CF10B4F42B7B961EFD29173`; archive [`core-f-compat-6431fad3-unity-all.zip`](core-f-compat-6431fad3-unity-all.zip), SHA-256 `9341c09d66fbe693ad0a2f3d13cb0d2b6645b437403f4616fef129ec4ab2d6d4` (166,923 bytes). The approved packager verifier passed against the protected original report. The ZIP contains only normalized result XML; raw logs remain in protected local retention.
- The complete ALL report contains and passes `CORE_F_CompatibilityParityTests.CodecMatchesCapturedBytesAndLegacyReaderOutcomes` and `NumericHelpersMatchEveryCapturedUnityVector`, plus all five official Smoke case identities. The focused filtered parity run `4d16f38fa9d942558136771d992ece1c` passed 2/2; the filtered Smoke run `c283b2dedbfe4988ae7e83bf8d0a0ac3` passed 5/5.
- The packager rejects those filtered XMLs because NUnit retains parent Assembly/fixture `testcasecount` values for the complete discovered suite while the filtered XML contains only selected cases. The records are unchanged and retained privately; they are not represented as packaged evidence. The complete ALL report is the durable case-level evidence for both the parity cases and all five Smoke cases, matching the existing Core State precedent for filtered Smoke aggregate-count rejection.
- The initial first-import full run `d24701ee9ea344589e5ee427e4e11afd` passed 3,175/3,175 but had an unstable input snapshot after Unity rewrote raw line endings in `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset`; it is excluded as a gate. Subsequent focused, ALL and Smoke runs shared the same stable snapshot. The differences are line-ending-only: Git reports no content diff against the candidate tree. No production source, tests, assembly definitions, Packages inputs, or validation tooling changed during these runs.
- A prior PlayMode filter attempt selected zero cases and is excluded. Its accidental `ProjectSettings/ProjectSettings.asset` write in the protected implementation worktree was restored byte-for-byte to the captured pre-run SHA; the existing user `EditorBuildSettings`/`ShaderGraphSettings` changes, `.meta` files, and older oracle artifacts were left untouched.

## Validation disposition

- The complete .NET cold/warm package and complete Unity ALL report package are retained and integrity-verified.
- Focused and Smoke filtered executions are supplemental. Their individual XML normalization is rejected by the current fail-closed packager due to suite aggregate mismatch; no report was edited and no mismatch was waived. Their same required test cases and outcomes are present in the durable complete ALL report.
- `git diff --check` is required again on the final documentation/evidence successor before review. Provider switching remains separately gated on the reviewed parity/rejection work and exact-tip independent review.

`Design-text limitation:` one sentence in the reviewed design describes null object rows in the runtime roster differently from the hash-bound Unity Editor capture. This implementation follows the accepted captured output and does not change a provider callsite. Before provider switching, the design owner should reconcile that prose and verify the exact captured behavior; this note does not reinterpret the approved contract.

## Unity oracle-input inventory remediation

The first independent implementation review found that Unity's standard input roots omit the three CSV files read by `CORE_F_CompatibilityParityTests`. The earlier `6896dae1a7b847cea43056a568490a36` ALL package is preserved but is superseded for final review by a sidecar-bound rerun on the same exact code commit/tree. The code and test source did not change.

- Final sidecar-bound unfiltered ALL EditMode run: `17bfb6a506ee456f8aeb7d8ed77f5b45`, 3,175/3,175 passed with zero failures/skips/inconclusive and stable standard input snapshot `8b09f676668e5ebcf87ffdc7da0b20ac23648bb92ff7edcddf62c8ac170b9689`.
- Additional input identity sidecar: [`core-f-compat-6431fad3-unity-all-extra-inputs.json`](core-f-compat-6431fad3-unity-all-extra-inputs.json), SHA-256 `657178480c76188600bd8a7d3e5a99488ccaFA37c4d85c060192178400b8ba70`. It binds pre/post byte length, SHA-256, Git blob identity, source commit/tree, and run ID for the three CSV fixtures above. Every before/after hash matched, each Git worktree entry was clean, and the sidecar declares them as additional Unity validation inputs.
- Final normalized result package: [`core-f-compat-6431fad3-unity-all-inputbound.manifest.json`](core-f-compat-6431fad3-unity-all-inputbound.manifest.json), SHA-256 `B0A4F12E99E8208844B6B2ED5690B8829794BBD5BBF857E65825EF16205BD349`; archive [`core-f-compat-6431fad3-unity-all-inputbound.zip`](core-f-compat-6431fad3-unity-all-inputbound.zip), SHA-256 `ee56efbee92816320dd681911764e1418ba02815d95313bd2d8d070c7421aa13` (166,854 bytes). The approved verifier again compared the archive with the protected original report.

The complete report from this final run contains and passes both codec/numeric parity test cases and all five Smoke case identities. The earlier standalone focused and filtered Smoke executions remain supplementary; their filtered XMLs were not edited or packaged. The review finding is ready for independent revalidation against the sidecar and final package.
