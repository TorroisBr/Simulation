# P12-G P9 genesis non-invocation witness validation

- Candidate code commit: `b8221e3f244179386aa1e308e8edbf1768980d55`
- Candidate code tree: `88629bebb11838e4ae63c584ddafee189df81af4`
- Validated `Assets` tree: `6281fd68a6a70af8126feb82ca6c59c09a0e454c`
- Base canonical: `a89ad052cbb98fc3c81bda04575f57fa6850c483`
- Unity Editor: `6000.3.9f1`

## Scope

This increment adds a scoped, thread-local test probe at `SimulationGenesisPipeline.ExecuteStages`. The probe counts entry into the P9 genesis stage pipeline but does not intercept, reorder, or alter any stage. The existing populated omitted-read-model success fixture activates the probe only around `TryRestoreDailyContinuation` and requires an invocation count of exactly zero.

The existing fixture also verifies same-day restoration, unchanged continuation roots and included C–F owner graph, fresh target read models, and two later deterministic boundaries. These comparisons support the bounded statement that the fixture observed no gameplay effects during staging. They do not establish a universal registry of every possible no-op gameplay callback; the candidate makes no such claim. This closes the explicit P9 pipeline non-invocation witness only and does not add a serializer, product hook, or gameplay behavior.

## Validation results

| Gate | Result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| Focused `DailyV1RestoreStagesFreshGraphPublishesOnceAndContinuesDeterministically` | 1/1; 0 failed, skipped, or inconclusive | `040B5F7DA780CB87C7A319D8D5FE1316E88AD873180ED643B58F4F4806FB2DAE` | `F5AF3946A456DBE167404350EA36A29129B6B604CFDA7C8C73A72D445C37F2C5` | `1C26735899A17EB1A768002240E2EDBDFF854700DC7E24787FC31EE6CEA8B55C` |
| ALL EditMode | 2804/2804; 0 failed, skipped, or inconclusive | `5EB86BF58F08A3A530B12D0443D930FC4336EF76AA98546F32C69D584AD7F87F` | `F17C689EB1F74508D6025AD86F3696B4E912304C3DC8A8D6EF55DC647495BCA0` | `FF633585D7FE6336FDFB4F6231827978C5F7164118B6455B6906ADD4F7BA50CC` |
| Official Smoke (`-testFilter Smoke`) | 5/5; 0 failed, skipped, or inconclusive | `68BA341F5F92C311EC0322F20C129B9464A9C71CC593FC17FDC0FFEB7586DA78` | `B95BF3F414501F45D750CFBDD9C118325000E504C3D0782CE38A9925779091AA` | `ED9FCFAA398347034F85EE49EA7FEBDBD0E10DEBFCF0D93BEBF810EE59C73183` |
| `git diff --check` | PASS (`a89ad05..b8221e3`) | — | — | — |

All XML files report `Passed` with coherent totals and zero failed, skipped, or inconclusive tests. Each compressed log was decompressed and its SHA-256 verified against the raw log before the raw log was removed from the candidate folder.

## Protected local files

The pre-run and post-run SHA-256 values matched for both user-edited settings:

- `ProjectSettings/EditorBuildSettings.asset`: `58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28`
- `ProjectSettings/ShaderGraphSettings.asset`: `5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7`

Unrelated untracked `.meta` files and historical validation outputs were left untouched.
