# P12-G PoliticalClaim Property-target rejection — independent review

**Verdict:** PASS — exact-tip implementation review

**Canonical base:** `229e2635e390564c7b732e65e4aa639b531d74db`

**Candidate reviewed:** `6edb32e8ad8d47467082dc6eee3bb9fd6d502e84` on `codex/phase12/P12GPoliticalClaimTargetRejection`

**Code commit/tree:** `252facf665077a5a2ab583c60a08809f308836ba` / `1a87fc09e015babbb0e1bd3e4315c5bb07c4d8ce`

**Validated Assets tree:** `7f8925768283f34da8ee42f78d3828c33ab132d8`

**Reviewer:** `/root/p12g_inventory_reconciliation` (independent of implementation)

The candidate is a clean two-commit fast-forward from canonical. The code diff adds only an integrated `SimulationRuntimeAdmissionTests` case; the descendant adds only validation evidence. The test constructs a populated `PropertyEntitlement` claim with a registered target Property, then at the detached snapshot seam changes only `TargetId` to a distinct well-formed Property identity absent from the exact staged PropertyStore. Section IDs/schema versions, both cardinalities, their shared revision, and every other claim field are preserved.

The stage observer confirms Institution/Office, Property/Estate, and Faction have staged before normal typed-reference validation rejects the Claim, before PoliticalClaim or PoliticalSupport owner-stage events/publication. Assertions preserve the active session, completed token, health, facts, roots, and owner graph; source continuation matches the uninterrupted control; an untouched retry succeeds; and restored/control parity holds after another boundary. This exercises one existing P12-E typed-reference rejection path, with no production API changes and no claim of complete graph coverage/readiness.

Validation XML roots report focused `1/1`, `SimulationRuntimeAdmissionTests` `182/182`, ALL EditMode `2856/2856`, and official Smoke `5/5`, all passed with zero failures, skips, or inconclusive tests. The independent reviewer confirmed focused/admission/Smoke XML counts; its connector could not return the large ALL XML or compressed logs. The coordinating checkout recomputed the XML, compressed-log, and raw-log hashes in the validation manifest. No `ProjectSettings` or unrelated `.meta` path is part of the candidate.
