# P12-B solo travel-start implementation review

**Verdict: NEEDS_CHANGES.** This review does not approve canonical promotion.

## Exact review target

- Canonical base: `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`.
- Candidate tip: `ba1ded6e56d0358de37dbbb70f449c01cbd47318`.
- Code commit: `63d4e3a880e7dba7246bce1ea93b1982cb9bb951`.
- Code tree: `cab0b326354bdd90d1f9c55a3daba70c52d9e7ca`.
- Candidate evidence: `docs/design/PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_CANDIDATE.md`.
- Reviewed by: independent reviewer `/root/solo_travel_design_review`.

The reviewer checked the full code diff against the canonical base, the
reviewed design at `91728e4aee7717c6b002691a9a9e96c4ad22ac71` and design
review record `812b04628bd8d3f04c491b1df4adae7e2c3692e2`, current Phase 12
authority, relevant action/provider and owner mutation paths, and the
candidate's exact validation artifacts. The remote candidate tip and
`origin/codex/phase12/canonical` were confirmed as `ba1ded6` and `e76e50b`
respectively before review; the review worktree's local canonical ref was
stale and was not used as authority.

## Required changes

1. `SimulationRuntime.TryBeginP12SoloTravelStartOperation` adds a source-City
   section whenever `npc.CurrentCity` is non-null. The reviewed contract
   includes that owner only when the current City projection actually
   contains the NPC. `NpcRuntime.StartTravel` likewise reports a City
   mutation only when `currentCity.ContainsImportantNpc(this)` is true. A
   non-null but non-reciprocal City pointer must therefore not add a City
   section to preflight or the changed-section set. Add focused coverage for
   this state while preserving the existing no-City-projection test.
2. `SoloTravelOperation_DeduplicatesDebitAndCompensationOwnerWrites` calls
   `MoneyAccountRuntime.TryDebit` and `TryCredit` directly under a reflected
   scope. It does not exercise the actual `TravelSystem.TryStartTravel`
   charge → rejected `NpcRuntime.StartTravel` → successful charge restoration
   path required by the design. Add an end-to-end selected-profile test that
   proves the two local account revisions are retained and the account
   section is reported once by the enclosing operation.
3. Add focused scheduled-request Travel coverage. The source path appears to
   route through the wrapped runtime action execution, but that behavior is
   not yet demonstrated by a test. Assert the nested `runtime.travel.start`
   owner operation is active for the requested Travel provider callbacks and
   preserve the existing success-status ordering outside that operation.

## Verified evidence and limits

All seven XML/log artifacts listed in the candidate record were independently
matched to their recorded SHA-256 hashes. Each XML reports Passed with zero
failed and zero skipped tests: the new suite 9/9, runtime admission 31/31,
TravelParty advance 10/10, runtime orchestration 12/12, long-run 7/7, ALL
EditMode 2214/2214, and official Smoke 5/5. The code diff passes
`git diff 'HEAD^' HEAD --check`.

These passing results do not waive the three focused design obligations
above. The candidate remains unpromoted; P12-B remains incomplete, P12-A
remains `WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains open.
