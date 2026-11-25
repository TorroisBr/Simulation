# Independent exact-tip review — faulted census capture boundary

**Verdict: VALIDATED_CANDIDATE**

- Candidate evidence tip: c371ff80a5fabc0ba8d5507c130dcafeb51cf9b
- Candidate evidence tree: 78fc262c98c93170ec8d2f6f0086e02875e4d04a
- Reviewed implementation commit: fdc57ef74f4dde012eae9ee8caf2d0e7b10da5ed
- Reviewed implementation tree: 5e96b4b1ec1106afd7596af0241f2ebccc24f0d4
- Candidate base: ebf64d09d3ed8292f8e8f4231f174271b764b6dd
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Reviewer: independent read-only review agent /root/review_faulted_capture_tip
- Review completed: 2026-11-03

## Findings and disposition

The production change rejects capture-scope assessment after the selected Daily-v1 census protocol fault latch is set. It preserves pre-fault assessment behavior and checks the latch under the Core operation boundary. The new regression covers the selected profile, the supported capture allowances, and pre-fault behavior. The independent reviewer found no actionable production-code defect, authority issue, scope expansion, or test-integrity problem.

The review initially identified two evidence gaps. Both are resolved at the reviewed evidence tip:

1. The additive TEST_CASE_IDENTITY_MAP.md records NUnit identity 2901f806-0acc-27fc-c9e2-72f7cb81f82c. The historical 49-case CORE-B identity map is unchanged.
2. The official standalone Unity Smoke selection was run after full EditMode: 5/5 passed, zero failed/skipped/inconclusive. The private manifest, original XML, and original log hashes match the validation record, and the before/after input snapshots are equal.

The reviewer verified the CoreValidation source hashes, cold/warm 1003/1003 results, full Unity EditMode 3298/3298 package, standalone Smoke evidence, official Unity package provenance verification, and absence of local workstation paths from the public validation record, manifest, and archive. The Unity archive SHA-256 is 06adadb3cfdeba0afd9c770401ec5f5c693eab80c6dbdc44ee7ae690fc5908a6; its public manifest SHA-256 is 574ed04bcfc6787d18376f78512c940bae6c8e1f88b68a4c24ff937877290831. git diff --check passes.

The review is against the exact implementation commit/tree above. Later commits at the time of review changed only the validation record and evidence files; they did not change implementation or tests. The two ProjectSettings edits were not staged or included.

## Scope limitation

This validates only the bounded faulted-census capture-scope safeguard. It does not deliver the public DailyV1Application.TryStart lifecycle, detached .NET restoration, the fixed .NET host, full CORE-F integration, or canonical promotion. Core canonical remains unchanged.