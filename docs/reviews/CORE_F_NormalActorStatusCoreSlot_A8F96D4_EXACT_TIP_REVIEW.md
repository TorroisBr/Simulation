# CORE-F Normal Actor Status from Core Slot — Exact-Tip Review

**Verdict: `VALIDATED_CANDIDATE` — PASS.** No actionable findings.

## Identity and scope

- Reviewed branch: `codex/core/CORE-F-core-owned-runtime-application`
- Review base and candidate parent: `4bc24b9b1fa3bbc3a849c2f9c1ade007150e9704`
- Base tree: `b136381727eeeef25be9b40242d2f6f4f10b8235`
- Candidate: `a8f96d4e6d87aa3f304aae72d8b84e7c40a5b5c1`
- Candidate tree: `b6ef20651cdec770887094dfdc1ae1fcbc4f31e9`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate ref matched the candidate SHA at review time.

The review covered the complete `4bc24b9..a8f96d4` commit diff: the Core selected-actor action stage, its Unity EditMode regression, and the validation record/result package.

## Findings

The predicate now permits an actor-side status plan for a projection-independent Normal action while retaining exact graph owner, definitions graph, current action slot, admitted action-definition reference, enabled-action, and payload/action reference checks. It excludes target status changes, NPC/City/item targets, amount, unit price, travel reason, net value, and commercial evidence. Action status definitions are resolved through the admitted effective-definition graph.

Execution bypasses only retrieval of the Unity `NpcActionRuntime` dispatch projection. Core still dispatches the exact slot, publishes the existing action result, and applies the successful status plan through the existing host P12 status-operation boundary. The Unity adapter continues to validate the exact runtime/owner projection for that operation. No second status authority or new P12 boundary is introduced.

The new regression initializes the real selected runtime, installs an exact graph-bound Core action slot, removes both Unity action projection fields, and invokes the runtime selected-action entry point. It verifies success, the exact admitted status-definition object in the actor's selected Core status owner, and one mutation-epoch advance. The local status-owner revision implementation is unchanged and remains the existing selected owner path.

The earlier stale-target logging finding remains resolved in the reviewed base: the Unity adapter suppresses projection-derived target logging for a projection-independent Normal action when the Unity projection is absent or mismatched, and its regression covers the stale-projection case. This candidate does not alter logging.

Existing test assertions were not weakened. The change and added test stay within the selected Core-owned actor-action boundary.

## Validation evidence

| Gate | Result | Run identity |
| --- | ---: | --- |
| New regression | 1/1 PASS | `fc92eb8e8e6c462f8fa9738860297dca` |
| `SimulationRuntimeAdmissionTests` | 222/222 PASS | `89414a7024af413fbd4f3b51678af359` |
| Full Unity EditMode | 3,257/3,257 PASS | `0f6d18a111ce49f0955e1c153ce08bb3` |
| Smoke | 5/5 PASS | `fda0cad19dfc4185b63d108e1c818d06` |
| CoreValidation | 682/682 PASS, cold and warm | `33c75597d726400f97b065d88cac6359` |
| `git diff --check` | PASS | reviewed candidate diff |

Validation source hashes match the candidate files:

- Production `DailyV1SelectedActorActionExecutionStage.cs`: SHA-256 `fd80e3e1af959ac0770016c4f6926068cf8301443937736b08359fffa973719d`
- Test `SimulationRuntimeAdmissionTests.cs`: SHA-256 `34939e47c34327141230ec2c1a38ecef53d93e92bfa96b76e5108d26fdfdcb43`
- Unity input inventory digest: `8d3fd2148d6e09c58fc63f1502002c83ff8e78ee3f7c655db4585768a6adae03`

The full EditMode result package manifest SHA-256 is `cc8cdb66e63654296f6b1d4cfa95611a0244e33fb832c3887625865e55687ba2`. The normalized-result archive SHA-256 is `56ee3f309dfed44fd080dc4253e90c2de891ee3f17e344f76f8f7dfb740dc195`; its original XML SHA-256 is `9b344613788e3247e6b8b8a982c85c63b8050e494a13f8300633079d0fc11b5b`, and normalized XML SHA-256 is `84a14dda4a70b6e6d67ce4f09a783a6adb7d2ac91175b4a424fe1afe3c6abaf0`. The archive contains the complete full EditMode normalized result.

The five Smoke cases are present and passing in the packaged full EditMode report. The separate filtered Smoke XML remains protected locally and was not packaged because its NUnit parent-suite counters were incomplete. CoreValidation used the approved runner with `-SkipLab` because the Lab project is absent in this worktree; its raw TRX/log and manifest remain protected locally and are not claimed as a published DotNetTrx package. These packaging limits do not change the reported test outcomes.

## Limitations

This is a partial CORE-F candidate review only. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. This candidate does not establish a shared `DailyV1Application`, detached .NET host, complete shared P12 restoration, or CORE-F completion. No canonical promotion is approved by this review.
