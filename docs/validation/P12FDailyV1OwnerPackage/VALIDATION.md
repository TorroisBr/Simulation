# P12-F Daily-v1 owner package validation

- Checkpoint: P12-F, Knowledge, directives, P11 choices, and active commitments.
- Implementation base: b8f008dd6689e6548b53df110a5ae4fc9ba6b288.
- Canonical base fetched before validation: 0619a33cd4287d89bad80fbe546763aff8f2a75b.
- Implementation commit: 8809be743cbc94155cd85a417e58ec55bd60965d.
- Exact validated Assets tree: 5dda045428ce7d73e3355cf324b6734a0ad18c34.
- Unity: 6000.3.9f1.
- git diff --check: PASS on the candidate code and final evidence changes.

| Gate | Result | Retained result |
|---|---:|---|
| P12-F focused (-TestFilter P12F) | 23/23 | [Raw/P12F/P12F.xml](Raw/P12F/P12F.xml), compressed log beside it |
| P12-E owner-package regression | 6/6 | [Raw/P12E/P12E.xml](Raw/P12E/P12E.xml), compressed log beside it |
| ALL EditMode (-All) | 2731/2731 | [Raw/AllEditMode/AllEditMode.xml](Raw/AllEditMode/AllEditMode.xml), compressed log beside it |
| Official EditMode Smoke (-TestFilter Smoke) | 5/5 | [Raw/OfficialSmoke/OfficialSmoke.xml](Raw/OfficialSmoke/OfficialSmoke.xml), compressed log beside it |

runs.csv records commands, counts, and per-file hashes. SHA256SUMS.txt covers each retained XML and compressed log. All four runs were produced against the same final Assets tree; only validation/documentation files were added after the code commit.