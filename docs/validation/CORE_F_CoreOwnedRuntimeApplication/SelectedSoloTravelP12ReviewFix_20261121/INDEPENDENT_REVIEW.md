# Independent exact-tip review — Core-only solo-travel P12 operation

**Verdict: VALIDATED_CANDIDATE (PASS). No actionable findings.**

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Published base at review: `5cb06755d0fe3a0760bd5d30f3322ab68c6e48ac`
- Reviewed production/test commit: `3e5d142bd1266571c3b53d53ba09b2b2eea7bd5d`
- Reviewed executable tree: `9bcf133f83145937edc58f83672f70ae99e28683`
- Evidence/documentation tip: `8e6c0c6c914bb5da0e2464a6c49c4b3e2a073327` (tree `a1f5e5b9c6681979be0513c2b08df18f13990528`)
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The remote candidate was at the published base during review. This review is not a publication or canonical promotion.

The independent reviewer examined the complete six-file implementation/test diff from the published base. The earlier type-boundary defect is corrected: `DailyV1ExecutionBoundaryState.IsExactSelectedSoloTravelSpatialKnowledgeMutation` requires the exact context actor wrapper and a non-null SpatialKnowledge owner, while the paired census receipt verifies both rows bind to that exact underlying SpatialKnowledge instance. The operation derives an exact section set from the admitted graph, including account, travel state, travel plan, both spatial-knowledge rows, event allocator, record sequence, and origin City presence only when the actor is present there. It compares sealed Required census identities and unchanged baselines on the owner thread.

The operation records debit and any existing compensation revisions in the active SoloTravel scope; changed rows are deduplicated and one shared mutation epoch is advanced when that scope closes. Travel transition, exact City-presence removal, route/location receipts, and the TravelStarted event use Core authorities. The event is checked against the saved origin, exact actor/route/endpoints/decision and committed travel owner, then recorded through the bound event recorder after knowledge discovery and before TravelPlan clear. Failures fault the census and invalidate capture eligibility.

The regression runs through the ordinary `DailyV1SelectedDailyExecutionStage.Advance`, moves the exact Core actor and its presence to another canonical City before census sealing, and requires a genuinely undiscovered direct route. It verifies the new route and one shared SpatialKnowledge revision increment, exact event actor/origin/destination/route/decision/day and sequence bounds, owner identity/revision witnesses, complete 89-family current census, no protocol fault, and no extra actor decision. Existing parameterized cases and assertions remain; the new Travel case is additive. Host ports retain precedence; the Core-only route is restricted to the exact admitted ordinary `NpcTravelReason.None` action, leaving unsupported/commercial and host legacy paths unchanged.

## Evidence checked

- Official CoreValidation cold/warm: **1,185/1,185**; run `18c08a36c3314a059e03eba64ea6924a`; protected manifest SHA-256 `a3b5d2e77bf23c36c8c125cef7b1e230abbe3470dd1c4466e0f00dc337a37bc9`.
- Focused Core test: **14/14 PASS**.
- Full Unity EditMode: **3,325/3,325**; run `27b278e914404b1fa3eee0f5998aad7e`; manifest SHA-256 `8a31a27c7e0ca77455bf0ada43391064392fb906618f51e6362208857124e246`; XML SHA-256 `dd41418eadedb1f4c62ac934bd7f3fd3191baaef1fa117848df7d30eec4cf9df`.
- Official EditMode Smoke: **5/5**; run `b670703932944c09be0ea478f1a68f82`; manifest SHA-256 `567c86410b2a3b9e770044db70ea70934cfc1a9ca510111ce33efc446f6a3de7`.
- The documented `-TestCategory Smoke` zero-test attempt is excluded.
- Normalized Unity package manifest SHA-256 `6f094f8678499ced2a1ccad719324347422ff8ca33ba9b470f5317ed3660d4c6`; archive SHA-256 `41797c63722b466346fd39020bcbbe79ecd963389a907ed74d465381fa9bf736` (176,120 bytes), official verifier PASS. The independent reviewer confirmed the package has no absolute local paths.
- Changed production-source hashes and the Core test-source hash match their official input snapshots; `git diff --check` passes.

This review accepts only the bounded Core-only selected solo-travel P12 operation. It does not claim complete P12 owner/operation coverage, P12-B completion, P12-A readiness, full CORE-F acceptance, or canonical promotion.