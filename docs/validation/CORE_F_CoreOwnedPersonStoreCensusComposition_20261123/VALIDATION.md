# CORE-F Core-owned PersonStore census composition

Date: 2026-11-23
Base candidate: `2d6fb00507aa1a3f849112f447790b08103652c6`

## Scope

The two existing PersonStore census providers are now compiled from the shared Core package and created from the exact `PersonStore` in the selected runtime owner graph. Core emits the required membership and materialization-binding registrations in ordinal section order, with the live owner identity, schema, cardinality, and revision baselines.

During selected-profile inventory assessment, the Unity compatibility rows are checked against those Core witnesses. The protocol stages both replacements, verifies the exact required section pair and shared owner/revision, and only then publishes the Core providers to the shared census registry. A missing owner graph fails closed without returning a partial registration set. An empty but valid `PersonStore` still produces the complete two-row provider set with zero cardinality.

The source file and its Unity `.meta` GUID were moved unchanged into the Core package. No new owner or state was introduced. Mixed or unselected NPC compatibility behavior is unchanged; P18 receipt rows remain host-projected. This is a bounded provider-composition step and does not claim migration of all selected-profile owner families, a complete .NET host, or CORE-F completion.

## Validation

- Focused Core composition and existing P12 regression: **3/3 passed**.
- Official CoreValidation: **1,194/1,194 cold and 1,194/1,194 warm**. Run `ab437723fd9d41bca33176a970e4b9e5`; stable input digest `4e7e268a2b1c8583993647638839f60477bd140381a887c6473923c5a0cbaa11`; private manifest SHA-256 `5bb4b6cfea8bb5a6e965223c10e1da111cbe10244f270bab82f7408d65204167`. Cold TRX SHA-256 `c933e9e9e98a0cd23cdd55e185700aac2754390bfc9e0c8d4b18a33b6a158e89`; warm TRX SHA-256 `709360c3fbf52332e79fa6721723e99809d241f6872c007e4aad5e2442d66c0e`. The Core runner does not emit the approved `dotnet-trx-source-context-v1` adapter, so original TRX and the runner manifest remain in protected local evidence; no public normalized TRX package is claimed.
- Focused Unity census integration: **1/1 passed**.
- Full Unity EditMode: **3,325/3,325 passed**. Run `dd9c12f2546e40148269cde0b640097a`; stable input digest `80b0a76a270dc3f58fa070e9fef8e1a48f242d6efbc4babbad856f63e4563f5f`; private run-manifest SHA-256 `ec87096658da2bdcfe2f232e0d728b2ba0314893fd40e8db4fc0cb628264b61d`.
- Official EditMode Smoke filter: **5/5 passed**. Run `8ac7a8d93c414ebc89f506c10fd2c020`; the same stable Unity input digest; private run-manifest SHA-256 `a64f71f147312d55e3d839831f7acd1047cace2c06e01c6bebd2076f3e5c9899`. All five Smoke case identities are present and passing in the full EditMode report.
- The approved Unity packager produced and verified the full EditMode archive at [`Unity/coref-personstore-census-20261123.zip`](Unity/coref-personstore-census-20261123.zip). Archive SHA-256: `0539921e986c71f3efeda96134c135092b59bec485615f441c6889da1172bc91`. Package manifest SHA-256: `48097c829ffb08748c7d6e2206c78040315851759779cc40a40b0dd9eb40b398`.
- The filtered Smoke report remains unchanged in protected local evidence. The approved normalizer declined a separate Smoke package because its `Simulation` suite `testcasecount` does not match its descendants. The full EditMode package is coherent and contains the same five passing Smoke identities; no XML was edited or reinterpreted.
- `git diff --check`: passed.

Core input snapshot and Unity input snapshot each remained stable during their respective runs. The existing local ProjectSettings changes and unrelated worktree data were preserved and excluded from the candidate changes. No Unity, Core, or P12 contract was altered to accommodate the environment.

## Remaining work

This checkpoint transfers only the PersonStore membership/materialization-binding provider pair. Other selected census sections, the full Core-owned runtime, public host-neutral startup, detached restoration, and independent .NET process continuation remain separate CORE-F work.