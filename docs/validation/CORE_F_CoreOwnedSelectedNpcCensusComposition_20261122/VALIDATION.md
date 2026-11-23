# Core-owned selected NPC census composition

Date: 2026-11-22
Candidate base: eccb7af0d1eb98c6955c5eb0daf65f442d350c8d
Candidate implementation commit: b612168c177107890021da0454a45886cd9be8ed
Candidate source tree: 9ab1582d20e39e9a8f0941697c2bf0e56977573d

## Change

Core composes live census registrations for every selected NPC owner family represented by its current graph, using the existing provider builders. It checks the composed family identities, alias-owner identity, live owner identity, schema, revision and cardinality, then sorts registrations by ordinal section identity. Each MoneyAccount row remains cardinality one. Non-Person residence rows are included when applicable.

An empty selected NPC graph composes an empty per-NPC provider set. This is distinct from a missing provider set and does not add a minimum NPC cardinality rule. P18 local-observation and merchant-trade receipt authorities remain host-projected until their owners join the Core graph. Unity compatibility providers remain in place for unselected or mixed-roster NPCs. This checkpoint does not claim complete P12/Core ownership or CORE-F completion.

## Validation

The final official Unity runs captured source tree 9ab1582d20e39e9a8f0941697c2bf0e56977573d with inputStable=true. The final CoreValidation run records candidate commit b612168c177107890021da0454a45886cd9be8ed and the same committed tree.

| Gate | Result | Run identity |
| --- | ---: | --- |
| Focused Core empty-roster composition test | 1/1 PASS | CoreSelectedNpcCensusProviderCompositionAllowsEmptySelectedRoster |
| Official CoreValidation cold | 1188/1188 PASS | f3ab7e8d1cc14aac83423b40d724dac6 |
| Official CoreValidation warm | 1188/1188 PASS | f3ab7e8d1cc14aac83423b40d724dac6 |
| Focused Unity bootstrap composition | 41/41 PASS | 39d80d36e34d4196a009445438668368 |
| Full Unity EditMode | 3325/3325 PASS | 8b24fd3de0bd47b48e3a3332403d2508 |
| Official Smoke filter | 5/5 PASS | 33169343262143a9bb3a2b625374820a |
| Candidate diff check | PASS | base eccb7af0 to b612168c |

The full EditMode deterministic package is Unity/coref-selected-npc-census-full-final-20261122.zip (176,107 bytes, SHA-256 760df0b0b07ede981f8c57ef2080c0381ee329721f80efb438945d51c205f280). Its manifest SHA-256 is 919546e8d7d603db92ecf00df59ba7f460348afed5685fed8c974ad87577046e. The official verifier returned VALID: runs=1 entries=1. The protected full-run manifest SHA-256 is 53c24ad99b9f30c50560c75c8ecc6e6dd62499109b85e1af8b8bff4a552febe6; its exact source tree is the candidate tree above.

The final CoreValidation native simulation-core-validation/v1 manifest SHA-256 is 85e11f955045a7d97929eceb5af81b5480b293f5e0ceccc68f76fb74c4c5751f. Cold TRX SHA-256 is 5d73ea82a32432b3be6d62dd8e6df29ce6c3d26e9256b8025908d2e607215293; warm TRX SHA-256 is 7782cd344fd24217a38031db406ac9d11d1dae156c3043f565cc26656d6d3495. Original TRX and logs remain in owner-only E: retention. No public DotNetTrx package is claimed because this native run does not emit the separately required dotnet-trx-source-context-v1; no context was fabricated. The runner noted this checkout has a Unity Library, so this run does not claim Library-free execution.

Filtered bootstrap and Smoke XML reports remain unchanged in private retention. The official runner reports 41 and 5 executed cases. Their enclosing NUnit suite nodes retain the 3325 discovered-suite testcasecount; the public packager rejects the filtered hierarchy because suite counts differ from emitted descendant cases. The reports were not rewritten or packaged. NUnit documents that filtered test-run.total may be lower than discovered testcasecount ([NUnit Test Result XML Format](https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html)). This records the current filtered-run packaging behavior only; it makes no conclusion about other historical XML artifacts.

A first CoreValidation attempt was launched outside the repository root and stopped at MSB1009 before tests ran. Its diagnostic is retained and excluded from the pass results. The corrected pre-commit run passed; the final CoreValidation run above is bound directly to the committed candidate.

## Preservation and disk

Protected ProjectSettings files were hash-compared with the final Unity input snapshot and all three matched. Unrelated worktree changes, existing evidence, and the earlier package were preserved; no protected or unrelated paths entered the implementation commit.

E: had 23.89 GiB free immediately before the successful official validation sequence and 23.01 GiB afterward, about 0.88 GiB net use including retained logs, temporary output, and packages. No validation evidence or Unity cache was deleted for storage recovery. Free space remains above the 15 GiB advisory threshold and 8 GiB minimum.