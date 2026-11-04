# CORE-F selected Guard Arrest P12 scope

Status: validated implementation candidate on the existing CORE-F candidate branch. This is not Core canonical promotion or CORE-F completion.

## Scope delivered

- `DailyV1P12CrimeJusticeMutationCoordinator` now derives the selected Guard Arrest owner bindings and P12 section list from the exact admitted Core owner graph.
- The selected Unity dispatch path uses that Core scope. The existing Unity P12 scope path remains for callers without a selected Core graph.
- Guard/target owner identity, definitions identity, Justice owner presence, and target cardinality are checked before census admission. Invalid selected owners fault closed.
- The action ordering, sentence projection, status projection, and publication effects remain unchanged.
- The Unity regression now asserts the observed two NPC status writes (`Free` removal and `Arrested` addition). The shared mutation epoch remains monotonic because the operation includes other writes.

This slice does not complete the public `DailyV1Application.TryStart`, the full P12 detached restore and host startup path, or process-separated continuation. It does not claim full CORE-F validation or acceptance.

## Validation

Validated code commit: `29ff69ab4f954e17202ffe26fae1ae0b2ac698c0`
Code tree: `f3f1c98f920a1b9fd8a9160e25e66024501ed404`
Parent: `b44185a47683d98de70af220671b64e324f90208`

- Official CoreValidation: **1010/1010 cold and 1010/1010 warm**. Run `e0e6b79e99fd4639bf028f14602ddf8e`; private manifest SHA-256 `5d3d622e14c3b69ff3a53b9d95cdecbf5a50be86a18a923d1cd43fc6f112783`; cold TRX SHA-256 `6c84d251ca9eaa9256c97eb00c810529ba751c60a13799fc33dcb276305e3356`; warm TRX SHA-256 `90680311162d0fd455b215002a681b8a32421eeee2e0bab50efa12028cde86b5`.
- Official Unity EditMode focused test `SimulationRuntimeAdmissionTests.SelectedDailyV1GuardArrestDispatchRebuildsProjectionFromExactCoreSlot`: **1/1 PASS**. Run `ed6add3186ef48cca81cf724ebd12974`; `inputStable=true`; private manifest SHA-256 `7cd36c20db4ab674a7b084305978573a7ad23dffbbe9cf2c10ffa9bf8fc0c20b`; original XML SHA-256 `ed117b8ea0a69dda44e0462ad9625d1014c076ede296c8089edc5c25db2a69a5`; original log SHA-256 `22de9c7f88fe68ad7fcc0ed4fb6b42733c90e0f6c24aea07c5c8ca4caf84f79c`.
- Supplemental official Unity EditMode mixed-roster regression `SimulationRuntimeAdmissionTests.SelectedCrimeJusticeDailyStagePublishesP12WritesAndUsesWholeRosterCompatibilityFallback`: test XML **1/1 PASS**, run `e0ff62746f25422fb2a82d5497aa9ee4`, `inputStable=true`; private manifest SHA-256 `ff2f76418eb4704f9d2b8ed98619da87b2f8a6a1bc9694d4a779d64429c6dc57`; original XML SHA-256 `d778b80e56f3247382915181fcdfade390e3529069ce6fd13953617aa6200fde`; original log SHA-256 `bf603f55fefac6d8f6de80078e750a45659079173bc0dc06384d4f8a0e855882`. The tests completed and XML passed, but Unity remained alive after its shutdown log and the official owned-process cleanup was used; the manifest records process exit code `-1`. This is supplemental test evidence, not a clean-process-exit claim.
- `git diff --check` and the staged-diff check passed for the implementation commit. Three unrelated local `ProjectSettings` changes were not staged or committed.

The original reports and manifests remain in the private evidence store; they are not copied into this repository. Earlier focused runs with incorrect test expectations are also preserved and are not represented as passing runs.

## Limits

The result is an additive CORE-F candidate checkpoint. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. No formal CORE-F completion, canonical promotion, full Unity suite, Smoke, or long-run result is claimed.
