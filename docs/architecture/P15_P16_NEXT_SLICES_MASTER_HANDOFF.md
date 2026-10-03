# Master handoff — bounded P15/P16 next slices and parallel design

**Architecture baseline:** `codex/architecture/world-identity-projection` planning content promoted at `a2788e6400251b5ce3cf8d2269ea8a5b5fdfd4a8` from base `da34d50bd7831ac3eefab31e925492ede8dded5c` (2026-10-03). Recheck refs, States and code when scheduling; the active P12-B worktree remains an independent stream. The independent review [record](P15_P16_NEXT_SLICES_REVIEW_RECORD.md) validates content tip `3ca00b8a74da2cdab807ec4dc5f8ec95454645b7` and the final metadata delta.

## Ready set and gates

| Track | Candidate classification | Master action after canonical promotion | Implementation/canonical gate |
|---|---|---|---|
| P15-A — one structure at existing Location | `READY_FOR_MASTER_IMPLEMENTATION_HANDOFF` after candidate promotion; bounded technical design independently reviewed PASS in [the checkpoint record](../design/PHASE15A_RUNTIME_STRUCTURE_CHECKPOINT.md) | Prepare an isolated implementation candidate only after design review/promotion; preserve one structure owner and explicit separate proving profile. | Promoted P8-A Location, current-base implementation review, P12 daily-profile exclusion/admission proof, serial `SimulationRuntime`/P12-B hotspot integration before domain promotion. No material debit in this inert fixture. |
| P16-A — one force, one passage, finite supply | `READY_FOR_MASTER_IMPLEMENTATION_HANDOFF` after candidate promotion; bounded technical design independently reviewed PASS in [the checkpoint record](../design/PHASE16A_SINGLE_PASSAGE_MOVEMENT_CHECKPOINT.md) | Prepare an isolated implementation candidate only after design review/promotion; the proving profile binds one selected `ArmedForceId`, rejects all other forces and permits one successful crossing total; position, carried supply and its receipt remain in the existing military owner. | Promoted P7/P8-A/B, bounded P14-compatible item/finite-stock semantics, current-base review, negative P12 admission test for new state in an already-censused owner, serial P12-B integration before domain promotion. |
| P13 retention/causal-input design | `P13_RETENTION_CAUSAL_INPUT_DESIGN_READY` for design only | Use [the design](../design/PHASE13_RETENTION_CAUSAL_INPUT_DESIGN.md) to prepare a future exact-profile technical design. | Authoritative reconstruction/fork `WAIT_DEPENDENCY` on complete chosen-profile continuation, recoverable history, compatible execution and WorldId fork provenance. |
| P19 public-extension-surface design | `P19_PUBLIC_EXTENSION_SURFACE_DESIGN_READY` for design only | Use [the design](../design/PHASE19_PUBLIC_EXTENSION_SURFACE_DESIGN.md) to scope a real new-world generation contributor contract first. | Loader and durable module-state implementation remain deferred and need their own consumer/technical review and compatible continuation/history. |
| P10 / P14 / P20 next slices | `READY_FOR_PRODUCT_SCOPE_DECISION` | Present the [decision packet](P10_P14_P20_NEXT_SCOPE_DECISIONS.md); do not dispatch a follow-on. | Human chooses bounded gameplay scope; then design/review and promoted prerequisites. |

The technical-design records are not runtime capabilities. The independent technical review passed at `3ca00b8`; `READY_FOR_MASTER_IMPLEMENTATION_HANDOFF` is effective after architecture promotion at `a2788e6`. The Master checks current code, profile admission, isolation and conflict windows before implementation dispatch.

## Dependency and hotspot ownership

```text
P8-A + runtime guard → P15-A
P7 + P8-A/B + P14-compatible finite item semantics → P16-A
P18 → only later timed versions; P20 → only later crew/shared versions
P12 profile exclusion/rejection → each domain promotion
P12 exact chosen-profile continuation + recoverable causal history → P13 fork
P9 genesis contributor seam → first P19 public-contract design
P10-A / P14-A / P20-A → next product choices
```

P15-A writes a new `StructureStore` and reads `SpatialAuthorityStore`; P16-A changes `ArmedForceSpatialStateStore` and reads spatial passage facts. Both may touch `SimulationRuntime` and selected profile factories/guards. P16-A's new carried-supply/receipt state changes an owner already visible to P12, so absence of a passage in `UnityBootstrap-Daily-v1` alone does not establish exclusion. Serialize these integration windows with P12-B; keep independent design/implementation branches isolated. P14-A City market stock is not a construction debit or force supply owner. P18 and P20 stay unchanged. P17 remains deferred.

## Reconstruction and evidence

P15-A retains ID, definition/revision, Location link, creation boundary and accepted causal order. P16-A retains the immutable selected-force binding, force position, finite supply item/quantity, one successful-crossing receipt with operation ID, passage and fixed P8 evaluation context/revision and accepted causal order. External inputs to a durable world additionally retain payload and authority. Both expose exact semantic-state and private staged-hydration/validation seams for future P12/P13 work without implementing Save or Replay now. P16-A has no general multi-move ordering policy; a second crossing, including at the same boundary, rejects unchanged. Tests must establish rejected mutation leaves records, indexes and revisions unchanged, deterministic clone/order, future fork before/after behavior and selected P12 profile fail-closed behavior. Documentation changes alone require diff/link/review checks, no Unity test.

Architecture-canonical promotion was approved and completed. Future domain checkpoint promotions retain their own implementation review and human gates under `AGENTS.md` and `EXECUTION_MODEL.md`. No closed Phase is reopened. P12-B continues on its own stream.
