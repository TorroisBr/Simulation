# P16-A current-base implementation review

**Result:** `VALIDATED_CANDIDATE`. **Checkpoint:** P16-A, One Passage Military Movement with Finite Supply. **Reviewed code commit:** `98f80648a226212cd13c37bce34d0e2d6c68574a`. **Reviewed executable tree:** `1abb2f83817659fa9bce8e66826f79fde34765b8`. **Candidate evidence tip:** `b1127a34cdcc1cd214f25f23e91f1118eb6f9cf5`; this commit adds validation documentation after the reviewed code commit and does not change the executable tree. **Independent reviewer:** Luna review agent, read-only, 2026-10-04.

## Base and current authorities

The P16 implementation candidate was built on P15 canonical `5054211ad883d14fc6727416c313f1f1824679f4` and P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`. Its current-base integration commit `98f80648a226212cd13c37bce34d0e2d6c68574a` has first parent `2c53f8d2f7e9c6d4629b2401fe6ac6509c789030` (the P16 candidate before P14 refresh) and second parent P14 canonical `06e9c30101a74bd618d3651885c489c79fe866bb`, which includes the P10-B/P14-B integration. The merge base is P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.

At review time, the relevant promoted authorities were P10 canonical `e53252de5277fd5af46bbacb8eda5ee6e74aff08`, P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`, P14 canonical `06e9c30101a74bd618d3651885c489c79fe866bb`, P15 canonical `5054211ad883d14fc6727416c313f1f1824679f4`, and architecture canonical `f6924e63d8e5731da1d33021d0361e7defe6dad7` on `codex/architecture/world-identity-projection`.

The P15/P16 architecture-planning candidate at `a2788e6400251b5ce3cf8d2269ea8a5b5fdfd4a8` was promoted into `codex/architecture/world-identity-projection`; current architecture canonical `f6924e63d8e5731da1d33021d0361e7defe6dad7` descends from that promotion. The current Phase 16 Brief records P16-A as `READY_FOR_MASTER_IMPLEMENTATION_HANDOFF`. The planning promotion is durable and is not an outstanding gate.

## Review outcome

The reviewer inspected the exact current-base code diff and the P16-A checkpoint contract, Phase 16 brief, current architecture, relevant P7/P8/P10/P12/P14/P15 code and States, validation manifest, and retained test artifacts. **PASS; no actionable findings.** The reviewed behavior remains within the approved single-force, single-passage, one-success, finite-carried-supply boundary.

The review confirmed:

- `ArmedForceSpatialStateStore` remains the sole position/supply owner; movement and debit commit atomically with the one-shot receipt and revision.
- The selected-force binding, fixed P8 traversal context, passage/revision revalidation, target day/order, zero-stock behavior, owner-thread guard, and non-reentrant runtime lease match the reviewed contract.
- Rejected, duplicate, stale, unauthorized-force, and insufficient-supply paths preserve position, supply, receipt, and revision.
- P18 intraday movement remains excluded; P16-A does not add route planning, duration, Person availability, Battle/War/occupation effects, production gameplay content, or serialization.
- The P12 Daily profile rejects populated P16 state before publication. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.
- P10-B and P14-B code compose without production-file overlap. The sole shared test file merges cleanly, and the combined P14 City/P10 Ruin proving profile still rejects before identity allocation or publication, preserving P8 one-owner-per-Location.

No review finding requires code changes or additional tests. The reviewer relied on the archived exact-tree validation evidence and did not rerun Unity.

## Exact-tree validation

The validation ran on code commit `98f80648a226212cd13c37bce34d0e2d6c68574a`, tree `1abb2f83817659fa9bce8e66826f79fde34765b8`, using Unity `6000.3.9f1`. The manifest and raw artifacts are in `docs/validation/P16A/P16A-current-base-revalidation-1abb2f8.md` and `P16A-current-base-revalidation-1abb2f8.zip` (SHA-256 `5C7C6D878EB880A5879A558AD52370144C782413F05E8F3904097E6FCDD09BFE`).

| Gate | Result |
|---|---:|
| `P16AMilitaryMovementTests` | 20/20 PASS |
| `SimulationRuntimeAdmissionTests` | 37/37 PASS |
| `SimulationBootstrapCompositionTests` | 21/21 PASS |
| ALL EditMode | 2323/2323 PASS |
| Official Smoke | 5/5 PASS |
| `git diff --check` | PASS |

Every suite had zero failed, skipped, or inconclusive tests. The manifest records per-suite XML and log hashes; the archive hash matches.

## Readiness and integration constraints

This is an independently validated implementation candidate, not a P16 canonical promotion or Phase closure. P16-A's accepted planning and technical design are canonical under architecture tip `f6924e63d8e5731da1d33021d0361e7defe6dad7`. The named P16 canonical branch does not yet exist. Run the current-ref bounded-promotion preflight before creating it; no additional product or architecture decision is outstanding for this slice.

After any architecture-planning promotion, refresh all refs and reclassify this candidate. If its approved P16-A contract and prerequisites are canonical and the reviewed executable tree is still unchanged, the standing bounded checkpoint-promotion policy applies after required exact-base integration checks. Preserve P12-B incomplete, P12-A `WAIT_DEPENDENCY`, and P13 blocked; this slice establishes no complete census/shared-epoch coverage, global quiescence, capture eligibility, export, or hydration.
