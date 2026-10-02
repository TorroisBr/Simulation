# WI-A World Identity implementation candidate

## Candidate boundary

This is the implementation candidate for the approved WI-A World Identity
Foundation checkpoint, based on the reviewed P12 integration proposal
`9d1474b4299d8e888dd387e02e9018d9e8627f84`. That base contains the reviewed
P12 direct NPC-owner invalidation code plus its State/matrix records. The
canonical P12 branch remains `2f7c7422812de40aa1223e8310dcbd9f5d8ca474` until
its separate promotion gate is approved.

The implementation establishes one immutable, process-local `WorldId` in the
canonical `world:<32 lowercase hexadecimal digits>` form. `TesteSimulacao`
allocates it before genesis validation and retains it privately through the
bootstrap. Runtime, P18 profile, and published composition use the same object
instance. Public bootstrap access remains closed until genesis returns on its
entry thread and the selected P12 bootstrap publication scope closes with the
registered-owner census healthy. Failure clears the draft and unpublished ID,
faults an active admission context, and latches initialization against retry.

The typed P18 profile requires the exact `WorldId` instance held by a composed
runtime. The existing string profile remains available to identity-less
standalone fixtures. No persistence, continuation, copy, fork, or save-game
semantics are added.

## Validation evidence

The validated source/test tree is `45a29246fb5132372c5d14f57272ae8c50bb00e7`
(`ca1b387942be4f0f2336c592c5f6de06733a89a6`). The candidate integration
replay preserves that commit's complete `Assets` tree; its additional parent
changes are P12 documentation only.

| Validation | Result | Artifact SHA-256 |
| --- | ---: | --- |
| Bootstrap focused suite | 19/19 | XML `2168194EFCB5B0F2F5766C7430552F95D25E7926589E36B7D31F7F68D5D17B4F`; log `8E63E912347AD3935E8AD1D66395F0CFC2475E93256DED9F62149DCE2CAE5F18` |
| P18D consumer integration | 10/10 | XML `3E92E28E62A3F9C83F178AA17F83D568BBE893B4BC94E484A998DAEBE9B2BDED`; log `51410D98153C54F38DFB53472AC6D9CC7349664EDCD1403F741A521EB01D9921` |
| Runtime admission | 25/25 | XML `B7596C097AE0AF073FAF548115261FADA17CE4F67DBE337516595E79D8BA61F8`; log `0926F4F5A4EB0616728D79E9F7C56CD449E7A177984B7D459F22B732A2765B2B` |
| All EditMode | 2155/2155 | XML `F41F1F93AAAE2AB90C148BF06D1FFDB26D5E8F5F79510AF57421DCBE9E7EE25D`; log `7CF65C58E8F121EF0884DDFF503C56AA364881AF956A9AE946E84F41BE66D219` |
| Official Smoke | 5/5 | XML `05D1D32003A0B0CAAC67BC3C6E669E29FDD8683DFE28A2CCABEA40B8A69425D9`; log `DA1F3677D5308D9F18BFF88137207265FF056A21DE65B4D10B54D4987E6B8D17` |
| `git diff --check` | PASS | — |

Artifacts are retained under
`.worktrees/wia-full-validation/Library/ValidationResults/WIA-RuntimeIdentity-PreDraft-*`.

## Limits and integration handoff

This checkpoint does not claim P12-A readiness, P12-B completion, P13
readiness, persistence, save/continue, or fork identity semantics. P12-B
remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.

The implementation changes the shared `SimulationRuntime` and bootstrap
publication hotspot. Before any later FR-B composition, revalidate the WI-A
publication gate against P12 operation admission, preserve P12 owner commit
notifications, and keep FR-B's exact Faction/Person admission as its read-cut
proof rather than treating the partial P12 epoch as global coherence evidence.
