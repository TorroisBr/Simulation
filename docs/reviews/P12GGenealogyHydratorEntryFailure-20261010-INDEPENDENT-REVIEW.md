# P12-G Genealogy hydrator-entry failure-atomicity — exact-tip independent review

**Verdict:** `VALIDATED_CANDIDATE` — bounded restore-stage test evidence only  
**Review date:** 2026-10-10  
**Candidate branch:** `codex/phase12/P12GGenealogyHydratorEntryFailure`  
**Candidate evidence tip:** `8405ffca5f496c2f9c9d7fd4e34553cce5c327d4`  
**Reviewed code commit:** `732864564bbf2fa8bd7373cac1994876280e876d`  
**Canonical base and current canonical:** `95e922eb3c2083a87bf1353bce60303f42a2a62c`  
**Reviewed code tree:** `8754f563b43e0a3b3994b1acc65ccfca2846fb89`  
**Reviewed Assets tree:** `161d5e89f178a43672804f4f5b28739e51ceaee2`

## Review findings

The full comparison from canonical base `95e922e` to candidate tip `8405ffc` is a clean three-commit fast-forward (ahead 3, behind 0, merge base is the canonical base). The final candidate commit changes only Markdown whitespace in the validation manifest. The candidate tree retains the reviewed code commit's exact Assets subtree.

The implementation adds `DGenealogyHydratorEntry` as the final enum member in `P12GDailyV1RestoreStage`, preserving all existing implicit enum values. `P12DDailyV1OwnerPackage.TryCaptureAndStage` invokes the optional stage observer immediately before `GenealogyStore.TryCreateFromOwnerSnapshot`. With the production observer unset, this adds no domain/runtime behavior.

The candidate adds that stage to `SimulationRuntimeAdmissionTests.DailyV1RestoreInjectedPrivateFailureKeepsOldSessionHealthyAndAllowsLaterRestore`. The observer throws an `InvalidOperationException` at the selected stage. The shared harness confirms the callback was reached and asserts that the old active-session reference, completed-boundary token, owner-thread health, selected facts, continuation roots, and full included-owner projection remain unchanged. It then advances source and uninterrupted control identically, verifies their owner projections match, retries restore successfully, compares the restored projection, and advances it.

One qualification is material: `DPersonsStaged` already fires immediately before the new event, with no intervening operation. Its existing injected exception was already at the same operational point before the Genealogy hydrator call. The new named event provides an explicit Genealogy-entry evidence label, but it adds no distinct behavioral state or new reachable failure interval. This review accepts that traceability value for the specifically named cutpoint while recording the duplication. The evidence does **not** exercise a false return, throw, or partial mutation from inside `GenealogyStore.TryCreateFromOwnerSnapshot`; it does not cover every owner hydrator or validator.

## Validation evidence

The committed validation manifest binds the run to code commit `7328645`, code tree `8754f56`, and Assets tree `161d5e8`. I inspected the retained XML summaries:

- Focused `SimulationRuntimeAdmissionTests`: 175/175 passed, 0 failed/skipped/inconclusive.
- `DailyV1RestoreInjectedPrivateFailureKeepsOldSessionHealthyAndAllowsLaterRestore`: 55/55 passed in both the focused and ALL EditMode XMLs, including the newly appended case.
- ALL EditMode: 2849/2849 passed, 0 failed/skipped/inconclusive.
- Official Smoke: 5/5 passed, 0 failed/skipped/inconclusive.
- `git diff --check`: PASS as recorded in the candidate validation manifest.

The XML summaries and validation manifest are committed alongside the candidate, and the Assets tree matches the recorded validated Assets tree. The candidate diff contains no ProjectSettings changes or untracked `.meta` files.

## Scope and integration limits

This is a restore-only observer cutpoint and atomicity-test increment. It does not change P12-G profile semantics, owner data, production hydrator behavior, publication, serialization, P12-A/P13 readiness, or Phase status. The in-memory restore operation still discards the private candidate and retains the old active session on this injected exception.

The review supports the exact bounded candidate as validated evidence. It does not certify complete P12-G §6.4 failure injection, complete graph/compatibility coverage, P12-G completion, capture eligibility, P12-A readiness, P13 readiness, or Phase 12 closure. Protected ProjectSettings and unrelated untracked files remain untouched.
