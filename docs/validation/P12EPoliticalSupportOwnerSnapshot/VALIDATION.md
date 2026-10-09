# P12-E PoliticalSupport owner snapshot validation

- Current P12 canonical base: 4fb8af28780e492c75010ed87bbe21ae6c3b816d.
- Current-base design and independent design review are included at parent commit f9b123d19cff1d668d0861cebf36b44c526fe956.
- Implementation candidate code commit: f1c8442859001af1e9bf861352e7b4a82e0cd79d.
- Candidate code tree: a406942a632ba283f88a203f6cb23f7764ed4eb8.
- Candidate Assets tree: dc81595d1e8cd19e55aafd6c5d81de24723d0a57.
- Unity editor: 6000.3.9f1.
- Focused suites: 5 suites, 38/38 passed (snapshot 5/5; PoliticalSupport foundation 6/6; P12 census 6/6; PoliticalClaim snapshot 9/9; Faction snapshot 12/12).
- ALL EditMode: 2688/2688 passed.
- Official Smoke: 5/5 passed.
- git diff --check: PASS on the implementation candidate.
- Exact XML and compressed Unity log copies, plus SHA-256 values, are listed in runs.csv and SHA256SUMS.txt.

The validation was run after the final implementation and test source edits. It covers detached schema-v1 capture, exact completed-token/owner-vector binding, all typed source-target combinations, Support/Oppose values, multiple ended history rows and re-add, deterministic order, exact local revision restoration, typed-reference validation, malformed rows and sections, duplicate active-pair rejection, length-prefixed pair identity, max-revision preservation, and captured-day staging.

Scope remains limited to the existing PoliticalSupport owner: no runtime/bootstrap composition, census or P12-B mutation-wiring changes, PoliticalWorldRevision snapshot, shared persistence coordinator, P12-G publication, P12-A/P13 readiness, complete P12-E coverage, global quiescence, or Phase 12 closure.

The exact-tip implementation review is tracked separately and must pass before canonical promotion.
