# P12-E Conflict/War Core owner stage validation

Candidate code commit: `f3f56d76829adc5467d24781fc73520765a3f3d3`
Candidate code tree: `b9038d65389f19fc65e61a7a63a1e229cc5bd794`
Base candidate: `576b79d044980e29c00cc47eedb9de4ffddb3fe1`

This is a bounded candidate checkpoint. It moves Conflict and War stores, snapshots, and census providers into Simulation.Core, then stages Conflict after ArmedForce and War after Conflict through the exact staged owner references. Battle-specific contracts and storage remain Unity-owned. It does not complete CORE-F or authorize canonical promotion.

## .NET validation

Official CoreValidation passed on the exact candidate in a clean E: worktree with no Unity `Library` and no worktree changes:

- Cold CoreValidation: 924/924 passed.
- Warm CoreValidation: 924/924 passed.
- Lab build and cold/warm runs: passed.
- Run ID: `fed2956898e84fe9a242199fe7d8daf9`.
- Private run-manifest SHA-256: `d004c0924f505c427e6d908d778aafb1efa876beb18a33c45f46284fbc084bea`.
- Cold TRX SHA-256: `355e0cc9e25b5c7d7da955e7c79b7c41488b4a397cdbccadddba6ec7a852321f`.
- Warm TRX SHA-256: `15fef86c96447267d1e2fa998a458efecd539d9d534fa1af450a9c4d9ca10a44`.

The original .NET reports and logs remain in the protected evidence store. This record does not replace the reports with a result-count summary.

## Unity validation

The official runner passed the stable full EditMode run on this candidate:

- ALL EditMode: 3,279/3,279 passed; zero failures, skips, or inconclusive cases.
- Run ID: `b4252918715d46fa82e525354567d240`.
- Stable input snapshot SHA-256: `2153c7dfa8e5e52a3d72a1018d8737c6b916aefb9ae6e9b4a81591b6dba00ff4`.
- Original XML SHA-256: `a7c8144c1ba6238f1c7dcd5b0d5874f2a9004ea6691c0d94376e797fe839aad4`.
- Sanitized result package: [`UnityAll/coref-p12e-conflict-war-f3f56d76-editmode.manifest.json`](UnityAll/coref-p12e-conflict-war-f3f56d76-editmode.manifest.json), SHA-256 `abba00885b1b0e10638b4e06aef56482ffc8fa6d50ba799bf2166ecab9a6caca`.
- Archive: [`UnityAll/coref-p12e-conflict-war-f3f56d76-editmode.zip`](UnityAll/coref-p12e-conflict-war-f3f56d76-editmode.zip), SHA-256 `80f11d3fe2fb648c903b6f4eb072b74db96ed1218fd9db50de1c67bb226975f3`.
- Official packager verification passed against the original run manifest.

Unity's first import changed `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` in the isolated validation worktree. Those changes were preserved there and remained stable during the passing run. They are not part of the candidate commit. No ProjectSettings changes from the implementation worktree were staged or committed.

The official filtered Smoke run also passed 5/5 with zero failures, skips, or inconclusive cases (`-TestFilter Smoke`; run ID `6c192b3c24584dd4beb4df27eca5c645`, `inputStable=true`). Its original XML SHA-256 is `d3679538b9ad68c95c5b9eacb8d698c2ba21f971276cf454daf80c520674a1ba`; original log SHA-256 is `cb325382088cbab92b46a809cbf28ae5237b1b91ebe3978943c357b44c0fd658`; private run-manifest SHA-256 is `cd0f303d3e4bf9d85953c212e81ee00b3771a598014a9aee8b63493c2c43e993`.

The current Unity evidence packager rejects that filtered XML because its `Simulation` suite declares `testcasecount=3279` while the filtered report contains five selected test cases. The root report correctly accounts for five outcomes. The original report remains unchanged and protected; no corrected or reconstructed XML was created. Consequently, the Smoke result is retained as private original evidence but is not in the sanitized public package. This evidence-packaging limitation remains open for review and must not be described as a complete public validation package.

## Integrity

- `git show --check` passed for the candidate code commit.
- The initial Unity compile failed because three newly created `.meta` files lacked standard CRLF line endings and a final newline; the files were corrected without changing their GUIDs. The passing Unity run above is after that correction.
- The first EditMode run changed the two Unity project settings listed above; it was not used as stable evidence. The passing run used the initialized inputs and recorded `inputStable=true`.
- No production or test source changed after the passing runs.