# CORE-F Daily-v1 initial continuation admission

## Candidate and scope

- Candidate branch: `codex/core/CORE-F-app-start-restore`
- Implementation commit: `c6e5b73f518d2b5f67692ccaa3a656415fbf357f`
- Implementation tree: `0c0b6ead7809acff372810f84d0ae34407fdf720`
- Base commit: `d27cb342b38bd8b841a325dc7b1c6efbe4640adf`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

This bounded milestone moves initial continuation-image loading and the fail-closed genesis decision into `DailyV1Application`. Only `NoSave` permits genesis. A loaded image or a present, invalid, incompatible, or inaccessible slot blocks genesis for that application instance. The Unity bootstrap now calls the Core application for a present slot, and Core tests cover both missing-slot genesis and corrupt-slot rejection on the same application.

This is not a complete `DailyV1Application.TryStart`, a Core-owned active runtime, or detached-host completion. The Unity bootstrap still performs the initial slot-presence probe and profile construction; the existing restoration coordinator and remaining runtime ownership work remain outstanding. No canonical promotion or CORE-F completion is claimed.

## Implementation identity

| File | Git blob | SHA-256 of tested worktree bytes |
|---|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `b839a84a676cda230e2c6abbd504eff1834c3ac8` | `8b9abd46281c0ff7939fdaa629d8009be785db2296e3d994b96efb0566adb4d6` |
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `c120a3788da446afba1cd7d4245a2b9147fbbf85` | `8c52e01357b0264c78125f4905c63bea9b409083825d9d7f4590f17c71d3ba04` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `d290cd6d6d5803386588aa4b479f42f41c395694` | `6eb7cd96fb5bf2eb2aebaa2d97aff9230b830158d3d4c5a660b5c2df5b22cb33` |

The source and test changes above are the complete implementation commit. The preserved local `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` edits were captured as validation inputs where applicable and were not staged or committed.

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| Focused `DailyV1ApplicationHostTests` | 6/6 passed | Covered by CoreValidation below |
| CoreValidation cold | 898/898 passed; 0 failed, aborted, skipped, or inconclusive | Run `53624575db8d4d0b8c4692254f8af27a`; original TRX SHA-256 `ffd029274a993904d45bbe76ea61b701ab30baf1a56fecd24507042760ccdaea` |
| CoreValidation warm | 898/898 passed; 0 failed, aborted, skipped, or inconclusive | Same run; original TRX SHA-256 `43021587dc0876b15b0c547b89d8d46dbf90c4e900d8a007a87035a0d25cc600` |
| Targeted Unity restore regressions | 2/2 passed | `PresentInvalidDailyV1SaveFailsClosedBeforeGenesis` and `PersistedDailyV1ImageRestoresWithoutUsingSourceSessionAndContinuesIdentically`; both identities also pass in the stable full EditMode report |
| Full Unity EditMode | 3274/3274 passed; 0 failed, skipped, or inconclusive | Run `8b068be7fc064f02bbb10a9ba535e79c`; exact input snapshot SHA-256 `417ffa8d12ee4418688110529281fac9c57a53197dea90ea2645ad0375c72712`; original XML SHA-256 `c76c7b73ccf2fd5db33ad8b270052d662ea441c9ae3cea57302db4494dbdeee5` |
| Filtered Unity Smoke (`EditMode -TestFilter Smoke`) | 5/5 passed; 0 failed, skipped, or inconclusive | Run `feca0f3f0b514192bcc073dfdcc9476c`; input stable; original XML SHA-256 `0da3ea5c05bdf373780110f887a40b4edcadc7d8041cc73d4a0286ebed833f3d` |
| Unity evidence package | Valid against protected original | Archive SHA-256 `516eac9c0e37c08187a543931419a6aab2fc4468f0cbfcb778689a8bdb2ced2e`; package manifest SHA-256 `05ed6e37d1fc11216dd191d07f904b5022a79d87e78c465b48959a0fdad9dd6d` |
| `git diff --check` | Passed | Checked after implementation commit and again with this record |

The Unity full-suite and Smoke runs used Unity `6000.3.9f1` and Test Framework `1.6.0`; their before/after input inventories were stable. The full result package contains normalized NUnit result XML only. The protected original Unity XML/logs and CoreValidation manifest/TRX/logs remain in owner-only local evidence retention. CoreValidation's runner explicitly does not normalize or publish raw TRX; these raw reports are not included in this candidate. The recorded cold/warm TRX hashes and gate counters are from its protected run manifest.

The initial focused corrupt-slot run passed but its standalone input snapshot was unstable; it is not relied upon as the final gate. The same test, along with the successful persisted-image continuation test, passed in the later stable full EditMode run whose package is verified above.

## Remaining work

Continue the approved runtime/session integration: move the initial slot-existence/admission decision behind the Core application boundary, complete the shared `TryStart` lifecycle over the selected Core runtime, and finish shared P12 restoration and the detached .NET host. This record establishes only the bounded initial continuation-load/fail-closed behavior.
