# Phase 20 State — Multi-participant Activities v1

**Status:** OPEN — entry and technical designs, including the formation-close
clarification, passed independent refresh review; the first implementation
checkpoint remains proposed; no P20 capability is promoted.

**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`.

**Current prerequisite base:** `codex/phase18/canonical` at
`99cac77f7d66e8eb61fe68efb6959a4a5b7029ca`. Its promoted relevant capabilities
are P18-A timeline/scheduler (`985c56c40fc01dc6a4d392120e2d32151a558d03`),
P18-B activity lifecycle (`97918cbbe4238a65a216b1a1f0ef84c70b4d080c`), and
P18-C availability/actor decisions (`ab05ecfe976e80badf6f509b8e9be25ff556ca23`).
The latest P18 State-only update changes no relevant P18 code or API. P18-D and
P19 are not blanket prerequisites for this Phase 20 proof.

**Current-base refresh:** the proposal's prior review baseline `311baa9` was
advanced to `99cac77`. Independent impact review classifies this as
`UPSTREAM_IRRELEVANT`: the update changes only standing agent instructions and
workflow skills; architecture, Roadmap, P18 code/State and both alignment
records are unchanged. The proposal branch includes current P18 canonical as
an ancestor. Existing entry, technical-design, and formation-close reviews
remain semantically applicable. `ActivityInstanceId` remains separate from
`PersonId`; the exactly-two-Person fixture is still only fixture cardinality,
not a universal contract. P20-A remains proposed and awaits explicit
checkpoint acceptance.

## Work status

| Work | Status | Evidence / gate |
|---|---|---|
| Entry architecture | REVIEWED — PASS | `docs/design/PHASE20_ENTRY_ARCHITECTURE.md`; formation-close refresh reviewed at docs candidate `5ea4283b0c0dc15336c3eed477dd24ef276897f4` against architecture `c285466`, current P18 State `311baa9`, and both alignment records. |
| Technical design | REVIEWED — PASS | `docs/design/PHASE20_TECHNICAL_DESIGN.md`; prior technical/API review at `6a0d16494735853ce35a8974ab348551650afd6b`, with formation-close refresh reviewed at docs candidate `5ea4283b0c0dc15336c3eed477dd24ef276897f4` against current P18 State `311baa9` and both alignment records. |
| P20-A — Synthetic Multi-participant Operation | SCOPE PROPOSED; NOT ACCEPTED | `docs/design/PHASE20_P20A_CHECKPOINT_PROPOSAL.md`. Proposed ID only. Explicit checkpoint acceptance is still required before implementation. |
| Runtime capability / Phase closure | NOT PROMOTED / OPEN | No P20 code, runtime capability, or closure marker is claimed. |

Both architecture alignment records remain active constraints:
`docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
`docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`. Preserve logical
time and deterministic causal order, stable semantic identity, and
`ActivityInstanceId` independent from `PersonId`; do not derive a fixed general
participant count from the two-Person proof. Defer public Mod API/loader work to
P19. No gameplay consumer is included.

## Next gate

The only P20 checkpoint currently proposed is P20-A. Its acceptance would
authorize that bounded synthetic proof alone. Until acceptance, implementation
is not authorized. After acceptance, refresh the canonical base, implement in
an isolated worktree, run focused validation, and submit the candidate for
independent review against the actual P18 contracts. After review passes, run
the required integration/regression gates and request canonical promotion
separately. P18-D or P19 progress does not gate this checkpoint.
