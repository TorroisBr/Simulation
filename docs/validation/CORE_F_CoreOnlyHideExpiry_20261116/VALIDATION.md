# CORE-F hostless Hide expiry checkpoint

## Candidate identity

- Code commit: `4baf790889b8017a65e56071f12d669cfb1a475b`
- Code tree: `c660c2d20502ec7d88fc0f4229c7c019f3b7aee7`
- Parent: `9ba83f777e8b502ca89f2ccb3e73f95942aa7572`
- Scope: continue the Core-only selected actor turn through an existing one-day hidden state, decrement the hidden-day owner to zero, and remove the exact configured hidden-status fact through the same admitted Crime/Justice P12 boundary. The hostless logger omits presentation output only; a configured host logger is still called.
- The path remains restricted to the admitted selected profile, complete census, supported Normal/Hide actions, no pending directives/choices, no sentence, and no arrested actor. This does not complete the selected action set or CORE-F and is not Core-canonical promotion.

## Independent exact-tip review

**PASS** for `4baf790889b8017a65e56071f12d669cfb1a475b` against `9ba83f777e8b502ca89f2ccb3e73f95942aa7572`. The review confirmed the daily hidden-day write and hidden-status removal occur under the existing owner protocol, and the exact NPC census revision advances twice for expiry and status reconciliation. Existing action identity and rejection coverage remains.

## Validation

| Gate | Result | Evidence identity |
| --- | --- | --- |
| Focused Core-only actor-turn regression | 4/4 PASS | `CoreOnlyActorTurnExecutesAdmittedActionWithCompleteP12Census` |
| Official CoreValidation cold | 1,143/1,143 PASS | Run `77ec914419c3408b86d2beb3f7e72b83`; private manifest SHA-256 `2686b038cd6363530cfaa24119ee3a072ee41ca1933dcba32d8ab80884f41dae` |
| Official CoreValidation warm | 1,143/1,143 PASS | Same run. Input digest stable; the three changed file hashes match the tested worktree bytes and the committed source content. |
| Official Unity EditMode | 3,321/3,321 PASS | Run `da31dc67104849a68613277b2f450b58`; stable input digest `e79a8424bfbb454cf6dc5b172dd3a71361c5ce84e1729b8979ba4591e092ce05` |
| Official Unity Smoke filter | 5/5 PASS | Run `d3a78e84391440a7881f0df0e642eef0`; same stable input digest; all five identities also pass in the full EditMode report |
| Unity evidence package verification | PASS | One full EditMode report; archive SHA-256 `b51851b3b7c2972352d492dc831b8f62692bd9ed30d8f08f77b10fc33e1036b8` |
| `git diff --check` | PASS | Exact three-file implementation/test change |

The Core and Unity runs captured the candidate source bytes immediately before commit creation. The review verified the Core and Unity file inventories against those bytes. The Core runner reported that this checkout had a Unity `Library` directory, so these results do not establish Library-independent .NET validation. The Core original TRX/log files and manifest remain in private local evidence; this runner did not emit the approved `dotnet-trx-source-context-v1` adapter, so no public .NET evidence package is claimed.

## Public Unity evidence

- [Normalized full EditMode manifest](Unity/coref-core-only-hide-expiry-editmode-20261116.manifest.json) — SHA-256 `87174aa8710b5bc322e03d1cd4f376bb3b91c2d7a9c56a2ed6951f777a0a0970`
- [Normalized full EditMode archive](Unity/coref-core-only-hide-expiry-editmode-20261116.zip) — SHA-256 `b51851b3b7c2972352d492dc831b8f62692bd9ed30d8f08f77b10fc33e1036b8`
- The approved verifier confirmed the package against its private original run manifest. The original Unity XML and log remain preserved privately.

## Changed source identities

| File | Git blob | Captured worktree-byte SHA-256 |
| --- | --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ActorTurnExecutionStage.cs` | `d1006fa7b1911f6a56ca985d643cad22d4b36e97` | `ded3d90c8e7fd7171a5e89a31858ca3a5bb95b16be9a46755fcc98f3adaba1ae` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs` | `507f6f71fe86c437180106922b756a3b5eca2571` | `a7013554bb1d24a8f8c2a3c66d3c155c8f0177bdc7ff9a064a31edd407cb892d` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `dfd508e83f31c8cabf350f1ba1fca4b3d721e101` | `ea0901da330fcbd33ab50bd3455b7468be8afdf29a55373da2383a6c511699ff` |

The captured worktree-byte SHA-256 values above match the CoreValidation input inventory; the Unity inventory matches both production files, while the portable test file is inventoried by CoreValidation and is not compiled by Unity. All three worktree files contain CRLF/LF line-ending sequences. Git normalizes those line endings, and each file's normalized text matches its committed Git blob exactly.
