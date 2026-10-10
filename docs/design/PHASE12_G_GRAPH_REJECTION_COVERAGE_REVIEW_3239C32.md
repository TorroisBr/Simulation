# P12-G graph rejection coverage — exact-tip independent review

**Verdict:** `NEEDS_CHANGES` for the complete P12-G candidate; `PASS` for the incremental graph-rejection test delta.

- Canonical base: `3c1575530a1d44c3932767b6e8879be2e6672dc8`
- Candidate branch: `codex/phase12/P12GGraphRejectionCoverage`
- Candidate evidence tip: `e1796ad907545907109b4717815881fdf3f75945`
- Reviewed code commit: `3239c321070b94afd8d22ebd03a04e2151fe4a73`
- Reviewed Git tree: `6c9dd26bd8a078f893444f3628674228786d3b94`
- Reviewed `Assets` tree: `e60b27df5677c6116cb7e04aeed5582934d27866`
- Independent reviewer: `/root/p12g_failure_matrix`

## Incremental test delta

The code delta since `92206b2` is test-only in `SimulationRuntimeAdmissionTests.cs`. The added cases are valid and preserve the active source session:

- a second valid authoritative P8 Location is rejected for selected-profile cardinality;
- a City registry entry moved under an NPC-family key is rejected as typed allocator-family validation (not a separate global duplicate-ID detector);
- a transferred source boundary token injected after target admission is rejected before the candidate is returned;
- publication is rejected while an active-session operation is held, preserving the old session and allowing later continuation and restore retry.

## Exact validation evidence

All artifacts were independently rehashed and matched the candidate validation manifest:

- Focused `SimulationRuntimeAdmissionTests`: 113/113 PASS; XML SHA-256 `22E895FC37B65362147CAC11277D957F9D67DC6DC259EE60EF330717DC56562C`; raw log SHA-256 `A6285439F4E51F74CD97D680208AF8CBF693D2CA6F16329250662B779F27F5E2`.
- ALL EditMode: 2787/2787 PASS; XML SHA-256 `DEB711F12F398D911F6D76A57A7C7F9C45447316FE073899C8FCE7422DC3026D`; raw log SHA-256 `06A450B489292DBD126E33B71845AF516677DEB335E5635D21F4048B0112FDE2`.
- Official Smoke: 5/5 PASS; XML SHA-256 `6774529BE9F82EE37CE4750877D76EC645FC85BEA581C065888EAE1FD4B5D22F`; raw log SHA-256 `08D14D69108BAF32C582231A980B56B8640F0C370BFC71C72E46279BE7739457`.
- Archived raw-log ZIP SHA-256: `A0531502D1049CF73783358C72E14950398E48579C91B73F90FCA87672945FEC`.
- `git diff --check`: PASS.

## Remaining P12-G obligations

The complete P12-G contract is not satisfied. Review identified remaining evidence for the section/profile compatibility matrix; systematic B–F definition/provider/cross-section corruption and relation/cardinality cases; failure injection inside owner hydration, relation/commitment and global validation boundaries plus candidate lifecycle disposal; and exact P8/P9 lineage variants. The in-memory coordinator does not parse a serialized envelope; general serialization remains assigned to P12-A, so this candidate does not claim that capability or P12-A readiness.

This review found no defect in the incremental tests and does not certify or close P12-G. The candidate has no claim of P12-G completion, P12-A readiness, P13 readiness, capture eligibility, or Phase 12 closure. P12-G remains `WAIT_DEPENDENCY`; Phase 12 remains open.

Protected ProjectSettings edits and unrelated untracked `.meta` files were not staged or modified.
