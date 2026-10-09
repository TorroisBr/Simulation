# P12-F current-base design review — 2026-10-09

**Verdict: PASS.** This is an independent, read-only technical-design review. It does not review or promote implementation code.

## Exact review inputs

- P12 canonical base: `0619a33cd4287d89bad80fbe546763aff8f2a75b`
- Architecture baseline: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Design candidate: `fd40d2e53d4a73ee343e932869ed3510fa66563c`
- Design document blob: `69622d3b966c85ae40ec92f1a611bee7aed6f552`
- Candidate State blob: `1bd0cb75082cea5a19b758cdf377fadbca6bdf24`
- Design: [PHASE12_F_CURRENT_BASE_DESIGN_REVALIDATION_0619A33.md](PHASE12_F_CURRENT_BASE_DESIGN_REVALIDATION_0619A33.md)

The candidate was reviewed in full against current code, the accepted P12 Brief/decomposition, canonical P12 State, architecture §§2, 91A/B, 92 and 92A, and the intraday/extensibility and multi-participant alignment records.

## Findings

The final NPC seam is consistent with source ownership: P12-D captures one paired D/F view and stages one `NpcRuntime`; P12-F consumes only detached immutable `P12DNpcFRow` values. `P12DNpcFProjection` and its evidence remain transient because the evidence retains source NPC references. Token, owner-vector, and staging-attempt references may remain in temporary private C/D/E composition metadata through graph checks, but are excluded from F owner values, serialized data, and published runtime. TravelParty links are validated against detached F rows and D-staged NPCs.

The reviewed design preserves the accepted F owner set, dynamic NPC/Person cardinality, owner-local revisions, exact-zero LocalTopology boundary, TravelParty-before-D ID staging, terminal P11 ActorChoice history and idempotency, P18 temporal exclusion, typed P12-E unresolved bindings for G, and semantic identity/time handling. No scope or dependency edge changes.

Earlier exact-tip review attempts were NEEDS_CHANGES: candidate `f4c2a9e` retained the projection/evidence, and candidate `87a10d0` had one stale owner-table sentence. The design was corrected through candidates `6df6208`, `4d09701`, and final `fd40d2e`; this PASS applies only to the exact final design blob above.

## Readiness

P12-B/C/D/E are promoted within their recorded bounded scopes; F's C/D/E dependencies are satisfied. The accepted P12 prerequisite capability scope and implementation authority from 2026-09-27 cover P12-F, so the current-base design is **READY_FOR_IMPLEMENTATION** without another scope-acceptance gate.

This does not promote P12-F code or imply P12-A, P12-G, P13, whole-profile continuation, capture eligibility, or Phase 12 closure. P12-A remains `WAIT_DEPENDENCY`, P12-G remains `WAIT_DEPENDENCY` on B–F and validated inventory, P13 remains blocked by its reconstruction prerequisites, and Phase 12 remains open.

No Unity tests apply to this documentation-only design review. `git diff --check` passed.