# CORE-F Runtime Owner Section Contracts — validation

**Status:** validated implementation progress; not canonical, not a promotion package, and not CORE-F completion.
**Base/candidate checkout:** 8770bbb43353cf9106b5598f6c4c09b4f1bd7343 (tree f39f33315bea4fd47654786a35faced423560dd9), plus the four exact source/test files listed below.
**Core canonical:** a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.

## Implemented boundary

The Core-owned DailyV1ExecutionBoundaryState now stores the expected P12 owner-section contract catalogue, its sealed state and revision. In shared-runtime mode, ContinuationCensusProtocol commits section registration and inventory transitions to that Core state while retaining its Unity provider objects and per-family validation as an adapter projection. Dynamic roster inventory replacement is permitted only while a shared runtime protocol operation is active. Standalone protocol instances retain their existing local behavior.

This change does not establish that all 89 selected owner families are complete, dynamically inventoried, or ready for persistence. It does not move the individual owner providers into Core or claim new capture, hydration, daily-execution, or detached-host authority.

## Exact changed source/test inputs

| File | SHA-256 |
|---|---|
| Assets/_Project/Scripts/ContinuationCensusProtocol.cs | 3EEE35212547FAE30F6E40CD4E2D7E07BE4596A5E4899ADA7F82C9E6A59F1EAD |
| Assets/_Project/Tests/EditMode/Editor/ContinuationCensusProtocolTests.cs | 600730B0444076ECF756D4799444C8DEB16F8F0A861BF71CF2B6B5F75120A710 |
| Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs | 2B62531CEE2FCBA9240AC5A8BAF7CAB9386E5CFE73DDE6703F06A28C4AAA0C79 |
| tests/Simulation.Core.Tests/DailyV1RuntimeProtocolOperationContractsTests.cs | 88B3D81CF58EEA168CEFBDCD01C145F0E7FD370D2DC599D6239C643D0AB39A14 |

The existing unrelated ProjectSettings changes and untracked evidence were left out of the candidate patch and remain unstaged. They are included only as pre-existing worktree state in the protected runner context; they were not edited by this task.

## Validation

| Gate | Result | Run and source identity |
|---|---:|---|
| CoreValidation cold/warm | 888/888 each, PASS | Run adc2e20da32e4f51a28a6150fbe72a40; working-tree input digest 506e280c07320f9f25d0663d27f900a9e0df02f452c0eafa870e4bc38ef5eb0c |
| Focused ContinuationCensusProtocolTests | 26/26 PASS | Run a899a34de2ef4895ad476170bb7e3bc1; XML SHA-256 9ce04188c96af36243996538ce4262c8d6f8c6633c9412f04987703832703e91 |
| Unity ALL EditMode | 3,271/3,271 PASS; zero failed, skipped, or inconclusive | Run db6e25125b1e441491e87feab641b9be; candidate-source input digest 629e473f7ad7dfa54355ad24a5f21dc196124bdde50e1a3ef996f853de8d5e58 |
| Official filtered Smoke | 5/5 PASS; zero failed, skipped, or inconclusive | Run 154546d902824102a1130a2c23df334c; input path/hash inventory equals the ALL EditMode run (1,956 entries; derived inventory SHA-256 f24ee7cda35e1a7b467d4b036e3f6c77d0054eb46710ba366802ac8d460b6b0d). The overall runner snapshot digest differs because the Smoke execution used a detached validation worktree; both manifests identify the same commit/tree and every input path/hash entry matches. |
| git diff --check | PASS | Line-ending notices concern normalization; no whitespace error in the owned change. |

The Unity editor was 6000.3.9f1; runner host metadata is in the protected originals. The complete EditMode package below is the durable report evidence. CoreValidation and the separate Smoke report remain in protected local retention; their hashes and run IDs are not presented as substitutes for accessible reports.

## Durable Unity package

The repository packager created the complete EditMode result-only package and verified it against the protected original run manifest:

- Manifest: Evidence/coref-runtime-owner-section-contracts-editmode-20261027.manifest.json
- Archive: Evidence/coref-runtime-owner-section-contracts-editmode-20261027.zip
- Manifest SHA-256: 00C2F2E97C01CC2E0990E96197E1A709ABDBA8E8723D24CEA2DB1A74F3980026
- Archive SHA-256: 8CB44F8125C272520174C069195AED3BAEAC17C126852FB73528FDC9B3345241
- Official original-to-package verification: PASS; one run, one archive entry.

The complete EditMode report contains all five Smoke cases with Passed results. The separately invoked Smoke filter also passed, but its filtered XML is not in a public package: the approved packager rejects the runner's full-discovery parent suite counts against the five selected descendants. No XML was edited, relabeled, or hand-normalized.

The CoreValidation manifest and cold/warm TRX reports remain protected. The current approved TRX source-context adapter does not accept the CoreValidation manifest schema, so no .NET TRX package is claimed. This evidence limitation blocks canonical promotion until complete required gate reports are permanently and independently accessible under the evidence policy. The source and tests remain available as a recoverable candidate.

## Known review limitation

The prior operation-inventory review records that Core operation scopes are counted for quiescence, but individual scope handles are not correlated to operation IDs for strict out-of-order-disposal detection. This implementation preserves that behavior; it does not claim strict per-handle LIFO identity pairing.

## Required follow-up

1. Preserve the source/test candidate and obtain exact-tip independent review.
2. Resolve the evidence-policy packaging path for the required CoreValidation and filtered Smoke results without altering originals or bypassing the approved packager.
3. Continue the approved Core-owned P12 owner/capture boundary only after this bounded inventory addition is durably reviewed.
