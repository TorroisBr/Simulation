# CORE-F guarded selected actor-action routing

Status: validated candidate checkpoint; independent exact-tip review PASS. CORE-F remains incomplete; Core canonical is unchanged.

## Source identity and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Implementation commit: `279375c80b76a8cee2ef121c206a3bb146d1e332`.
- Implementation tree: `72d56fc181306696f720b44317e41b11fdf0b410`.
- Previous code commit: `c8b8420816daa2c091ea2b4259e215de4704d535`.
- Evidence-record parent at run time: `ec900225cc2e713a16117f5a17ec67ff882d6b5d`.
- Cumulative code base: `aef01355dc72ecbf409d19639c80d0ddae715f37`.
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Scope: exact selected actors are routed to Core's existing selected action pipeline. The selected Unity adapter's legacy `EvaluateAndExecuteAction` callback now validates its exact actor and fails closed instead of calling Unity `EvaluateAction` / `TryExecuteCurrentAction`. This prevents the selected Core actor boundary from silently reverting to Unity-owned action selection or execution. Unity still supplies action projection and effect/admission ports.
- The callback rejection is tested to leave the exact Core action-slot revision unchanged. Mixed-roster fallback selection, compatibility-owner mapping, and P12 census providers are unchanged.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| Focused selected actor-turn boundary | 1/1 PASS | Official EditMode run `9eebf2f962454d108286a262da6e062f`; input stable; original XML SHA-256 `18d8749f4ee292dc36a2a54cfb3cb75d33ad02f33228dddcd2df9d37dcc58235`. |
| Full Unity EditMode | 3,306/3,306 PASS | Official run `7da02a731b7c414f99cf486d61aee4bc`; input stable; original XML SHA-256 `81474c7380002b31c5ec1411b99ae04a602dd35ff310d8019afe8d713ccb5f4c`. The full run contains all seven `SimulationRuntimeLongRunTests` cases. |
| Official Smoke | 5/5 PASS | Official EditMode filter `Smoke`, run `b4058710aba349d8a637267aad1025a6`; input stable; original XML SHA-256 `483a5212b3e04243ab9db0487ef3e7dbc9d0111d01cfb1e126917fd74b6aef97`. |
| `git diff --check` | PASS | Guard/test change and implementation commit. |

All three runs recorded the same input digest: `574a3e689ba13d40880bbc2c970e052ba9ea0d8ce308de1088bc222bee46382a`. The full run captured the guard/test worktree before commit at Git HEAD `ec900225cc2e713a16117f5a17ec67ff882d6b5d`; after commit, both changed-file SHA-256 values were rechecked and exactly matched the captured input records. No implementation or test edits followed the runs.

Original reports and logs remain in protected local evidence storage. The full EditMode report was normalized by the approved packager; filtered source reports and raw logs remain private and unchanged.

## Public evidence package

- Package: `UnityFull/coref-selected-action-route-guard-editmode-20261111.zip`.
- Package manifest: `UnityFull/coref-selected-action-route-guard-editmode-20261111.manifest.json`.
- Package-manifest SHA-256: `b0fcd90dd246a99982f6ae8ef2fc9683bfa6758c8c04444ee6b889df10357c9c`.
- Archive SHA-256: `6f37387a93e150fec25fd2130b9ba769f10746381a39518c72f1b009d833a201` (175,095 bytes).
- Official package verification against the protected full-run manifest: PASS; one normalized full EditMode report.
- The archive contains normalized result XML only; it contains no raw logs or private workstation paths.

## Source hashes captured and rechecked after commit

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `e2e461939e09dc64d2a6cb652b1c0e56a454644b4dda716164c704a653994c36` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `a8129f51634ded6ccb65d1bcb86a6d4d24c58ccaf28061a30ac46224c5fc5f53` |

This checkpoint does not complete CORE-F, publish public `DailyV1Application.TryStart`, establish detached .NET continuation, or promote Core canonical.
