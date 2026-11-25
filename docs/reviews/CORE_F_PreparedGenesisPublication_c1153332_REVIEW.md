# CORE-F prepared-genesis publication — exact-tip review

## Exact candidate

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Reviewed implementation commit: `c1153332124135c10192f841aff465fd488ddc5e`
- Reviewed implementation tree: `0af7f157cdaddd286a361e19ddc0ba71f93d4a8e`
- Parent candidate: `56fbb2f3b0ae34d81d565d7560fa339347870c8b`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Review: **PASS — VALIDATED CANDIDATE**

## Findings

No actionable defects were found in the exact three-file change. The publication method verifies that the preparation, application, GenesisRequired admission, NoSave result, private Core session, genesis composition, definitions, and owner-thread context all match before attempting the session gate. Unity forwards the retained preparation only when it owns the corresponding Core session projection. The restore path remains separate.

The regression verifies that a host boundary for a different execution state leaves the candidate private and registers no projection; a matching boundary publishes the exact prepared session and projection; and duplicate or cross-application publication is rejected without replacing the active session.

## Independent evidence review

- CoreValidation run `2fc33d29e3cb4125a8cd9d7a8621a7a8`: cold/warm 1,015/1,015 PASS; stable input digest `bdd48f0588b58395464a63ed361a97337286df0ded35077922996b572a1125d8`. The reviewer independently checked the protected original manifest and TRX hashes and the changed Core source/test hashes.
- Full Unity EditMode run `fce45917466e4c86955e2f26dcb057ed`: 3,299/3,299 PASS; stable input digest `8320043336c211b69420a39d1f9f9fd94301f8063ddf082ed97ae5e6da2713f2`. The sanitized public package passed the official verifier.
- Official Smoke run `6d1cd4d38048402c971d0ac7406fe010`: 5/5 PASS; independently checked from its run manifest.
- The CoreValidation checkout had a Unity `Library`; these results do not establish Library-independent .NET host execution.

The reviewer performed no edits and did not rerun tests. Full validation hashes and evidence retention are recorded in [VALIDATION.md](../validation/CORE_F_PreparedGenesisPublication_20261105/VALIDATION.md).

## Limits

This review accepts only the bounded prepared-genesis publication checkpoint. It does not accept or promote the complete CORE-F application/host package, establish the public `DailyV1Application.TryStart`, complete detached restoration, or change Core canonical.
