# P12-G Daily-v1 existing-NPC binding transition witness

## Scope

This is test-only evidence for the selected Daily-v1 owner inventory. The new
test checks the independent 299-section manifest immediately after registering
a Person without changing the NPC roster, then again after binding that Person
to an existing NPC without changing the NPC roster or City presence. This
separates the `P` and `M` transitions that the existing materialization test
changes together.

No production capability or runtime behavior changed. This evidence does not
close the exhaustive live-owner/writer inventory, provide the P12-G target
coordinator, establish whole-graph atomicity, or change readiness. P12-G and
P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open.

## Exact-tree validation

All runs used the candidate worktree on 2026-10-10 with Unity `6000.3.9f1`.
The code-bearing test candidate is `4c23600a5424cf3955fb774f8a03b201f8956573`
(Git tree `1255f32ce4594a072afa8ffbcb464c3023acdd93`; `Assets` tree
`4a00345b555642b25c6e2c8acb0b1cd9009e8ca1`). Independent exact-tip review
PASS is recorded in
[`../../design/PHASE12_G_DYNAMIC_TRANSITION_WITNESS_REVIEW_4C23600.md`](../../design/PHASE12_G_DYNAMIC_TRANSITION_WITNESS_REVIEW_4C23600.md),
commit `a1bf79a4a16c068c7a63e76532c2f1ea934cb996`.

| Suite | Result | Result XML | XML SHA-256 |
|---|---:|---|---|
| `SelectedDailyV1ExistingNpcPersonBindingReconcilesDynamicOwnerManifest` | 1/1 PASS | `Focused/EditMode-20261010-004429-9c6841ca6f24436d83f67c7e163cb20a.xml` | `91A78551DB9D334582798744397A75DA58969691E99065E63BE34FC88A064DA9` |
| ALL EditMode | 2740/2740 PASS | `AllEditMode/EditMode-20261010-004446-62cf94785ee64de98addd18fd2157a6e.xml` | `C3581DAD88AA80065ED20DF46DCA2C534FD670C230935DC8E5B77621BAFA93D8` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 PASS | `OfficialSmoke/EditMode-20261010-004527-38313f68c7de47ffa004f450a8b329a2.xml` | `2FFD1A54121E9E2E03A94680940DBBC4C59574E5863E25663C74ECA74C89A9F8` |

The three raw Unity logs are in `RawLogs.zip` (SHA-256
`DA5BEE33BDA286BBF8406E0EE17A072576CD8569F778602B49D8F226FDCF13C1`).
The log entries retain the same filenames as their corresponding XML files.
`git diff --check` passed for the implementation diff.
