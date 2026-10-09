# P12-E PoliticalSupport owner snapshot validation

- Current P12 canonical base: `4fb8af28780e492c75010ed87bbe21ae6c3b816d`.
- Current-base design and independent design review: `f9b123d19cff1d668d0861cebf36b44c526fe956`.
- Implementation and focused-coverage code commit: `9da6c882c035870ca0d67612288e1b1d76ccf56e`.
- Code tree: `768db39ebf3720c93d86250cefd370922df61e77`.
- Assets tree: `45f959b73528924a296bba6a9404a7878b7f6013`.
- Unity editor: `6000.3.9f1`.
- Focused suites: 5 suites, 39/39 passed (PoliticalSupport snapshot 6/6; PoliticalSupport foundation 6/6; P12 census 6/6; PoliticalClaim snapshot 9/9; Faction snapshot 12/12).
- ALL EditMode: 2689/2689 passed.
- Official Smoke: 5/5 passed.
- `git diff --check`: PASS.
- Exact XML and compressed Unity log copies, plus SHA-256 values, are listed in `runs.csv` and `SHA256SUMS.txt`.

The latest validation was run after the final test-source edit. The added case mutates the PoliticalSupport owner after a completed boundary and confirms capture rejects both a changed cardinality/revision and a revision-only change. Existing coverage verifies detached schema-v1 capture, exact completed-token/owner-vector binding, typed source/target combinations, Support/Oppose values, ended history and re-add, deterministic order, exact local revision restoration, typed-reference checks, malformed rows/sections, duplicate active-pair rejection, length-prefixed pair identity, revision saturation, and captured-day staging.

The initial exact-tip implementation review passed with no correctness findings and noted missing focused cases for owner drift. This rerun adds both cardinality-changing and revision-only owner mutations after a completed token. A fresh exact-tip review of code commit `9da6c882c035870ca0d67612288e1b1d76ccf56e` is pending. Malformed witness variants cannot be injected through the exact live token/vector boundary: capture first authenticates the token and exact owner census vector. The copied-vector rejection and stale-owner mutation cases cover the caller-reachable failures. Review and validation evidence remain bounded to the existing PoliticalSupport owner.

Scope remains limited to the existing PoliticalSupport owner: no runtime/bootstrap composition, census or P12-B mutation-wiring changes, PoliticalWorldRevision snapshot, shared persistence coordinator, P12-G publication, P12-A/P13 readiness, complete P12-E coverage, global quiescence, or Phase 12 closure.
