# Phase 20 State — Multi-participant Activities v1

**Status:** OPEN — prior entry and technical designs reviewed; a
formation-close clarification is under independent refresh review; the first
implementation checkpoint remains proposed; no P20 capability is promoted.

**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`.

**Current prerequisite base:** `codex/phase18/canonical` at
`311baa930227371a807fb324ff11fc024800ddf9`. Its promoted relevant capabilities
are P18-A timeline/scheduler (`985c56c40fc01dc6a4d392120e2d32151a558d03`),
P18-B activity lifecycle (`97918cbbe4238a65a216b1a1f0ef84c70b4d080c`), and
P18-C availability/actor decisions (`ab05ecfe976e80badf6f509b8e9be25ff556ca23`).
The latest P18 State-only update changes no relevant P18 code or API. P18-D and
P19 are not blanket prerequisites for this Phase 20 proof.

## Work status

| Work | Status | Evidence / gate |
|---|---|---|
| Entry architecture | PRIOR VERSION REVIEWED — PASS; REFRESH PENDING | `docs/design/PHASE20_ENTRY_ARCHITECTURE.md`, prior candidate `2f9c93b588ffccaae60aedf6c16191c1251f6a1f`; current clarification aligns bounded formation-close timing and awaits independent refresh review. |
| Technical design | PRIOR VERSION REVIEWED — PASS; REFRESH PENDING | `docs/design/PHASE20_TECHNICAL_DESIGN.md`, prior candidate `6a0d16494735853ce35a8974ab348551650afd6b`; prior review was against promoted P18-A/B/C and State `bcb3f67`. Current P18 State `311baa9` records that review; this clarification awaits independent refresh review. |
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
an isolated worktree, independently review the code against the actual P18
contracts, run its focused and required regression gates, and request canonical
promotion separately. P18-D or P19 progress does not gate this checkpoint.
