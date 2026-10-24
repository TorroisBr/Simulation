# Independent exact-tip review — target status projection identity

**Verdict: `VALIDATED_CANDIDATE`**

## Identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Base commit: `3cb725822f925981c90813adb144d5bac58dd664`
- Candidate commit: `144ef29177897c5de235d72361ba8adf471495f8`
- Candidate tree: `70832df59d1e9884583877d3de250c1bc1cf7736`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate was verified at the candidate SHA; canonical remains at the SHA above.

## Reviewed change

The reviewed commit adds an exact runtime-object check for a Unity action projection's target. For a target-bearing selected slot, `HasExactCurrentActionProjection` resolves the payload's `TargetNpcRuntimeId` through the admitted owner graph, requires the resolved owner to be the exact graph owner, resolves that owner through `runtimeByOwner`, and requires `ReferenceEquals(actionRuntime.TargetNpc, exactTargetRuntime)`. The Unity logging and compatibility-dispatch boundaries therefore cannot consume a different `NpcRuntime` object merely because it aliases the target's `RuntimeId`.

The regression creates a distinct stale runtime object with the exact target's `RuntimeId`, injects it only into the Unity `NpcActionRuntime` projection, and executes the selected Core action. It verifies that the Core status mutation commits once to the exact target owner, does not mutate the actor owner, increments the mutation epoch once, and does not log the stale alias name. The prior projection-independent action test remains the coverage for execution without a Unity action projection. No target selection, Core payload, action ordering, writer authority, or domain semantics were broadened.

No actionable review findings remain.

## Source and evidence integrity

The full EditMode run manifest records input snapshot `0a33ae3288453ea2ab09e13b4d04a46c2d95cc8c6fc9c96599e133649ca4f9cb`. Its source hashes match the committed candidate files exactly:

| Candidate source | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `b511cfcd50db6f2cffda5e511aacef44b6257a526bc2bacde7d2077a6c45c66f` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `2e8cb72c6e3b327147d9972c084c361368b4b56d7bbf83c74d0918682a108856` |

The manifest's repository HEAD/tree identify the parent because validation ran against the modified worktree before the candidate was committed; the exact changed source hashes above match the committed tree. The original and normalized report identities are recorded in the manifest.

| Gate | Result | Evidence |
| --- | --- | --- |
| Focused alias regression | 1/1 PASS | The named test is present and Passed in the packaged full EditMode XML. |
| `SimulationRuntimeAdmissionTests` | 223/223 PASS | Run `228bf4bddf6f4932a302fc410047dcd2`. |
| Official Smoke filter | 5/5 PASS | Run `f980454968234784ae434869a43e4c48`. |
| Full Unity EditMode | 3258/3258 PASS; no failures, skips, or inconclusive cases | Run `514f761f865649e38229e5dcefdff48e`, Unity `6000.3.9f1`; original XML SHA-256 `30081f7c5cd0852e9dcc73b770563866e711003e20c1c6ec8a572d8d47464ee1`; normalized XML SHA-256 `804af6912e678c5d0104454a254b484cf1bd8a1cddaa7a1470aa4edec88597dd`. |
| EditMode package | Integrity verified | Manifest archive SHA-256 `8497b96b59f570749480e7ea29db1968b84c72028cfff99749e2f68a36db1ce9`, 172,621 bytes; archive has the expected single XML entry and its hash/length/counts match the manifest. No local-user paths were found in the public package manifest. |
| CoreValidation (`-SkipLab`) | 682/682 cold and warm PASS | Run `5352071c0a67492aaf7491b4b2851a15`; the validation record says `LibraryAbsent=False`, so no Library-independence claim is made. |
| `git diff --check` | PASS | Exact base-to-candidate diff. |

## Limitations

This review validates only this bounded exact-target status/logging integration candidate. Core canonical is unchanged. It does not establish a shared `DailyV1Application`, complete detached restoration, a fixed .NET host, full CORE-F acceptance, or canonical promotion.