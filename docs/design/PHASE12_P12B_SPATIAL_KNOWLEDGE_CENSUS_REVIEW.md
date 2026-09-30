# P12-B SpatialKnowledge census design review

- **Canonical base:** `81ddfe4bd0620b1b61a0a52aa074ef2ed57c2833`
- **Reviewed candidate:** `cfcc2fe1116d7f557ff958434a1218caedd15607`
- **Review:** Independent technical review, exact-tip PASS.
- **Validation:** `git diff --check 81ddfe4..cfcc2fe` passed; no tests were run
  for this documentation-only candidate.

The review confirmed that two per-NPC location/route witnesses, the read-only
collection-view correction, and rejection before revision saturation are
bounded P12-B live-owner census evidence. They do not implement P12-F causal
Knowledge export or staged hydration, so the P12-C/D/E-before-F dependency
remains intact. The accepted P12-B–P12-G prerequisite authorization covers
this slice; no new checkpoint acceptance is required.

The review also confirmed that outer-operation invalidation compares final
owner identity, cardinality, and revision against the baseline. A compensation
that leaves revision-visible drift requires refreshing the complete changed
section set once; exact witness restoration requires no refresh. The design
does not wire the protocol, enable capture, or clear P12-B's other blockers.
