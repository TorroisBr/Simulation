# Independent exact-tip review: CORE-F P12 NPC census aliases

**Verdict: `VALIDATED_CANDIDATE` (PASS).** No actionable findings remain for this bounded candidate.

## Reviewed identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `49273064773646ed431309bf5975d0a464178a45`
- Candidate tree: `7701d3120421c011ed65b1c81a5449a43c948d52`
- Actual merge base and requested base: `9ab7253ac16bb075fbb1286c5e15f75bde778e4f`
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Validated implementation tip: `d26b1f0fe85ce547167a8f721f7eb445fe81024d`
- Validated implementation tree: `6429f3507cf5156dc8b5cede48f86013e453e3f3`

The candidate is the five-commit continuation after the stated base. The exact Core canonical SHA is an ancestor of the candidate. Review covered the complete candidate diff, including production, tests, and validation records. No implementation source or Git refs were changed during review.

## Findings

The selected NPC runtime census providers bind CurrentAction, LifeState, StatusCrime, TravelState, and unbound Residence rows to the exact `DailyV1NpcRuntimeOwners` aggregate. Residence for an NPC materialized to a Person remains owned by that `PersonStore` entry. The provider construction verifies distinct runtime owners and exact admitted definition-graph identity; each witness carries its existing section ID/schema, cardinality, and revision.

The Core rebind checks duplicate/missing section IDs, matching Inventory and MoneyAccount row counts, selected-family cardinality, required role/schema, exact owner object identity, cardinality, and revision before publishing a rebound vector. It replaces only the selected alias witnesses already present in that vector. A mixed selected/legacy roster returns the entire original host vector unchanged; it does not splice Core rows into it. This change adds no owner family and does not claim that all 89 families have migrated to Core. The accepted 89-family key/role/shape contract is unchanged.

The previously reported travel admission mismatch is fixed: the selected TravelState witness is compared with `installed.SelectedCensusOwnerIdentity`. P12-D root snapshot validation now uses that same identity for the five applicable aliases, including Residence only when it is not Person-backed. Legacy `NpcRuntime` identity remains the fallback when no selected Core aggregate is bound. The protocol registration, travel pre/post checks, and snapshot admission use the selected identity consistently.

Existing assertions were not weakened. The changed Unity assertions update only the expected identity to the selected Core owner identity. Added Core tests exercise exact aggregate alias identity and revisions, rejection of stale revisions and a foreign owner object, mixed-roster whole-vector preservation, and the PersonStore-owned Residence case.

## Validation and evidence

The full Unity EditMode package was verified using the repository's official package verifier:

- Run: `0b6dfa4851214274b00d522a3ade3e83`
- Package source commit/tree: `d26b1f0fe85ce547167a8f721f7eb445fe81024d` / `6429f3507cf5156dc8b5cede48f86013e453e3f3`
- Input snapshot entries: 1,974
- NUnit result: 3,283 total, 3,283 passed, 0 failed, 0 skipped
- Manifest SHA-256: `a886b4d5d28f45cd99178afec02f6c680f67aea697a2935e04b14df3f0b9a00b`
- Archive SHA-256: `4129839f1a3f9f33774e3c94647ff8979e14f6c6939d452d9ec634f91ae03d48`
- Official package verification: PASS (one normalized XML archive entry)

The `Assets`, `Packages`, and `tests` trees are identical from the validated `d26b1f0` tip through review candidate `49273064`; therefore the later candidate commit changes do not alter the validated source or tests. The pre-fix `45acb5a0` diagnostic with 144 snapshot failures is excluded from passing evidence; the later `d26b1f0` run is the passing source tip.

CoreValidation cold and warm are recorded as 962/962 PASS. Their retained original TRX hashes match the record: cold `e6869e44b852bb0a520b68a34396cad78e332a2f523410ca586bd2b0b11b669d`, warm `bbf68d3276f3c40ba083b30991a15234f741473bb5069bc56343f285a299f3f0`. The `Simulation.Core` and `Simulation.Core.Tests` subtrees match between code commits `7c101c89e7f5c2e661e80e67d41ffb3f16171077` and `d26b1f0fe85ce547167a8f721f7eb445fe81024d` (tree IDs `cc9a789aec91930c26c12c53927dd0ac6564487a` and `314632e7afcbfce1edaf8bfdfc044d869929c4f6`). No public Core TRX evidence package was produced: the approved adapter requires a `dotnet-trx-source-context-v1` input that is unavailable for this incremental run. This is an explicit evidence-package limitation; this candidate does not claim final detached-host evidence or CORE-F acceptance.

Bootstrap composition is recorded as 39/39 PASS and NPC root snapshot tests as 33/33 PASS. The separate Smoke filter is recorded as 5/5 PASS, but its filtered report was not packaged because Unity's parent-suite count is inconsistent with the filtered descendants. Those five cases are present in the passing full EditMode package. No report was rewritten or reinterpreted.

The public validation record and package were scanned for absolute user-home filesystem paths; none were found. `git diff --check` for the candidate against its base passes.

## Scope and disposition

This is a validated, bounded NPC census alias/rebinding candidate only. It does not complete the selected owner census, claim full CORE-F/host acceptance, or promote code to Core canonical. The separately recorded Core TRX packaging limitation remains for a later evidence workflow that has the required source-context adapter.
