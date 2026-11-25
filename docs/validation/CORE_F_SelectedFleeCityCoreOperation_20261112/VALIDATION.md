# CORE-F selected FleeCity Core operation boundary

## Candidate identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Code commit: `ad8b321e37dd9ae613eb904ef64523ef8ff83078`
- Code tree: `e4fda2c7b6edb3dc6b7c3b49596ba018dac3135d`
- Code parent: `662cdcd19213513f8fc3c65f8ebe6b9b39f86e9d`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Status: validated candidate only; no Core-canonical promotion or CORE-F closure claim.

The implementation moves selected FleeCity's admitted Crime/Justice P12 operation opening and identity checks into a Core-created boundary. Core pins the exact admitted session, actor/action/payload, owner graph, Justice census binding, protocol, and immediate operation scope. Unity retains exact City/Location projection preflight and publication callbacks, including the existing TravelPlan leaf admission. Non-admitted compatibility behavior remains on the host boundary.

## Validation

| Gate | Result | Run identity |
|---|---:|---|
| Official CoreValidation, cold | 1,109/1,109 passed | `781c6ef0ba6e47278b8c9359e389dff8` |
| Official CoreValidation, warm | 1,109/1,109 passed | `781c6ef0ba6e47278b8c9359e389dff8` |
| Unity `SimulationRuntimeAdmissionTests` | 246/246 passed | `4662199487c5419f9cf374505e4c1c3d` |
| Full Unity EditMode | 3,318/3,318 passed | `6f41ccb1fb8941a7898dbfc008de834f` |
| Official filtered Smoke | 5/5 passed | `914d3beed45a4b06a6a8c45a9421736b` |
| `git diff --check` | passed | implementation commit `ad8b321e37dd9ae613eb904ef64523ef8ff83078` |

CoreValidation's input inventory was stable (`9bc76d052639f594dda769664699b41933dacb7be402ea02a4ff0792688c3667`): all 541 source inputs and 4 generated dependencies matched the committed candidate after commit. The Unity inventory was stable (`eddda14f6972e5999cc1854d3d207de3884cab831422014a0d518525d2246c2a`): all 1,982 captured inputs matched the committed candidate after commit. Both post-commit audits found zero hash or byte-count mismatches.

The Core run manifest records the parent candidate as HEAD while the implementation was in the working tree; the complete captured inputs were compared byte-for-byte and by SHA-256 with the committed code tree. No validation inputs changed during execution.

The public normalized Unity package contains the complete full EditMode report, which includes the focused admission and Smoke test identities:

- Manifest: `UnityEvidenceFull/coref-selected-flee-city-core-operation.manifest.json`
- Manifest SHA-256: `9cbf474f2221ad6e051b6d9ed54e4b39a143a226175885161138296afe0a3513`
- Results archive: `UnityEvidenceFull/coref-selected-flee-city-core-operation.zip`
- Archive SHA-256: `ba80d95cd391971e5e0ebf9ec81c25922799ddd3c12c7de3f0d6c31dd56b6841` (175,742 bytes)
- Official package verification against the protected full-run manifest: passed.
- Public manifest absolute-workstation-path scan: passed.

The packager rejected a combined package attempt containing the filtered reports because their aggregate NUnit suite count did not match the descendant cases. No output package was produced from that attempt. The accepted package uses the complete full EditMode run; the focused and filtered Smoke originals remain protected in private evidence retention.

Original Unity XML/logs and the CoreValidation TRX/logs remain in owner-only local E: evidence. The approved `dotnet-trx-source-context-v1` context needed for a public normalized TRX package was not assembled; no Core TRX package is claimed. The retained original report files were not changed.

## Scope limits

This checkpoint transfers only the selected FleeCity operation boundary described above. It does not claim complete Core-owned daily execution, a detached .NET host, completion of shared restoration, full CORE-F validation, Phase closure, P12 scope change, or Core-canonical promotion.
