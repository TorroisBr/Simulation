# P12-G ScheduledDirective + TravelParty continuation parity — independent review

**Verdict: `VALIDATED_CANDIDATE`**

- Reviewed candidate tip: `f07a05e15521f34b699b9156406fb71d367a4de2`
- Candidate base / current P12 canonical: `4804fd7b732f6069083d8c413593eff004173569`
- Candidate branch: `codex/phase12/P12GDirectiveTravelContinuationParity`
- Code commit: `66679ec607b29ab21cb6003966ff8112c84d5908`
- Code Git tree: `d8cc2f729d5b7ab32e420b96bc83e9dc0f995d53`
- Validated `Assets` tree: `4baa557a00f14c71abcdcd51ea430c2c773695fc`
- Review date: 2026-10-11

The independent review verified the full candidate diff against its actual canonical base. The only executable change is an EditMode continuation test in `SimulationRuntimeAdmissionTests.cs`; no production behavior, runtime seam, package, or unrelated user file changes. The test restores at the completed day-1 boundary, checks the complete included-owner projection and continuation roots, then compares restored and uninterrupted projections at each following boundary through day-5 directive processing and TravelParty arrival.

The directive targets an unarrested NPC. The ordinary eligibility path produces no `EscapePrison` action and transitions the directive to terminal `Skipped` on day 5. The corrected State and validation manifest accurately claim parity through this skip and party arrival; they do not claim successful dispatch or action-effect parity. The evidence is one combined fixture and does not close the broader compatibility/cross-section matrix, other failure-path coverage, universal causal no-replay, or all-owner multi-boundary parity.

The retained exact-tree reports passed: focused case 1/1, `SimulationRuntimeAdmissionTests` 190/190, ALL EditMode 2864/2864, and official Smoke 5/5, all with zero failures, skips, and inconclusive tests. Report and log hashes match the validation manifest and the validated `Assets` tree. `git diff --check` passes. The documentation-only correction after code validation did not change executable files or the `Assets` tree, so Unity validation did not need to be repeated.

No product or architecture decision is required. This is a test-evidence candidate under the already accepted P12-G scope. It changes no dependency edge or readiness label; P12-G and P12-A remain `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains `OPEN`. No complete owner/shared-epoch coverage, runtime-wide quiescence, capture eligibility, export/hydration, or Phase closure is implied.
