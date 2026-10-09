# P12-E PoliticalClaim + Faction owner snapshot integration validation

- P12 canonical base: `a768f2d9eca161f5cff059a782737412f43b2861`.
- Candidate branch base: `02a3b9be5268efb173ad9da762e0e99861efd69f`.
- Candidate implementation commit: `1e2d81b7e18d7290ef3adf90cb44160bf3aba1a5`.
- Exact tested Assets tree: `803ace9ead6ca5e6cbd3ca95b4574e5d03a8393f`.
- Focused gates: 14 suites, 183/183 passed.
- ALL EditMode: 2683/2683 passed.
- Official EditMode Smoke: 5/5 passed.
- Staged and unstaged `git diff --check`: PASS.
- Pre-existing protected files (ProjectSettings and .meta) byte-identical: 683/683 (657 .meta, 26 ProjectSettings files).
- Three Unity-generated .meta files absent at preflight remain untracked: `Assets/_Project/Scripts/ArmedForceSpatialPosition.cs.meta`, `Assets/_Project/Tests/EditMode/Editor/ArmedForceSpatialPositionTests.cs.meta`, and `Assets/_Project/Tests/EditMode/Editor/P17ARuntimeTests.cs.meta`.
- The final-run XML results and compressed Unity logs are committed. Uncompressed Unity logs remain local because they contain generated whitespace.
- `runs.csv` records each final suite, counts, and XML/compressed-log hashes. `SHA256SUMS.txt` covers the 49 committed XML and compressed-log artifacts under `Raw`.
