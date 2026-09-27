# Phase 20 State — Multi-participant Activities v1

**Status:** OPEN — entry and technical designs, including the formation-close
clarification, passed review on their recorded bases. The current-base refresh
against P18-A continuation semantics awaits independent review. P20-A scope was
accepted by the user on 2026-09-27; no P20 capability is implemented or
promoted.

**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`.

**Current prerequisite base:** `codex/phase18/canonical` State tip
`eabc1c24a0ba8951ded87280472cc7137e741434`, with promoted code integration
`1dd0479626ddf00bf08aa66533f54fef7328a421`. Its promoted relevant capabilities
are P18-A timeline/scheduler plus additive continuation extension
(`1dd0479626ddf00bf08aa66533f54fef7328a421`),
P18-B activity lifecycle (`97918cbbe4238a65a216b1a1f0ef84c70b4d080c`), and
P18-C availability/actor decisions (`ab05ecfe976e80badf6f509b8e9be25ff556ca23`).
The continuation extension freezes an ordered manifest, resumes by stable
continuation/step identities, blocks ordinary same-instant work and advance
beyond the boundary until complete, publishes returned timeline facts before
ordinary same-instant work, and withholds P18-C source-signal handoff until
successful outer advance return. P20 start work is ordinary due work subject to
this barrier; P20 does not own or implement continuation. P18-D and P19 are not
blanket prerequisites for this Phase 20 proof.

**Current-base refresh:** P18 canonical advanced from the prior reviewed base
to State tip `eabc1c2` and code integration `1dd0479`, which promotes the
additive P18-A boundary continuation implementation. Existing P20 formation,
identity, and cardinality semantics remain compatible, but the entry/technical/
checkpoint documents now explicitly map P20 start work behind the continuation
barrier and record returned-fact publication and post-successful-advance
P18-C handoff. Independent review of this refreshed documentation is pending.
`ActivityInstanceId` remains separate from `PersonId`; exactly two Persons is
only the P20-A fixture policy and does not alter general one-or-more
cardinality. P20-A scope was accepted by the user on 2026-09-27; acceptance is
scope only, with no P20 capability or implementation until this refresh passes
independent review.

## Work status

| Work | Status | Evidence / gate |
|---|---|---|
| Entry architecture | PRIOR REVIEW PASS; CURRENT-BASE REFRESH PENDING REVIEW | `docs/design/PHASE20_ENTRY_ARCHITECTURE.md`; prior formation-close review at `5ea4283` against P18 State `311baa9`; refreshed for P18 State `eabc1c2` / code `1dd0479`. |
| Technical design | PRIOR REVIEW PASS; CURRENT-BASE REFRESH PENDING REVIEW | `docs/design/PHASE20_TECHNICAL_DESIGN.md`; prior technical/API and formation-close reviews at `6a0d164` / `5ea4283`; refreshed for continuation barrier/publication/handoff semantics at P18 State `eabc1c2` / code `1dd0479`. |
| P20-A — Synthetic Multi-participant Operation | SCOPE ACCEPTED; REFRESHED PROPOSAL PENDING REVIEW | `docs/design/PHASE20_P20A_CHECKPOINT_PROPOSAL.md`. User accepted scope on 2026-09-27; exactly two distinct PersonIds remains this fixture's policy only. Implementation waits for current-base independent documentation review. |
| Runtime capability / Phase closure | NOT PROMOTED / OPEN | No P20 code, runtime capability, or closure marker is claimed. |

Both architecture alignment records remain active constraints:
`docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
`docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`. Preserve logical
time and deterministic causal order, stable semantic identity, and
`ActivityInstanceId` independent from `PersonId`; do not derive a fixed general
participant count from the two-Person proof. Defer public Mod API/loader work to
P19. No gameplay consumer is included.

## Next gate

The accepted P20-A checkpoint authorizes only its bounded synthetic proof.
Implementation remains paused until independent review approves this current-
base documentation refresh. Then implement in an isolated worktree, run
focused validation, and submit the candidate for independent review against the
actual P18 contracts. After review passes, run
the required integration/regression gates and request canonical promotion
separately. P18-D or P19 progress does not gate this checkpoint.
