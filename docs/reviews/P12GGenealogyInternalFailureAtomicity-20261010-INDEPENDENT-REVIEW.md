# P12-G Genealogy internal partial-stage failure — exact-tip independent review

**Verdict:** VALIDATED_CANDIDATE
**Review date:** 2026-10-10
**Candidate branch:** `codex/phase12/P12GGenealogyInternalFailureAtomicity`
**Candidate tip:** `888decd21fc0ed169b837e10a21d173206107100`
**Base and current P12 canonical:** `f5f1a247bf72b8d1cfdffe62f3486989e4692d5c`
**Implementation commit:** `45692c94c6f22fb3798cc246a89007d151930315`
**Implementation Git tree:** `23727c85a540bf30f595875294532e5080ce3c9f`
**Validated Assets tree (manifest):** `a3b896bb1e2ff3005be642557780ea563ed98f88`

## Review findings

The candidate is a two-commit clean fast-forward from the stated base. The current canonical ref resolves to the base; the candidate branch resolves to the requested exact tip. Its diff is limited to `GenealogyStore.cs`, `P12DDailyV1OwnerPackage.cs`, `P12GDailyV1RestoreCoordinator.cs`, the existing `SimulationRuntimeAdmissionTests.cs`, and the scoped validation artifacts/manifest. No ProjectSettings or unrelated files are changed.

The internal staging hook invokes the optional callback only after adding a validated parentage record and both adjacency entries to the private staged Genealogy store. The coordinator only supplies that callback when the existing stage observer is non-null; the normal production path with no observer passes null through and retains its prior behavior. The existing outer restore boundary catches `InvalidOperationException`, returns a failed private composition diagnostic, and does not publish the candidate.

The added parameterized case constructs three Persons and two valid Genealogy edges in both source and control runtimes. At the new cutpoint, the observer throws immediately after the first staged edge, so the private store has received one of two edges. The test asserts the restore rejects, the same source session/token and healthy owner boundary remain intact, included-owner projection and source facts remain unchanged, source continuation matches the uninterrupted control, and a later retry reconstructs the same continued graph and can advance again. This is one injected thrown failure after partial private Genealogy hydration; it does not claim coverage of all false-return branches, every internal Genealogy failure, other hydrators, or the complete §6.4 matrix.

The focused XML was fetched and inspected directly: 176/176 pass, zero failures/skips/inconclusive; the parameterized atomicity fixture is 56/56 pass and case (56) passes. Its committed blob SHA is `b5d932040c4a0f5e9d5b612cb022bf440608bb81`. The Smoke XML was also directly inspected and reports 5/5 pass; blob SHA `abceed38ddb70300cc82db9139e5e39d0ffb646b`. The committed validation manifest reports ALL EditMode 2850/2850, zero failures/skips/inconclusive, and `git diff --check` PASS. It lists these XML SHA-256 values:

- Focused XML: `E5CA00175EF307D7EF4C9F2229218E84D81ECB650A491A50282335508753B68D`
- ALL EditMode XML: `79DBB3BADD7E1985DEA5F36D0E4D5A9C88CCCB72C519F65BAB0E2262FF10E9EA`
- Smoke XML: `6C1F7E6C8D4B565C8FC50BD9863CBAEE37D05358D58333F7A336ABA4D3E26535`

It also records compressed and uncompressed log SHA-256 values for all three gates. The manifest ties validation to implementation commit `45692c94c6f22fb3798cc246a89007d151930315` and Assets tree `a3b896bb1e2ff3005be642557780ea563ed98f88`. The connector returned no contents for the large ALL EditMode XML and did not expose compressed log bytes; therefore I verified the manifest’s hashes and gate claims but could not independently recompute those SHA-256 values or inspect the full ALL XML/log contents. No Unity tests were rerun.

This review validates only the bounded internal Genealogy partial-stage failure case. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. No broader readiness, capture eligibility, export/hydration completeness, or Phase closure is implied. Canonical promotion is not performed by this review.
