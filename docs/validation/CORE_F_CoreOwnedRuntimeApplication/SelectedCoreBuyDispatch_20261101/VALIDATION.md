# CORE-F Local Open-Market BuyGoods Projection

## Candidate identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Implementation commit: 59931aafe7a9cfc10fbd6080259d472e5a26d5e3
- Implementation tree: 962d237c3d3e44d9c9d1e62131ab07c3bb5273de
- Parent: a60049db9b0966bdbc415cd35f34ffc522c622bf
- Core canonical during validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Origin: https://github.com/TorroisBr/Simulation

The implementation commit changes only SimulationRuntime.cs and SimulationRuntimeAdmissionTests.cs. The worktree’s unrelated ProjectSettings and user changes were excluded.

## Scope

When an admitted local open-market BuyGoods action retains its exact typed Core action slot and payload but lacks its Unity NpcActionRuntime compatibility projection, SimulationRuntime reconstructs that view from the admitted action, exact current City projection, exact selected item asset binding, amount, and expected unit price. It installs the view without a P12 mutation notification and uses the existing selected dispatch boundary.

The projectionless route requires the payload City to equal the actor’s current City, no target NPC, a canonical item, positive amount, a non-traveling actor, and no active Merchant plan. Missing or duplicate City projections and a missing exact item-asset binding fail before a random draw or owner write. Existing projected purchases, cross-City purchases, and planned trades remain outside this slice.

The regression also verifies that the failed initial inventory/city/item lookup paths leave action, account, inventory, and market revisions and the P12 census mutation epoch unchanged. Successful execution checks the deterministic draw key, exact Core owner mutations, revisions, epoch, and final census.

This does not complete CORE-F, change P12 capture/restoration, implement DailyV1Application, or promote Core canonical.

## Validated source identity

All successful official Unity runs recorded stable inputs. The committed source bytes match the source inputs:

| File | SHA-256 |
| --- | --- |
| Assets/_Project/Scripts/SimulationRuntime.cs | 8d7b112e3ae162f60c542621b26f38371f522236b9a4387c4fb25f63a3e39165 |
| Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs | 765b64def7383b128c7b569d7e939cb60cb504752cf9bb3554c51714d30597bf |

Unity Editor: 6000.3.9f1. Runner inputStable: true for each successful run.

| Gate | Run ID | Result | Original XML SHA-256 | Original log SHA-256 |
| --- | --- | --- | --- | --- |
| Focused local BuyGoods projection recovery | fcff142ea7e749efb6455933368c42b3 | 1/1 passed | 72cdf9b5ca9816b801f58c58af63c6944aaa09cabf9681fb7ec2f6fbf4406f4f | 8d5b87b917fd518db9919e9f087c37fc7f941fdcd1b3bf8735262b47cca7e727 |
| Full EditMode | 60d6180af32e48c5baaa39d79d83fe6d | 3291/3291 passed | 224649f7905ef1969d5beacb99f9862ef1f68465f18fed8b6866c055b5a7ddf4 | 0cead2a86b0d1ab3156a6f3fe4c5cf4653395d5acedfc9f5b0ac7274b1d779ff |
| Official Smoke filter | b2d165670b374175aa431e25e45f7aa4 | 5/5 passed | a3fa52c000d6be61ba702db492033c8ab6b0a3df09103515f230dbf5a49d49e2 | e19950aa3e4df6f22b189ab0797e1059130ce1f7efe32ce9595160d739b46fac |

A preliminary focused attempt, run 1b9568b5639b417c8c508ecc48bef472, failed compilation because the new fixture referenced local variables before their declarations. The declarations were moved before the successful runs. The original failed report and log remain preserved; they are not counted as passing validation.

The successful source hashes match the implementation commit. The full run’s captured HEAD was a60049db9b0966bdbc415cd35f34ffc522c622bf; the tested working-tree bytes were subsequently committed unchanged as 59931aafe7a9cfc10fbd6080259d472e5a26d5e3.

## Evidence package

The complete EditMode report was packaged and verified by Tools/UnityValidation/Package-UnityValidationEvidence.ps1.

- Manifest: UnityEvidence/coref-local-open-market-buy-dispatch-20261101.manifest.json
- Archive: UnityEvidence/coref-local-open-market-buy-dispatch-20261101.zip
- Archive SHA-256: b94ba7b5d7ed938b3b76a8ac98687cb7303fa22c4f46115f41c2be60459490c5
- Archive size: 173999 bytes
- Manifest SHA-256: ba337cae7752954136e7b68dd59b8711506d4dbd803a0419261d5901cc02faa
- Verification: VALID, one run and one normalized XML entry

The package uses the approved path-redaction policy and contains only the normalized complete EditMode XML. A separate archive scan found no workstation or private evidence-root paths, and confirmed the XML contains the new BuyGoods test. Original reports and raw logs remain preserved separately; filtered Focused and Smoke reports were not edited.

git diff --check passed before the implementation commit. This is a bounded candidate checkpoint only; it does not make any broader CORE-F readiness claim.
