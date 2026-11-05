# CORE-F restored Person/NPC/Genealogy — independent exact-tip review

Verdict: PASS — VALIDATED_CANDIDATE for the bounded restored owner-graph relationship check.

## Exact identities

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Exact candidate tip: 1468b8cfa6ee3802c8e8bb92821310aeabbe8d45
- Candidate tree: 692303815bb0ad58dd83f5595f6f1dc3c5155f3d
- Implementation commit: 1182692aa2e4db7ae4e6154f48db8d87bfb7bfea
- Implementation tree: d48b98cbc638a7dc340d1454ee019c536ae39c05
- Implementation base: b4c6be93e7038761ca1aa74f52db5a2be5af64c3
- Current Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

The implementation commit changes only DailyV1Application, DailyV1SelectedRuntimeOwnerGraph, and DailyV1ApplicationHostTests. The candidate tip adds only the validation note, manifest, and EditMode archive. Remote refs were checked immediately before recording; candidate and canonical were not changed.

## Review result

The Core owner-graph validator rejects missing or duplicate selected NPC RuntimeIds and missing or duplicate PersonIds. For every materialized Person-to-NPC binding it requires a nonblank ID present in the selected NPC owner roster, then checks the PersonStore reverse index resolves to the exact same PersonRuntime object. It also compares the enumerated binding count with PersonStore.MaterializedBindingCount. Each Genealogy endpoint must be a nonblank PersonId present in that same PersonStore.

DailyV1Application invokes this validator from restored-Core-target acceptance after full target-vector capture and image/vector comparison, before the later restored-target and publication gates. Failure returns through the Core target-invariant failure path; candidate publication cannot proceed. No owner data, writer, revisions, epochs, P12 census, or persistence representation is changed.

The existing Unity restore adapter still validates reciprocal Unity PersonId and BoundPersonRuntime projections. This Core check validates the domain owner graph; it does not claim to replace Unity projection validation. GenealogyStore and PersonStore staged constructors already reject malformed/duplicate local owner values; this validator adds merged cross-owner membership checks.

The new Core regression is additive and rejects a validly stored Genealogy edge whose Person endpoints are absent. The existing Core restored-target test also exercises the validator on a normal empty Person/Genealogy graph. No prior assertion was removed or weakened. Existing P12-D relationship tests retain valid-link and unresolved-person coverage. No actionable code finding was identified. The full candidate diff passed git diff --check.

## Validation evidence and limits

- Public full EditMode run b43fecca14914fbf954b9846a5b1705a reports 3,299/3,299 passed, with zero failures, skips, or inconclusive results. I independently verified the public archive SHA-256 2bed96b1336661f5575f17a2f9b4e136a42a18a46bd67710f525bf387cde4e7c, one normalized result XML, those counts, and absence of drive-absolute paths.
- The public input manifest records base HEAD b4c6be93/tree 16ab4aeb, but its per-file hashes bind the tested working-tree source bytes. DailyV1Application Git blob b83daf8313d6ecbdf6302ba1305824b858bd735a has SHA-256 7849b6dec09a4df55381517846cfeddbbeb8a08677ea8d43eeeed07d4e48ad9d, matching the manifest. DailyV1SelectedRuntimeOwnerGraph Git blob f3d76367f579cb3783ad436a0034d214e452de07 has LF SHA-256 a9205aa89edcb860a1011189470c3d6c459ef776fe3747bc2311dbc7431d0438; its CRLF worktree form is SHA-256 166a0fa3d9d02a7d31f9d89d79ffbfa32904de3e7ecfdbc9b72cc4681cf40a65, matching the manifest. The code files in the implementation commit therefore match the tested production inputs despite the commit being created after the run.
- The public manifest Git blob SHA-256 is 30000407ff52a6960e28dd570e23ece485278bbca04bd068f1dfc97a46d97e28; its Windows CRLF checkout SHA-256 is 7d9b97e97523508e06685f74ba5b6314d13b5505446aeba0240f3d95f30b9261. The package manifest declares the verified archive hash above.
- CoreValidation is reported as 1,020/1,020 cold and warm, run df7432faa28b430a83b28acf74f20087, private manifest SHA-256 8622d61a3bde599d3de2ee84e8c73ec531c98c43e8e5e9ce8aca415bfc96a5e1, input snapshot digest 2239aa848b659c47b5aebb6db0b049fd7600724446e3e3679f13b0b370068498. The owner-only manifest/TRX was not available to this reviewer, so its contents were not independently inspected. The worktree Library was present; this is not final Library-free .NET proof.
- Focused P12G EditMode is reported as 33/33, with the original result privately retained and not independently inspected. The filtered XML was rejected by the official packager for inconsistent root/descendant counts. No separate Smoke result is claimed; the public full EditMode archive contains normalized result XML only.

## Scope

This review validates only the restored Core Person/NPC/Genealogy relationship check. It does not establish standalone .NET restore or DailyV1Application.TryStart, a fixed .NET host, T/T+1/T+2 continuation, full CORE-F acceptance, or canonical promotion. The separate promotion workflow and its preflight remain required.
