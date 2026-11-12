# Selected Daily-v1 NPC-to-Person binding validation

Status: validated implementation candidate; not promoted to Core canonical.

## Tested implementation input

The runs used the candidate worktree based on commit `126998ab8c4030c6408bfeaf976104bc86c50ff5` (tree `109bb5528384240f5ae8e94dc398afa4740d78cc`) with the current Core binding implementation and tests present in the working tree. CoreValidation recorded stable input digest `fb6c30e3c5f077c8ba7e2bc1005660de604cc514417c5a2033894e8d4edec58b`. Unity recorded stable input digest `580d3b200716096fa2d62e4255e140f8556392ca6389bff4148f9eb0563dae6b`; focused, full EditMode, and Smoke runs share that digest.

## .NET CoreValidation

Official runner run `f08d612fa1b240548c80281fb6505d99`, SDK `9.0.315`: cold **1,100/1,100 passed**, warm **1,100/1,100 passed**, zero failures. The lab build and cold/warm deterministic demonstration also passed. The protected original manifest and TRX reports remain in private local retention; they are not included in this repository package.

| Private original | Bytes | SHA-256 |
| --- | ---: | --- |
| CoreValidation manifest | 1,699,009 | `6159cd638cce82c26b0b890338ec387706e7619b8f2ee81d145d81b9b6bcae9a` |
| Cold TRX | 1,572,151 | `2264b460f1647c128f9b2f87a6ab03d4dbad7d8fec5e20cad33078101408d848` |
| Warm TRX | 1,572,151 | `58ad3fb41b8b19f897b2fca0a1ea58300378da299be3816414d17c01a8598544` |

## Unity validation

Unity `6000.3.9f1`; all accepted runs report stable input snapshots.

| Gate | Run | Result |
| --- | --- | ---: |
| Focused `SelectedDailyV1ExistingNpcPersonBindingReconcilesDynamicOwnerManifest` | `b354f9043c1f491c9ca1d221d2a333c8` | **1/1 passed** |
| ALL EditMode | `03522a65cc14402f946bd877962ac79a` | **3,316/3,316 passed** |
| Official EditMode Smoke filter `Smoke` | `c6db331d5bd1419e8b2634107fd87286` | **5/5 passed** |

The verified full EditMode result package includes the focused regression and all five Smoke identities. The filtered focused and Smoke originals remain protected in private local retention. An initial PlayMode selection returned zero cases and is excluded; the accepted Smoke run is the EditMode `Smoke` filter above.

- Full EditMode package: [`coref-selected-npc-person-binding-editmode.zip`](coref-selected-npc-person-binding-editmode.zip)
- Package manifest: [`coref-selected-npc-person-binding-editmode.manifest.json`](coref-selected-npc-person-binding-editmode.manifest.json)
- Archive SHA-256: `7ba966d5c95492cae2a31e47e6441b4e63b883b00381b5f61ebe8f710093e8f6`
- Manifest SHA-256: `61ac43ab1f249ec5f68b93b4278cb3a13a046adb05ef41d882ffb7bcab6471a2`
- The approved packager verified the archive against the protected original run manifest (`runs=1`, `entries=1`). The archive contains normalized result XML only; no raw logs are published.

`git diff --check` passed. The change preserves the existing outer P12 `runtime.npc-membership` reservation and mixed/unselected compatibility path. It does not complete CORE-F, change P12 protocol identity, or claim Core canonical promotion.
