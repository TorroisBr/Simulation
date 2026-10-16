# CORE-F Post-Institution Owner/Member Join — Independent Exact-tip Review

Verdict: **PASS** for documentation/evidence candidate
`0a5f8e0fad16e20a6585960c11c9a61eef0e82aa`, based on
`9ef6398245442d1b4bb9360f12e4caa51f8ac120`.

Production source/test identity remains
`ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`, tree
`e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`. Core canonical at review:
`33f41329c1ca5eb7aa60f276bc08319a55a71884`. Architecture canonical:
`797eb02f06fc5ca759a7e47283589d97a5efe73d`.

The candidate changes only `docs/CORE_STATE.md`,
`POST_INSTITUTION_OWNER_MEMBER_ACCESS_JOIN.md`, and
`audit-source/PostInstitutionJoin.py`. The generator now compares the full
`(source_path, unity_symbol)` multiset for the selected Unity projection. A
positive match and a same-row-count symbol substitution rejection both pass.
The generator reproduces the 1,660 source references across 89 owner rows, 89
capture rows, and 24 operations: 142 unique paths, zero unresolved references,
220 runtime sources, 289 restore sources, 2,582 selected Core edges, and 288
selected Unity edges. All three generated CSVs reproduce byte-for-byte with the
hashes in the audit. The generator hash is
`dad277efb6ea45e438c43d250537afe21a91725dc7847f7692eb357e8b3a8b4f`.

`git diff --check` passed. The privacy scan found no workstation/home/temp
paths, credential/token patterns, bearer strings, or private-key markers. No
production or test source changed, so Unity/.NET product suites were not
rerun; focused integrity checks and exact generator reproduction passed.

This review verifies source-reference and symbol-projection integrity only. It
does not bind each family to the concrete current owner declaration/member,
constructor, exact live instance, writer/epoch callback, capture/codec, staged
hydrator, target validation, and applicable case identity. Accordingly,
`CORE-F-Shared-DailyV1-Application-and-Host` remains `WAIT_DEPENDENCY`; no
implementation readiness or Phase/Core closure is implied. The three
pre-existing local ProjectSettings edits remain outside the candidate.
