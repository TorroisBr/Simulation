# CORE-F Host-neutral Daily-v1 startup — PersonStore census provider

## Candidate and scope

- Candidate: `codex/core/CORE-F-restore-stage-transaction` at `e646edef94b5f721d2fca769155eea180b8f7261` (parent `388a85887245f9b2616ca1adbc954bce639cafd1`).
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; this checkpoint is not promoted.
- The selected Core census composition now derives one life/residence census identity from every actual `PersonStore.Persons` member, including unmaterialized Persons, and supplies a provider reading that exact `PersonRuntime` witness.
- NPCs bound to a Person use that Person row once; an NPC without a Person continues to use its `NpcResidence` row. The composition checks exact owner instance, section identity/schema, cardinality, revision, duplicates, and expected group counts before registry sealing.
- Startup remains fail-closed. No synthetic provider, partial registry, active session, full-profile `TryStart`, or capture-eligibility claim is introduced by this checkpoint.

## Validation on the exact committed source

- Official CoreValidation: 1,195/1,195 cold and warm; cold build and both CalendarDemo gates passed. `Simulation.Lab` was explicitly skipped because this checkout has no Lab project file. The runner reported a Unity Library present, so this run does not claim Library independence.
  - Run `062b5d1d92ee48099600e2259c54a05e`; E:-local private manifest SHA-256 `0df03652a9ce026a9043c851557b7fa8fae34e916335defe8dd945f906b28d31`.
- Official Unity EditMode: 3,325/3,325; stable source input; Unity 6000.3.9f1. Run `aeb871f42111452581b9a909e57f988c`.
  - Privacy-safe package: `Unity/coref-host-neutral-person-census-e646edef.zip`; archive SHA-256 `6a7ed614de3726fffad0608e4893087aa8a41597d7275ccc1b646ee0959c70a3`. The official package verifier passed against the original private run manifest.
- Official Smoke filter: 5/5; stable source input; run `2111e8cc264b4f90be774d0496646080`. The full EditMode package above also contains the complete suite, including these Smoke identities.
- Focused Core regressions: 2/2 passed, including the unmaterialized Person census provider and fail-closed startup checks.
- `git diff --check`: passed.

## Unity temp-path diagnostic

An initial full EditMode run failed three unchanged P12A durable-store tests because Unity/Mono received a 263-character generated temp-file path and surfaced `DirectoryNotFoundException` when opening it. The test resolved `Path.GetTempPath()` to the configured E: location. No persistence implementation or assertion was changed. The same failing storage test passed with a short E:-local temp root, and the full exact-commit EditMode suite then passed 3,325/3,325. Failed diagnostic runs remain preserved privately and are not represented as passing evidence.

## Remaining P12 census gaps — not complete

The 89-family contract remains unchanged. The live startup test currently reports 60 provider section IDs absent from the Core composition; this is not a claim that only 60 of 89 families remain. These require owner-backed providers or an explicitly approved exact-empty witness, not invented providers:

- P12-C: `p12c.runtime-id-allocator.events`, `p12c.runtime-id-allocator.decisions`, `p12c.runtime-id-allocator.travel-parties`, `p12c.simulation-record-sequence`.
- P12-D: `p12d.genealogy.parentage`, `p12d.explorable-sites`; and for each `npc-000001` through `npc-000010`, `p12d.npc-local-observation-receipts/<npc-id>` and `p12d.npc-merchant-trade-state-receipts/<npc-id>`.
- P12-F: `p12f.political-knowledge.holders`, `p12f.political-decisions.records`, `p12f.actor-choice-inputs`, `p12f.scheduled-directives`, `p12f.travel-parties`, `p12f.expeditions`, `p12f.npc-decision-occurrence-receipts`.
- P12-B: `p12b.justice-records`, `p12b.crime-p18-receipts`, `p12b.justice-p18-receipts`.
- Crime social appraisal: `p12.crime-social-appraisal.knowledge`, `p12.crime-social-appraisal.outcomes`, `p12.crime-social-appraisal.reactions`.
- P12-E: `p12e.economy-keyed-sale-receipts`, `p12e.armed-force-spatial.positions`, `p12e.armed-force.contingents`, `p12e.armed-force.forces`, `p12e.armed-force.relevant-person-references`, `p12e.battles`, `p12e.conflicts`, `p12e.contingent-manpower.states`, `p12e.estate.records`, `p12e.faction.affiliations`, `p12e.faction.records`, `p12e.institution.records`, `p12e.office.incumbencies`, `p12e.office.records`, `p12e.office.tenures`, `p12e.political-claim.recognitions`, `p12e.political-claim.records`, `p12e.political-support.relations`, `p12e.property.ownership`, `p12e.property.transfer-history`, `p12e.wars`.

The .NET TRX/log originals remain in protected E:-local evidence. The current approved DotNetTrx packager requires a different source-context schema than this CoreValidation manifest; no context was fabricated and no public normalized .NET package is claimed. P12-A full-profile capture/restoration, the standalone .NET host, and separate-process continuation remain unproven.