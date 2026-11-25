# Independent Exact-Tip Review — P12-G Restored Owner-Vector Core Admission

- Verdict: **PASS; no actionable findings**.
- Reviewed implementation: `955f3ee066e8377860b7a0d2bb61bc9d67e92b14`
- Reviewed tree: `cf679c0bc673acc8a0bbf733f0f9e786c04974db`
- Base: `9c197a7d86d0ac563bff77457ac8d0771743909e`
- Canonical Core remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The review verified that the moved image-to-target comparator preserves census creation, section/schema/role/cardinality/revision/logical-alias equality, target owner identity, and exact-empty/excluded constraints. The census remains the established 89-family shape: 61 fixed sections, 22 per-NPC families, four per-City families, and the applicable dynamic residence sections.

The Core application performs both image comparison and post-guard vector/epoch revalidation only while it holds the exact initial or replacement staging authority. The legacy no-application route remains intact. No image schema, mutation, capture-token, lease, session publication, or failure-mapping semantics changed. Core tests cover new process-local target instances, revision/cardinality drift, and broken aliases.

Validation evidence identities and changed-file input hashes were checked against the candidate. CoreValidation passed 928/928 cold and warm; full Unity EditMode passed 3279/3279; Smoke passed 5/5. The full Unity package verifies against its original run manifest. The review did not alter files or candidate refs.

This review applies to this bounded candidate slice. CORE-F remains open.