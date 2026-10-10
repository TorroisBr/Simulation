# P12-G PoliticalClaim Office-target rejection — independent review

**Verdict:** PASS — exact-tip implementation review

**Canonical base:** `fb4f89a05f6d00114176e05df26968325605dc07`

**Candidate reviewed:** `55ec1cfa15a828f2d5ebca40422381944c0bedd6` on `codex/phase12/P12GPoliticalClaimTargetRejection`

**Code commit/tree:** `5813e3fbe1824e23c98ea69c0d598d9a66c0f664` / `c03566fa4c7547d98b40a3068ed2d9a432067912`

**Validated Assets tree:** `90bf5c9415e4feb7d96d23eaa47071e6fcfd2292`

**Reviewer:** `/root/p12g_inventory_reconciliation` (independent of implementation)

The candidate is a clean fast-forward from the canonical base. Its final descendant after code and validation evidence is docs-only; the reviewed Assets tree is unchanged.

The null-by-default test seam reaches PoliticalClaim staging after Institution/Office, Property/Estate, and Faction owners stage privately. The fixture preserves snapshot identity, schemas, counts, revisions, and all other row values while changing only the Office target to a well-formed ID absent from the exact staged Office root. Normal validation rejects before the Claim owner stage event.

The integrated test verifies the old session, completed-boundary token, health, facts, roots, and owner graph remain unchanged; the source continues in parity with its control; a valid retry succeeds; and parity holds after another boundary. Restored-continuation census requires Institution/Office witnesses to match exact live cardinalities, revisions, and owner identities. Fresh bootstrap retains its zero-cardinality requirement. The change is consistent with the owner inventory, where all four Institution/Office sections are Required.

The reviewer parsed the exact-tip validation XMLs: focused case 1/1, SimulationRuntimeAdmissionTests 180/180, InstitutionOfficeCensusTests 4/4, ALL EditMode 2854/2854, and official Smoke 5/5, each with zero failures, skips, or inconclusive tests. The reviewer could not recompute compressed-log byte hashes through the review connector; the coordinating checkout independently recomputed the committed XML, compressed-log, and decompressed-log SHA-256 values against the validation manifest, and all matched.

No `ProjectSettings` or `.meta` files are part of the candidate. This review passes only the bounded Office-target rejection and atomicity case; it does not close P12-G or claim P12-A/P13 readiness.
