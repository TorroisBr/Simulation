# CORE-F selected Person residence binding validation

## Scope

The selected Daily-v1 `SimulationRuntime.TryBindExistingPersonResident` path now delegates to the Core owner graph when the runtime is the exact selected Daily capture profile, every NPC runtime member uses selected Daily-v1 definitions, the City is the exact world projection, and its selected owner is present. Mixed or unselected rosters retain the existing Unity membership path.

The Core operation validates exact graph, `PersonStore`, City owner, Person identity and registration; rejects dead, differently assigned, saturated and stale cases; and commits only the Person life/residence owner. City aggregate population and its revision do not change. P12 remains the existing `runtime.person.residence-bind` operation with its Person life/residence section reservation. Tests cover one Person revision and one mutation epoch on a successful bind, idempotent no-op behavior, stale-plan rejection, capacity and overflow failures, unchanged City population, and mixed-roster fallback.

## Validation

All validation below used source content based on candidate `8eac58267a49ac97ad3f0e36ed37d4c2c4f7a11f`. No implementation source or test source changed after the runs.

- Official CoreValidation (`-SkipLab`): cold 1,097/1,097 and warm 1,097/1,097; build and demo gates passed. Run `4c73d8617402424b9004c4071830eddf`. The captured Core compile-input digest is `cd915cc09d01b1c2628d90821053299653e958a076466ff53f8ee7522656349b`. The owner-only private run manifest SHA-256 is `c1bb8a700edf1cdd4923463d47b876e61f539c7a31d68cbf996b1b6419c0c235`; cold and warm original TRX files remain private and unchanged. This run used the existing Unity checkout with a `Library` directory; it does not establish Library-independent execution or run the separate Lab gate.
- Official focused P12 population lifecycle EditMode: 33/33 passed, zero failed/skipped/inconclusive, run `2358d8b116314cf1af4a254b57c861ad`.
- Official full EditMode: 3,316/3,316 passed, zero failed/skipped/inconclusive, run `89af83a22e6a43c7b0d552f71c9ab533`.
- Official Smoke name filter (`EditMode`, `Smoke`): 5/5 passed, zero failed/skipped/inconclusive, run `492d11bd39604746a1c85fae595b6700`.
- All three Unity runs had stable before/after inventories with the same input SHA-256: `04eb8d44f1d6c7cb712211e57e06616db164c4ab8fb27804d161e671ac0bf1c3`. The filtered P12 and Smoke case identities and outcomes were compared against the full report and are present in it.
- `git diff --check`: passed.

## Published Unity evidence

The approved evidence packager normalized and verified the full EditMode result against its owner-only source run. The archive contains the complete 3,316-case report, including all 33 focused P12 and five Smoke identities/outcomes.

- `Unity/residence-all-editmode-20261112.zip` SHA-256: `ef98b80c61c1af1c190b1f7859b7fe00891a35107d48b8d3df29278d4b37fb34`.
- `Unity/residence-all-editmode-20261112.manifest.json` SHA-256: `7f2a739bc1d2af87a8da0220787b101cbfb06bf731c73de9522310391bff1ba2`.
- Independent package verification against the protected run manifest passed; the public manifest contains no local filesystem paths.

Filtered P12 evidence was not separately packaged: the official normalizer rejected that filtered XML because its parent `Simulation` suite count disagrees with its descendants. The original report was left unchanged. The full EditMode archive passed the normalizer and contains the exact focused P12 identities and outcomes. An initial Smoke category-filter invocation returned zero cases and is excluded; the successful five-case name-filter run above is authoritative.

CoreValidation original TRX and logs remain in protected owner-only evidence storage. No normalized public .NET TRX package is claimed here; the current validation policy records that adapter as awaiting its own independent adoption review. The private manifest SHA-256 is `c1bb8a700edf1cdd4923463d47b876e61f539c7a31d68cbf996b1b6419c0c235`; cold TRX SHA-256 is `34e180fe87b4c76ee879f3dac5fafd62f1a6001d47fa39aa8fbecb8c01acafaf` and warm TRX SHA-256 is `dd035309b1dd3e2e0a67ec5425a4a9f1b0c3ce497d62d2817ad468d3899290c7`. These hashes identify protected originals but are not presented as a substitute for the complete reports; raw reports and logs are not published.

## Working-tree boundaries

Only the residence-binding production and test files and this validation record/package belong to this checkpoint. Existing `ProjectSettings` changes, unrelated validation-document edits, private evidence, untracked files and metadata are excluded and remain untouched. The Unity input digest above reflects the exact test-time input snapshot, including protected local project configuration; that configuration is not staged with this change.