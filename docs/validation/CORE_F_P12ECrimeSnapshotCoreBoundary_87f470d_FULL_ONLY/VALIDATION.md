# CORE-F P12-E Crime/Social Appraisal Core Boundary

## Candidate

- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`.
- Base: `99738f7daced48757f4c50e881ee3654fd6d7b19`.
- Implementation commit: `87f470d743f01fc6a0b56a4d77e579702594208b`.
- Implementation tree: `ec9ed8006e52043cb7f3243f466088951b8a5101`.
- Scope: move the existing P12-E Crime/Social Appraisal detached snapshot into Simulation.Core; retain Unity runtime/token access in a forwarding capture adapter; single-source the existing section IDs and schema in the shared Core owner contract. No live registration, capture eligibility, operation wiring, profile expansion, or daily behavior changed.

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| Official CoreValidation, cold and warm | 862/862 each; complete runner PASS | Run `6bef6a046ba44e3e940ad2fc1486b3e6`; private manifest SHA-256 `28b3a0e4604aaae796fe40fa728385a2d0c7ff90c27fa21844d45f9419cea72d`; cold TRX SHA-256 `2291c13186f94ab2044483b20599603d7c5ae0bda283a6b6e05737f170280e0b`; warm TRX SHA-256 `3d17e06bf135c2d9aaa50b1d456b64ef0a9ee5978c67147e12b9adecd6664451`; source input digest `41f1d45120dde1e9694b5f61e7f9d00adc9462a7a65001e555ff14f9e572e212`. |
| Focused Crime/Social owner snapshot EditMode | 6/6 PASS | Run `32c12294e9744907b6e24a09d139a8af`; stable input manifest retained in the protected local evidence store. |
| ALL EditMode | 3271/3271 PASS; zero failed, skipped, or inconclusive | Run `4670b88fff1a4294859f1e13eccd53dd`; stable input; report SHA-256 `8e877da637ecb03e0a901968b143ede636b5b9c8406a627493a5a7858ecf7683`. |
| Official Smoke subset | 5/5 PASS | Run `a63685f42a8a439186b3997ddba996da`; stable input; cases are also present in the full EditMode report. |
| `git diff --check` | PASS | Checked the candidate diff. |

The Core and Unity runs began from base HEAD `99738f7`, with the candidate changes present in their captured source/input inventories. Those recorded source hashes were checked against the committed candidate files; the files are byte-identical. The Core runner also reported stable source and generated-dependency inventories. Unity reported stable validation inputs.

The approved Unity evidence package is `coref-p12e-crime-snapshot-87f470d-all-editmode.zip`, SHA-256 `55500f3ceb0a75c284da0cbfdebff7ed23fbb0f69268523cf9898b59387bbfcc`; manifest SHA-256 `07406fcc8577edbe4792845ff259164a0e0d579b5affb026e632795b5245a1d1`. The repository packager's `Verify` action passed against the retained full-run manifest. It contains the complete EditMode result set, including the focused and Smoke cases.

The separate focused and Smoke XMLs are preserved unchanged in protected local evidence. The packager refused those filtered reports because Unity left the parent `Simulation` suite `testcasecount` at 3271 while emitting only the selected descendants (6 and 5 respectively). They are not treated as packaged reports; the full report passed structural verification. Original Core TRX and runner manifest are likewise retained in protected local evidence and are not published as raw reports.

## Scope limits

This is a source-boundary integration checkpoint, not CORE-F completion or Core-canonical promotion. It does not establish shared DailyV1Application construction, full owner/runtime closure, detached host restoration, executable .NET continuation, or cross-host parity. Existing P12-E profile and owner semantics remain unchanged.