# Core-owned selected NPC census composition

Date: 2026-11-22
Candidate base: eccb7af0d1eb98c6955c5eb0daf65f442d350c8d
Scope: selected Core owner-graph census composition, including valid empty-roster behavior.

## Change

Core composes live census registrations for every selected NPC owner family represented by its current graph, using the existing provider builders. It checks the composed family identities, alias-owner identity, live owner identity, schema, revision and cardinality, then sorts registrations by ordinal section identity. Each MoneyAccount row remains cardinality one. Non-Person residence rows are included when applicable.

An empty selected NPC graph composes an empty per-NPC provider set. This is distinct from a missing provider set and does not add a minimum NPC cardinality rule. P18 local-observation and merchant-trade receipt authorities remain host-projected until their owners join the Core graph. Unity compatibility providers remain in place for unselected or mixed-roster NPCs. This checkpoint does not claim complete P12/Core ownership or CORE-F completion.

## Validation

All result-bearing runs used the official runners. Unity run manifests report inputStable=true; the full EditMode run was also packaged and verified.

| Gate | Result | Run identity |
| --- | ---: | --- |
| Focused Core empty-roster composition test | 1/1 PASS | CoreSelectedNpcCensusProviderCompositionAllowsEmptySelectedRoster |
| Official CoreValidation cold | 1188/1188 PASS | bcf8e835d74b401bb4151e8925c11019 |
| Official CoreValidation warm | 1188/1188 PASS | bcf8e835d74b401bb4151e8925c11019 |
| Focused Unity bootstrap composition | 41/41 PASS | c7300e45563b4ca89cae808572ad31d9 |
| Full Unity EditMode | 3325/3325 PASS | d67ffada3a524cb8b24312220e6606e7 |
| Official Smoke filter | 5/5 PASS | 8efd97b49eb34ce88be0ea20d6a9c074 |
| git diff --check | PASS | candidate worktree |

The full EditMode deterministic package is Unity/coref-selected-npc-census-full-empty-roster-20261122.zip (176,028 bytes, SHA-256 55c3026094b88dae07c3921c5f403befe42c896f0e645ef0abb15f3ab99dde2b). Its manifest SHA-256 is c4d7f4d766fa2db10ae042711fb5994994cf2afbd5a7ab03be41360b936ea998. The official verifier returned VALID: runs=1 entries=1. The protected run-manifest SHA-256 is 78bc50e85ad1a6bdeDD64641754d00c03bb164022ab00c4899b5b61834e43f69; the original XML and log remain in the owner-only E: evidence store.

CoreValidation used its native simulation-core-validation/v1 manifest. Its private manifest SHA-256 is 6194833f4f1dd3c9ece0c84de4d24279567975507a23a5e1c58311ade9fec6d2; cold TRX SHA-256 is a27730fd3ee0616d9cf3ac1aa4b6576a1a35b6280592074b853331f9a79e297c; warm TRX SHA-256 is 337df35302c624aa28e3541d97540c3ebbc8a1dc1bcde21cff52ee9b69def6db. Original TRX and logs remain in owner-only E: retention. No public DotNetTrx package is claimed because this native run does not emit the separately required dotnet-trx-source-context-v1; no context was fabricated.

The filtered bootstrap and Smoke XML reports remain unchanged in private retention. The official runner reports 41 and 5 executed cases respectively. Their enclosing NUnit suite nodes retain the 3325 discovered-suite testcasecount; the public packager rejects the filtered hierarchy because suite counts differ from emitted descendant cases. The reports were not rewritten or packaged. NUnit documents that filtered test-run.total may be lower than discovered testcasecount ([NUnit Test Result XML Format](https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html)). This records the current filtered-run packaging behavior only; it makes no conclusion about other historical XML artifacts.

A first CoreValidation invocation was launched outside the repository root and stopped at MSB1009 before tests ran. That diagnostic is retained and excluded from the pass results. The corrected official invocation ran from the repository root and passed.

## Preservation and disk

Protected ProjectSettings files and unrelated worktree changes were left untouched. Original validation evidence was preserved. The earlier full EditMode package from the pre-empty-roster source is retained as historical evidence; this record points to the new package above.

E: had approximately 23.89 GiB free immediately before the successful official validation sequence and 23.55 GiB afterward (about 0.34 GiB net use). No evidence or Unity cache was deleted for storage recovery. The result remains above the 15 GiB advisory threshold and 8 GiB minimum projection.