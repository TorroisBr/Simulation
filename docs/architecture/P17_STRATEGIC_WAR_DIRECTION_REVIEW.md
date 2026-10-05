# Independent review — P17 strategic War direction and P17-A entry

**Verdict:** PASS. **Reviewed content tip:**
`f822e3f7cb24e4a67eed1481148edfd214f635ab` against canonical architecture
base `cddedfa41e72d9205d92411b5bedebf11ac0b16a`. **Review mode:**
independent, read-only architecture and technical-entry review; the reviewer
did not author the candidate. This record is review evidence, not canonical
promotion or P17 implementation approval.

The reviewer examined the complete four-document candidate diff, repository
lifecycle rules, P7/P12/P16 contracts, and current War ownership code. The
first pass found one minor lifecycle-vocabulary mismatch: the task-level
classification `READY_FOR_BOUNDED_CHECKPOINT` had been used as a repository
readiness state. The candidate was revised to use the defined
`READY_FOR_TECHNICAL_DESIGN` state for P17-A, then the complete diff was
re-reviewed at the content tip above with no remaining findings.

The accepted War model keeps territorial, attrition/capability and coercive
goals compatible; distinguishes strategic Faction participation from P7
operational force binding; retains one War authority; and separates goal
satisfaction, Battle outcome, military control and explicit War termination.
P17-A's withdrawal evidence is the retained P16-A receipt, with causal input
capture still required at the application boundary. The proposed P12
selected-profile exclusion or fail-closed admission gate avoids a blanket
full-P12 prerequisite. The direction identifies the raw P7 `TryEnd` bypass,
clone/guard/order/hydration needs, integration hotspots and a later Lab
demonstration. No durable product or semantic choice is left for an
implementation worker to invent; a bounded technical design and its own
independent review remain necessary.

The candidate changes documentation only. `git diff --check` passed; Unity
tests were not required. Architecture-canonical promotion remains a separate
human gate.
