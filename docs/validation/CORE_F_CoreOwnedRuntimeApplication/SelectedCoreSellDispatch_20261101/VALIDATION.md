# CORE-F Local Open-Market SellGoods Projection

## Candidate identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Implementation commit: dbfc01b27aa84d633157ff7d07a7dc2e4a4a2589
- Implementation tree: 5006069553e410f5b282eb43cb74740815b5be90
- Parent: 77a2344d7aa36a996f0098798ebda559eff55ae1
- Core canonical during validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Origin: https://github.com/TorroisBr/Simulation

The implementation commit changes only SimulationRuntime.cs and SimulationRuntimeAdmissionTests.cs. Other worktree changes were excluded.

## Scope

When an admitted local open-market SellGoods action retains its exact typed Core action slot and payload but lacks its Unity NpcActionRuntime compatibility projection, SimulationRuntime reconstructs that view from the admitted action, exact current City projection, canonical item definition, amount, and expected unit price. The reconstructed view is installed without a P12 mutation notification and passed to the existing dispatch boundary.

The local route remains limited to the current City open market, a positive amount, a non-null canonical item, and no active Merchant plan. Missing or duplicate exact City projections and missing item asset bindings fail closed before random draws or authoritative mutation. Existing projected SellGoods routes, planned Merchant sale, and cross-City paths are not changed by this slice.

This does not complete CORE-F, implement DailyV1Application, change P12 capture/restoration, or promote Core canonical.

## Validated source identity

All successful official Unity runs recorded stable inputs. The current committed source bytes match the source inputs in each run:

| File | SHA-256 |
| --- | --- |
| Assets/_Project/Scripts/SimulationRuntime.cs | 5d94c8d794a2debbf4c22fb31bc32b5eacfd67c445e2e74cf659d16c0320cc42 |
| Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs | 72e86a7179e295c95a4689bee13f53bce2935efa5fcddbd39d3cfa34819927ad |

Unity Editor: 6000.3.9f1. Runner inputStable: true for all three successful runs.

| Gate | Run ID | Result | Original XML SHA-256 | Original log SHA-256 |
| --- | --- | --- | --- | --- |
| Focused local SellGoods projection recovery | 06f983466d764255a54a80bca7d2452c | 1/1 passed | 55ac8962452c93e7def86d7223e3b5547bc6b135d573d90fbdfc09102c61979e | 6b727b1a7c4cabba1ecf9a74cb18e2f4d7736a205a46d9efe2fdddecfab46acf |
| Full EditMode | b111d4aafc52402dbc3cd8bfac077eff | 3290/3290 passed | b40de3a91e46ef061935c2f600b2ce75fab8cecba2007f111f3cc7b235c2a1fd | e2ff80aa7ca93c08b3a9f51e64c7fbfebc9a8228f541393b9a8bfc11f95cba96 |
| Official Smoke filter | ddb6915908264aeebfc9f926ae436e60 | 5/5 passed | 080b87f9161213d5c87554636d1b814b2da44a3d7c6378f20e24687a3b7a987a | d42369e182df42b2b6e1cad8a794b8bc9f30e03e2c55894434d276736067583c |

The focused and Smoke original XML/logs remain unchanged in the protected local evidence store. The approved packager rejects filtered Unity XML when its enclosing suite counts do not match the filtered descendants. No XML was edited or reinterpreted. The public evidence package therefore contains the complete unfiltered EditMode result, which includes the focused SellGoods test, rather than filtered XML.

## Evidence package

The complete EditMode report was packaged and verified by Tools/UnityValidation/Package-UnityValidationEvidence.ps1.

- Manifest: UnityEvidence/coref-local-open-market-sell-dispatch-20261101.manifest.json
- Archive: UnityEvidence/coref-local-open-market-sell-dispatch-20261101.zip
- Archive SHA-256: e07929f897c8cc1db827a8f2a9028ad5dc3456319b7bf7c2c68de99c47d68bf8
- Archive size: 173951 bytes
- Verification: VALID, one run, one normalized XML entry
- Manifest SHA-256: c017a64a7e471528566089f2e49da6343e7cc9e587cfba55db0542446e7c0a9c

The package uses the approved path-redaction policy and contains the normalized complete EditMode XML only. A privacy scan found no workstation-home or private evidence-root path in the package manifest. Original reports and raw logs remain preserved separately.

The new test also checks that projection reconstruction does not change the Core action-slot revision or P12 mutation epoch. Rejection for missing or duplicate exact City projection occurs before a random draw or owner write. Successful Core execution performs one deterministic draw and the expected seller account, inventory, and City market writes, followed by a successful selected-roster census.

git diff --check passed before the implementation commit. The implementation commit contains only the two listed source/test files.
