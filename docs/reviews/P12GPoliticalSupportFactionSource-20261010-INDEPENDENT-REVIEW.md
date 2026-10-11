# P12-G PoliticalSupport Faction-source rejection — independent review

**Verdict:** PASS — exact-tip implementation review

**Canonical base:** `870fdeddc5fbeea2625c61a81d9142b41f3ac1ea`

**Candidate reviewed:** `0c60a7869fb31b46965d1eac888cdddb11225c49` on `codex/phase12/P12GPoliticalSupportFactionSource`

**Code commit/tree:** `9d1100f0c07c5faf662b6241579cabb0f38f7cf9` / `99a15ab6693bcc2238eb7e1a2200bd1f9038c14d`

**Validated Assets tree:** `ca0df3c40caf184f0fcbcaae4866aae82094c74b`

**Reviewer:** `/root/p12g_inventory_reconciliation` (independent of implementation)

The candidate is a clean two-commit fast-forward from the canonical base; its code commit is the direct child of that base and its descendant adds only validation evidence. The code diff changes only the restored-versus-fresh Faction census guard in `SimulationRuntime` and one integrated `SimulationRuntimeAdmissionTests` case. For a restored continuation, the required Faction and affiliation providers retain exact identity, schema, provider count, actual cardinality, shared owner identity, and revision checks. Exact-zero cardinality remains required for fresh bootstrap.

The test stages one populated Faction, a valid `StatusRecognition` PoliticalClaim, and a Faction-sourced PoliticalSupport relation targeting that Claim. Its detached restore override changes only the source `FactionId` to a distinct, well-formed ID absent from the exact staged Faction section. It preserves schema, row count, shared local revision, relation identity, source kind, Claim target, disposition, and temporal fields. The stage observer confirms earlier E owners, Faction, and PoliticalClaims staged before ordinary PoliticalSupport validation rejects, before the PoliticalSupport owner stage/publication. Assertions cover active session/token/health/facts/roots/owner graph preservation, deterministic continuation against control, successful retry of the untouched snapshot, exact populated-Faction retention, and parity after another boundary.

The review found no scope expansion or unrelated changes. `ProjectSettings` and the unrelated untracked `.meta` files are outside the candidate diff. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P12-B remains `INCOMPLETE`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. This review does not claim complete owner/epoch coverage, quiescence, capture eligibility, export/hydration readiness, P12-A/P13 readiness, or Phase closure.

The validation manifest records focused `1/1`, admission `183/183`, Faction census `5/5`, Faction snapshot `12/12`, PoliticalSupport snapshot `6/6`, ALL EditMode `2857/2857`, official Smoke `5/5`, all with zero failures, skips, or inconclusive tests, plus `git diff --check` PASS. The reviewer independently parsed the accessible XMLs and checked their hashes and the accessible compressed-log hashes. The coordinating checkout locally recomputed XML, compressed-log, and decompressed-log hashes for all seven retained validation runs, including the larger artifacts that the review connector could not return; all matched `docs/validation/P12GPoliticalSupportFactionSource-20261010/VALIDATION.md`. The code and validated Assets trees remained unchanged after validation.