# CORE-F TravelParty City Presence P12 Integration

Candidate `70591e299004e6d58780bba3184f8416655415b0` (tree `c8eef3741180f78ed2e92c19d64804299a954910`), based on candidate `174aa7bb5e7a7b3a2eda5ac8b5ab26eb4b5905f4`. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

This bounded checkpoint extends the Core-owned TravelParty P12 operation baseline from the exact selected NPC TravelState and paired SpatialKnowledge rows to each selected City ImportantNpc presence owner. Core captures and verifies the exact City owner instance. A committed receipt is accepted only for one membership addition or removal and exactly one revision increment; an acknowledged receipt advances the operation-local baseline for the next serialized transition. Unrecorded drift and a multi-membership change reported as one receipt fault closed. The existing host protocol still validates the complete owner vector at operation entry and owns compatibility-roster and non-Core census rows. This does not claim P12 or CORE-F completion.

## Validation

- Official CoreValidation run `d4acc8e4aab54c8f901a817c24432756`: **1,040/1,040 cold and 1,040/1,040 warm passed**; zero failures. It ran against the candidate worktree immediately before commit. The runner's source-input hashes for both changed files were compared with the committed files and matched exactly. Cold TRX SHA-256: `bdadc2ee19ae099d2d931d74dd59dbecbfab2fbd15d6a2423e0bb809fd6fec80`; warm TRX SHA-256: `713e173c97b4dbe8dc757276ac4eee7c9cf393c0484bfbabf14bcf396366c5d9`.
- Official Unity EditMode run `9889765bd88b477a92f55b39b23b7c9b`: **3,300/3,300 passed**, zero failed, skipped, or inconclusive. It ran against the exact candidate commit and tree on Unity `6000.3.9f1`, PowerShell Core `7.6.5` / .NET `10.0.11`. The input snapshot was stable (`d52e356c0a577e311a9b2abe3a8eb492adc9c490035a8cd52b971ac021784b54`). Original XML SHA-256: `6990c9fbb43fa04faefcb891063980e1dbca0ff620732d0d327098052b8730ba`.
- Official filtered Smoke run `fa5d8c6ed8b04fe68c9bf5b83bcb7ec0`: **5/5 passed**, zero failed, skipped, or inconclusive, against the same commit, tree, and stable input snapshot. All five passed Smoke identities were found as passed in the full EditMode report. Original filtered XML SHA-256: `bdf20d3a98af9923e569cea0ecabb23d2c7af996dd7a18e1b0b4f6fa41b5a47b`.
- The complete EditMode XML was normalized and packaged with the repository evidence tool. Package verification passed (`runs=1`, `entries=1`). Archive: `coref-travelparty-city-presence-editmode-70591e29.zip`; SHA-256 `a0841aee45d77b91dc90bfd1c62c1a2f7abfb3a5904464f15faebe0aa37bb0a8`. Companion manifest SHA-256: `d35c1138b8499aa8eb391d87469965faaf2cadb0b253bfcf3b4d2c343e4e2f0d`. The manifest and archive are beside this record. The filtered Smoke original and raw logs remain in protected private evidence; no filtered report is published as a second aggregate package.
- `git diff --check`: PASS. The pre-existing unrelated `ProjectSettings/ProjectSettings.asset` modification was not staged or changed during either Unity run; input snapshots were stable.

Original XML files, raw logs, and private run manifests remain in the protected local evidence store. This record and the normalized full EditMode package contain no raw logs.

## Evidence limits

This validates only the bounded Core TravelParty City-presence census and receipt behavior. It preserves the whole host-vector validation contract and does not establish complete CORE-F runtime composition, detached hosting, or broader persistence readiness.
