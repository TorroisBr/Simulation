# CORE-F P12-D NPC Core-row capture — independent exact-tip review

**Verdict: VALIDATED_CANDIDATE.** No actionable implementation or test-integrity findings.

## Reviewed identities

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `0f047b28285376a93ee7e9ca65e291b7a0ef77f4`
- Candidate Git tree: `61eb445b438729977362fe869340601a37e27176`
- Immediate base: `dbe896a51937bea6c3e464033997c840dbd5cfc6`
- Base tree: `1274b8f028f4e5ed6217868d7dba4362975e67d7`
- Current `codex/core/canonical`: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate was the synchronized remote branch tip at review. Candidate branch, base ancestry, tree, and canonical ref were checked directly. The candidate checkout was pinned to the exact SHA and clean; `git diff --check` passes.

## Scope and review

Reviewed the complete base-to-candidate diff: ten committed paths comprising the Core continuation-save binding and immutable ordered NPC D/F snapshots, Unity package/capture integration, the stale-census and mixed-roster/Core-row regressions, and the scoped validation record/package. The payload excludes `ProjectSettings`, `.meta` changes, private raw evidence, and unrelated or untracked files.

The row authority is the exact `DailyV1SelectedRuntimeOwnerGraph` retained by the active session. Capture requires an active/revalidated continuation-save operation, graph identity equal to the session execution-boundary graph, and a complete selected owner-section vector. It binds the Core path only when graph NPC RuntimeIds exactly equal all 22 dynamic NPC census families. Each exported row is derived from the Core owner, selected definitions are validated by object identity, and a materialized Person is resolved from the graph's exact `PersonStore`; exporter checks reject a mismatched Person/RuntimeId binding. Rows are accumulated privately, checked against census cardinality and revisions, and published only after final operation revalidation. Failed or stale capture does not replace an already-bound operation snapshot.

The adapter accepts Core rows only when operation/source identity, exact row and owner counts, order, RuntimeIds, selected owner references, and definition graph/reference identities match every `NpcRuntime` projection. It still runs the existing projection checks and uses the operation-owned Core row objects as final D/F rows. Mixed rosters do not bind partial Core rows; the existing whole-roster compatibility capture remains active and the regression verifies that the unselected actor is included and the save operation remains valid. Legacy no-operation capture is unchanged.

The null identity arguments in the Core-side family matcher are intentional where the admitted census witness is owned by a Unity compatibility projection (including NpcRuntime-backed life/travel/crime/action/residence witnesses and exact-zero receipt witnesses). They do not admit arbitrary or missing census entries: the complete-vector contract requires non-null identities and exact family/alias cardinality; source-witness revalidation compares the retained token with current census identities; and the Unity capture checks each projection-owned identity and exact Core-owner binding. Families 0–6, spatial, inventory, money, merchant plan, and applicable Person residence are checked against their exact Core owner identity directly. Commercial projection binding to the selected Core owner is established by the existing `NpcRuntime` composition path; the Core exporter still reads the Core owner.

The existing `DailyV1SelectedOwnerSectionCensusContract` was not changed. Its complete-snapshot rule retains the complete selected NPC and City family sets, fixed required/exact-empty sections, family alias identity, and dynamic Person/non-Person residence checks (the existing 89-family contract). No owner family, schema, row format, or profile requirement was weakened. Export preserves selected-definition IDs, action payload identifiers/targets/amount/decision/occurrence fields, source values, and Core owner revisions. The added tests assert exact graph order and owner references, row counts/IDs, representative D/F revisions and payload identities, stale recapture preservation, persisted Core-row object identity, and mixed-roster whole-roster fallback. Test changes add coverage and do not remove or relax existing assertions. No new Unity assets or `.meta` identities were introduced.

## Validation and evidence applicability

- Official CoreValidation run `882cf6f2597e477ab716d48220bbd696`: protected native manifest SHA-256 `4fd2dae385750c5997418401731c75fe1beebe3cd432dde1b3b399160316c06d`; manifest status Passed, stable inputs, cold 936/936 and warm 936/936, zero failed/inconclusive. Cold/warm TRX SHA-256 values: `41e945a788a8c28ffd81eeddbfe34dd917b6282d4a13d70c309e6d3c7df92d8e` and `de335afc09987cdaf95a97ba857aa7af06e829931f61d0182224bc9566cbfbdc`. The recorded input digest is `78b52cd4aea26d1da18106b01cf6a05a6efca708a7a073e18b5e123b8894373e`. The private manifest's changed Core source/test hashes match the validation record and tested checkout; the Application source's reported digest matches after the repository's declared CRLF-to-LF normalization.
- Focused detached image restore/Core-row binding: run `50ba066efc9243f3b17e6fc9281144ea`, 1/1; protected run-manifest, XML, and log hashes match `VALIDATION.md`.
- Full official Unity EditMode: run `1e924c533f454cd0a221fc05d68f8ee1`, stable input digest `b8bbdc89c1a40b74859b1751e3f298fdeefbf7e115cc831021b94765f63a9fa8`, 3279/3279, zero failures/skips. Protected manifest and raw XML/log hashes match `VALIDATION.md`.
- Official EditMode Smoke filter: run `52fa1e1c054146e4b50dd92ff7e8dce3`, same stable input digest, 5/5. I compared exact test identities and outcomes against the five Smoke cases in the full EditMode XML; all five match and pass.
- The normalized full EditMode package has SHA-256 `ced9dd0b2c6c82ecabc2f10937c50a8dd181eae82425a9044c655f9639a7149a`; its manifest SHA-256 is `ee882b883c8194438ce32649fc82b8bef59f384d5d7b356dee25a3906074d03e`. Both hashes match the committed artifacts. The archive contains exactly one result-only XML with 3279/3279 passed cases, including the same five passing Smoke identities. The normalized XML contains no local Windows home/worktree path, machine identifier, or username. The validation record reports successful package verification against the protected original run manifest.

**Evidence limitation:** the cold/warm .NET original TRX files and native run manifest remain protected/private; this candidate contains no normalized public .NET result package. The manifest and original hashes were verified for this review, but this review does not claim that permanent .NET result packaging required by the validation-evidence policy is satisfied. Confirm that requirement independently at canonical promotion; do not publish the protected originals.

## Integration constraints

This validates only the bounded P12-D/F Core-bound NPC D/F row capture and adapter binding. It does not close P12-D, complete CORE-F, establish full detached-host parity, change the P12 profile, or promote to Core canonical. Canonical promotion remains a separate workflow and must verify permanent .NET result evidence as applicable.
