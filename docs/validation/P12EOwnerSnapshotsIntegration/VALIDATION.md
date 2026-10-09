# P12-E PoliticalClaim + Faction owner snapshot integration validation

- Basis commit: `02a3b9be5268efb173ad9da762e0e99861efd69f`
- Full staged index tree: `2abec938f8d9bbe34c3149844982fbd60a3488a7`
- Exact staged Assets tree: `803ace9ead6ca5e6cbd3ca95b4574e5d03a8393f`
- Focused gates: 14 suites, 183/183 passed
- ALL EditMode: 2683/2683 passed
- Official EditMode Smoke: 5/5 passed
- Staged and unstaged `git diff --check`: PASS.
- Pre-existing protected files (ProjectSettings and .meta) byte-identical: 683/683 (657 .meta, 26 ProjectSettings files).
- Three Unity-generated .meta files absent at preflight remain as untracked files: Assets/_Project/Scripts/ArmedForceSpatialPosition.cs.meta, Assets/_Project/Tests/EditMode/Editor/ArmedForceSpatialPositionTests.cs.meta, Assets/_Project/Tests/EditMode/Editor/P17ARuntimeTests.cs.meta.
- Per-run counts, XML/raw/compressed log paths and hashes are in `runs.csv`; raw and gzip artifact hashes are in `SHA256SUMS.txt`.
