# P12-G PoliticalClaim Recognition→Institution rejection — independent review

**Verdict:** PASS — exact-tip implementation review

**Canonical base:** `1232716829900e2985dcf4a43bdb028d95b81ae2`

**Candidate reviewed:** `821e5a9e002984345b77faf11f7d32c16b7bd855` on `codex/phase12/P12GPoliticalClaimTargetRejection`

**Code commit/tree:** `51d68c9be01f864df73641435e6781be04ebe00e` / `bbb01fb7ce8643c5ddf5885d38c96c8278325485`

**Validated Assets tree:** `f4b593c9c8a1acb945f0b0f3273c782ebae07e9f`

**Reviewer:** `/root/p12g_inventory_reconciliation` (independent of implementation)

The candidate is a clean two-commit fast-forward from canonical. The code commit adds only the integrated admission test; its descendant adds only validation artifacts. The test creates a valid `StatusRecognition` claim against an existing Person and a recognition by an existing Institution. At the detached P12-E snapshot boundary it changes only the recognizing Institution identity to a distinct well-formed ID absent from the exact staged InstitutionStore, recomputing `RecognitionId` with the existing composed-key function. It preserves both section schemas, row counts, shared revision, recognition state/day/reason, and complete history. The normal validator rejects the relation after Institution/Office, Property/Estate, and Faction staging and before PoliticalClaim owner staging or publication.

The integrated assertions preserve the active source session, completed-boundary token, health, facts, roots, owner graph, recognition identity/history, and source/control continuation parity. The untouched source snapshot retries successfully, and the restored runtime remains in parity after a further boundary. This is one P12-G relation-rejection and failure-atomicity evidence case only; it does not claim complete graph validation, owner/epoch coverage, no-replay, P12-G readiness, or P12-A/P13 readiness.

The reviewer parsed validation XML totals: focused `1/1`, `SimulationRuntimeAdmissionTests` `181/181`, ALL EditMode `2855/2855`, and official Smoke `5/5`, all with zero failures, skips, or inconclusive tests. Artifact hashes are recorded in the validation manifest and were independently recomputed by the coordinating checkout; the reviewer connector did not expose compressed log bytes for a second recomputation. No production source, ProjectSettings, or unrelated `.meta` paths are included.
