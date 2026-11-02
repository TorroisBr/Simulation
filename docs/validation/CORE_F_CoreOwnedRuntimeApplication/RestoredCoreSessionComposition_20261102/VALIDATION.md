# CORE-F Restored Core Session Composition Validation

Date: 2026-11-02
Candidate base: `1ac94f5d1c143a2393e44a28f0875dcd743a5c86`
Checkpoint scope: the Core application now derives the restored continuation admission identity and composes/retains the active Core session from the exact ordered image C/D/E/F staging token. The Unity restore coordinator receives that session for its compatibility projection. The separate capture-bound path and the `application == null` path retain their prior composition routes.

This checkpoint does not complete `DailyV1Application.TryStart`, move the Unity `SimulationRuntime` projection into Core, or claim completion of detached .NET hosting or CORE-F.

## Source identity

The official validation input snapshots remained stable at Unity digest `8fa1e4c3d9bc2bc3ff8791eaff8f86fd15eee0e2bb1e43b1e93c04e28f90fbc6` for the focused, full EditMode, and Smoke runs. Changed production-source SHA-256 values in that snapshot:

- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`: `dea31f9b4fc1c8666627b97bddf5ce1bb220eba42da2d058264fd6c1f52dd63a`
- `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs`: `cf4a66e0a0f1e37d4c1d654e854b1f3139763656ff472c41ce3fbf5ba0a7d04d`
- `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs`: `f711a9f603ca09d074dcc7bc09cb3ad539cb43dae3bc6e5ff3d65328669a77dc`

## Validation results

| Gate | Result | Run identity |
|---|---:|---|
| Official CoreValidation, cold | 988/988 passed | `2b50794e36cd4dc496e65313f920f871` |
| Official CoreValidation, warm | 988/988 passed | `2b50794e36cd4dc496e65313f920f871` |
| Focused Unity P12-G image-restore suite | 16/16 passed | `38c306cbeadc48c9b017b1101555578b` |
| Full Unity EditMode | 3294/3294 passed | `c3c5b674759643f88bde9879bb47e09e` |
| Official Unity Smoke subset | 5/5 passed | `57604904ba3e4061a313e4a6a37e8f33` |
| `git diff --check` | PASS | candidate worktree |

The three Unity runs used the same input digest. The full EditMode original XML SHA-256 is `e44a339e6a0b3bb95a59d0efdfcda7657dcd213a9e3434ea4f5da6e56070b532`. The focused report's original XML SHA-256 is `adb0688967655ed7104d193980d15dcbb05fc7583567911b51d625131bc76dc2`; Smoke report SHA-256 is `969472ceb500faa57097bbe886ba16c39e4f9b6686ef4874d7bc5e0ad9d2bdd2`.

The official full EditMode result is packaged at `coref-restored-core-session-composition-all-20261102.zip`. The official package verifier returned `VALID` (one complete normalized XML result, 3294 cases, no failures, skipped, or inconclusive results). Archive SHA-256: `b7e727611d9473f48fd934f457e9ffaf2f99003eca3296faf5b75d2108dcc593`. Package manifest SHA-256: `2d48e4a8e9b65a0c6ad359da4bab3bb04adcc53a178fb6f540552a82090fc819`. The package uses the approved local-path redaction and output-removal policy.

CoreValidation used the official runner with `-SkipLab`; it reports that this checkout contains a Unity `Library`, so no Library-independent build claim is made. Its cold/warm originals remain in the private E: evidence store. The runner emits `simulation-core-validation/v1`; this workflow has no approved public adapter for `dotnet-trx-source-context-v1`, so no public Core TRX package is claimed. Private manifest SHA-256: `c0aead107aa0f4c9cba1d18ce6fafac479799ce1fafeada4405e45d39e44c36a`.
