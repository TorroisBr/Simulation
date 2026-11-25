# CORE-F P12 NPC census alias integration validation

Disposition: **validated bounded candidate; not CORE-F completion or promotion**.

- Candidate code commit: `d26b1f0fe85ce547167a8f721f7eb445fe81024d`
- Candidate source tree: `6429f3507cf5156dc8b5cede48f86013e453e3f3`
- Candidate base: remote CORE-F candidate `9ab7253ac16bb075fbb1286c5e15f75bde778e4f`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Unity editor: `6000.3.9f1`

This integration binds the selected NPC P12 alias-family census rows to the exact Core `DailyV1NpcRuntimeOwners` aggregate and carries that identity through travel-batch admission and P12-D snapshot validation. Compatibility NPCs retain their `NpcRuntime` identity. The exact 89-family inventory, whole-vector mixed-roster behavior, owner revisions, and persistence admission remain unchanged.

## Validation results

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold | 962/962 PASS | Run `be02d80f83834437b92b008b29895d36`; original cold TRX SHA-256 `e6869e44b852bb0a520b68a34396cad78e332a2f523410ca586bd2b0b11b669d` |
| CoreValidation warm | 962/962 PASS | Same run; original warm TRX SHA-256 `bbf68d3276f3c40ba083b30991a15234f741473bb5069bc56343f285a299f3f0` |
| Bootstrap composition | 39/39 PASS | Run `20a3c23b596644f6b2361a08f818ed3b`; passed after the travel-owner identity fix |
| NPC root snapshot | 33/33 PASS | Run `0283152751544cbca67e100c78218101` on the final code commit |
| ALL EditMode | 3283/3283 PASS | Run `0b6dfa4851214274b00d522a3ade3e83`; stable exact-tip input |
| Official Smoke filter | 5/5 PASS | Run `0ef8675afd4742f88ee510510d5a2231`; same input digest as ALL EditMode; all five cases also passed in ALL EditMode |
| `git diff --check` | PASS | Candidate diff from the refreshed remote branch tip |

The CoreValidation runner recorded source commit `9ab7253...` plus its captured working-tree input digest `0002a81a55ba6d93eaaef62f1041877b31556db8c311204ad999a8cfa71ed0b3`. The three changed Core/test inputs were committed at `7c101c89e7f5c2e661e80e67d41ffb3f16171077`; their `Simulation.Core` and `Simulation.Core.Tests` Git subtree hashes match at that commit and `d26b1f0`. The current Core file hashes also match the runner's recorded hashes. The protected CoreValidation manifest SHA-256 is `1192f9a23bc8f7345c1c6cf3cc97f2a0ec704201b60427ced1581e13581ff5a6`.

The report packager verified the normalized full EditMode package with original comparison:

- [Full EditMode manifest](AllEditMode/coref-p12-npc-aliases-d26b1f0-editmode.manifest.json), SHA-256 `a886b4d5d28f45cd99178afec02f6c680f67aea697a2935e04b14df3f0b9a00b`
- [Full EditMode archive](AllEditMode/coref-p12-npc-aliases-d26b1f0-editmode.zip), SHA-256 `4129839f1a3f9f33774e3c94647ff8979e14f6c6939d452d9ec634f91ae03d48`
- Full and filtered Smoke source snapshots have identical SHA-256 `fc898162c4459e50aeeb6017e92a904511a3f7e109b93bd621d4def263ead318`.

The separate Smoke run's original XML/log and protected run manifest remain in owner-only local retention. The approved packager refused a separate filtered Smoke package because Unity's parent `Simulation` suite count is inconsistent with its filtered descendants; no report was rewritten or reinterpreted. The five Smoke test cases are present and passing in the verified ALL EditMode package.

The CoreValidation originals and private manifest are retained unchanged in owner-only local evidence. A packaging attempt supplied that runner manifest directly; the TRX adapter rejected it because it requires the separate `dotnet-trx-source-context-v1` context. No package is claimed for this incremental Core run. This does not weaken or satisfy the final detached-host evidence gate; that later gate requires its own compliant, library-free .NET execution and package.

A diagnostic full EditMode run on the pre-fix `45acb5a0` candidate failed 144/3283 in P12 owner snapshots. Its original evidence remains private and is not counted as a passing gate. The subsequent capture-identity correction is included in `d26b1f0`, whose focused and complete suites above pass.

No unrelated ProjectSettings changes, `.meta` files, protected evidence, or untracked user files are included in this candidate record. This result does not claim a complete owner census migration, operational detached host, T/T+1/T+2 continuation, or CORE-F acceptance.