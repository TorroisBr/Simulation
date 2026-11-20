# CORE-F Core-only autonomous Steal turn

Status: validated implementation candidate; independent review and publication remain pending.

## Delivered behavior

The selected Core-only actor-turn boundary now admits the existing Steal action when it has the exact supported Core action slot and distinct target relationship. Core’s existing crime operation performs selection, theft, owner mutations and P12 census refresh. The slice does not broaden the action catalog or alter Unity’s fallback for unsupported/mixed rosters.

## Source identity

- Validation base commit: `81b187dce3eb1d9c5714b7b4497a25fa2f7b202d`
- Base tree: `6c64c7c80430e5d90bb3363600f42e2403cb0c3a`
- CoreValidation working-input digest: `4500ecc0c079d9877b4fb40da6701f5f411383e768d667a88af69e0466d64814`
- Production source SHA-256: `3433f89a5f30e15ed19756048bf5d96769029a5c4df8617ada0b0103700d6d2c`
- Test source SHA-256: `0fee0b43ce5c6ebaa31f1ed53e832fa03ebf0086315af4c95f7dd02b1bb50f29`

The exact production and test hashes match the official CoreValidation inventory. The validation worktree also contained protected ProjectSettings and an unrelated earlier validation-document change; both remain outside this candidate and untouched.

## Validation

- Focused actor-turn cases: **11/11 PASS**.
- Official CoreValidation: **1,173/1,173 cold and 1,173/1,173 warm PASS**, with stable inputs. SDK `9.0.315`. Run `88ff28ba3a5f4f39a84bfed25051b37f`; input digest above.
- Official full Unity EditMode: **3,325/3,325 PASS**, zero failed, skipped or inconclusive. All seven `SimulationRuntimeLongRunTests` cases are present and passed in this report. Run `938c3309f1144d0db76cc5ee4e9123f9`; input digest `efe6c98a91361b5d76e7b87fc33ea3b59ca88a4ac3e72e3b2c4618b1c5e83fc7`; original XML SHA-256 `59b175b0600040079431feb2fef6701f47d1bb3fdf07d04dd49d1829046bb52e`.
- Full EditMode result-only package: [manifest](Unity/coref-core-only-steal-full-editmode-20261120.manifest.json), [archive](Unity/coref-core-only-steal-full-editmode-20261120.zip). The approved packager verified the archive against the protected original run. Manifest SHA-256: `aa1799c2892b5afb6edf981dc896b7ccd8aeb4056f3f818e11d56afdfb411480`; archive SHA-256: `3f93f7e372c5c6256d5642e21c4d47348aa281e98445f5fb5bb64743f66b88a9`.
- The `GMConsolePlayModeSmokeTests` fixture is in the Editor test assembly and enters Play Mode from an EditMode test. Its documented PlayMode selection returned zero tests and is not claimed as a pass. The correctly selected EditMode fixture passed **2/2** (run `7fb01400eb2a4f90a2c9531a328acf08`, original XML SHA-256 `5fd4fca671b64cf36a40c3df826a7da78dfcaa004613b4260533728ad5a0e207`); both cases are also in the full EditMode report. No separate “Smoke 5/5” claim is made.
- `git diff --check`: **PASS**.

CoreValidation’s originals and native `simulation-core-validation/v1` manifest remain in protected local E: evidence retention (manifest SHA-256 `24cc3bc7599b5dbce63e81d4f15fb60b390c1b4028abbd8bf5d40aa905fe104c`; cold TRX SHA-256 `e2340933431c514ce046c08109c09bfed6c746611e4cbf730d862d85e24c685f`; warm TRX SHA-256 `2aa1066306abf6b57e8ffa4e319638483667e9dc82d4f34c1c261261305137f8`). No public .NET TRX package is claimed: CoreValidation emits its native manifest, while the approved TRX packager requires a separate `dotnet-trx-source-context-v1`; no context was fabricated and no original was altered.

## Review and scope

Independent exact-tip implementation review is pending. This candidate does not promote Core canonical, close CORE-F, or claim a standalone .NET host or full independent runtime composition.
