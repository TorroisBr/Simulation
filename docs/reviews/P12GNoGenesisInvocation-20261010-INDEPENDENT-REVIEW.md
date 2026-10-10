# Independent exact-tip review — P12-G P9 genesis non-invocation witness

- Verdict: **VALIDATED_CANDIDATE**
- Candidate branch: `codex/phase12/P12GNoGenesisInvocationWitness`
- Candidate tip: `e9058cd59ee8e4d4c0f91b3831ee29de8120d9a2`
- Base / canonical at review: `a89ad052cbb98fc3c81bda04575f57fa6850c483`
- Code commit: `b8221e3f244179386aa1e308e8edbf1768980d55`
- Candidate code tree: `88629bebb11838e4ae63c584ddafee189df81af4`
- Validated Assets tree: `6281fd68a6a70af8126feb82ca6c59c09a0e454c`

## Review findings

The pushed candidate branch resolves exactly to the requested tip and is two commits ahead of the supplied base. The production-code diff is confined to `Assets/_Project/Scripts/SimulationGenesisPipeline.cs`: a `[ThreadStatic]` nullable probe reference, an internal disposable test probe, a begin-probe helper, and one null-conditional counter increment at entry to `ExecuteStages`. The original stage validation, stage ordering, and stage execution loop are unchanged. The probe observes entry only; it does not gate, reorder, or modify a genesis stage. Outside an active test probe, the added entry operation only performs a null check. The remaining code diff modifies only the existing successful Daily-v1 restore EditMode fixture; remaining candidate files are exact-tree validation artifacts.

The fixture scopes the probe around `TryRestoreDailyContinuation` with `finally` disposal and asserts the invocation count is exactly zero. This directly proves that the exercised restore fixture does not enter `SimulationGenesisPipeline.ExecuteStages` on the owning test thread. The same test retains its existing same-day restore, included C–F graph, continuation-root, fresh read-model, and two-later-boundary parity checks. The validation manifest properly limits broader gameplay wording to no observed effects / callgraph-bounded evidence; this probe does not prove a universal absence of every gameplay callback, nor does a thread-local counter observe a hypothetical call on another thread.

## Validation evidence

Manifest: `docs/validation/P12GGraphRejectionCoverage/NoGenesisInvocation-20261010/P12GNoGenesisInvocation-VALIDATION.md`, bound to code commit `b8221e3f244179386aa1e308e8edbf1768980d55` and Assets tree `6281fd68a6a70af8126feb82ca6c59c09a0e454c`.

| Gate | Manifest result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| Focused restore fixture | 1/1, 0 failed/skipped/inconclusive | `040B5F7DA780CB87C7A319D8D5FE1316E88AD873180ED643B58F4F4806FB2DAE` | `F5AF3946A456DBE167404350EA36A29129B6B604CFDA7C8C73A72D445C37F2C5` | `1C26735899A17EB1A768002240E2EDBDFF854700DC7E24787FC31EE6CEA8B55C` |
| ALL EditMode | 2804/2804, 0 failed/skipped/inconclusive | `5EB86BF58F08A3A530B12D0443D930FC4336EF76AA98546F32C69D584AD7F87F` | `F17C689EB1F74508D6025AD86F3696B4E912304C3DC8A8D6EF55DC647495BCA0` | `FF633585D7FE6336FDFB4F6231827978C5F7164118B6455B6906ADD4F7BA50CC` |
| Official Smoke | 5/5, 0 failed/skipped/inconclusive | `68BA341F5F92C311EC0322F20C129B9464A9C71CC593FC17FDC0FFEB7586DA78` | `B95BF3F414501F45D750CFBDD9C118325000E504C3D0782CE38A9925779091AA` | `ED9FCFAA398347034F85EE49EA7FEBDBD0E10DEBFCF0D93BEBF810EE59C73183` |
| `git diff --check` | PASS (`a89ad05..b8221e3`) | — | — | — |

I inspected the full focused XML: it reports 1/1, zero failures/skips/inconclusive, and the restore fixture result is Passed. The Smoke XML header reports 5/5 and zero failures/skips. The connector returned no content for the large ALL EditMode XML range, so its outcome and artifact hashes remain manifest-reported evidence rather than independently recomputed here. The manifest records that compressed logs were decompressed and checked against raw-log SHA-256 values.

The candidate contains no ProjectSettings or `.meta` changes. The manifest reports pre/post user-edited settings hashes matched and unrelated untracked metadata/historical validation outputs untouched.

## Limits

This is a narrowly scoped P9 genesis pipeline entry witness for the successful Daily-v1 restore fixture. It does not close the broader gameplay-callback invocation question or P12-G. The predecessor State limits remain: P12-G and P12-A `WAIT_DEPENDENCY`, P13 `BLOCKED`, Phase 12 `OPEN`; no complete owner/epoch coverage, capture eligibility, export/hydration, downstream readiness, or phase closure is inferred.
