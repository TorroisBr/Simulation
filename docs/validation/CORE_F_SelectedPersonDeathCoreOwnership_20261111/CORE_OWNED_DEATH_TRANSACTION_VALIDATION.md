# Core-owned selected Person-death transaction — validation addendum

Date: 2026-11-11. This addendum supplements the earlier `VALIDATION.md`; that record and its artifacts remain unchanged.

## Source identity and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Parent candidate: `9656c2d6e194ae10792c9af5c38210a6de1e687e`.
- Implementation commit: `6e42883e76333cc95521181cc8b8f4742caf5b5a`; committed tree: `ef1ef80e3a767c0ff9ad48080899ab9beba8d47b`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The six-file change moves selected Person-death transaction ordering and exact P12 section derivation into Core. Unity continues to provide the live census reservation and projection adapter. The 89-family census, mixed/unadmitted fallback, and P12 owner identities remain unchanged.
- Before the final validation, all six implementation/test worktree files were verified byte-for-byte against their committed Git blobs. Core and Unity validation below used these exact bytes.
- This is an intermediate CORE-F candidate checkpoint. It does not claim `DailyV1Application.TryStart`, a detached .NET host, CORE-F completion, or canonical promotion.

## Validation

All validation records captured stable input snapshots.

- CoreValidation run `159f450620854bc392db396f61fe7153`: cold and warm each 1,092/1,092, zero failures/skips; commit `6e42883e76333cc95521181cc8b8f4742caf5b5a`, tree `ef1ef80e3a767c0ff9ad48080899ab9beba8d47b`, input digest `a11be0cb200933cae0055ebb187dec15ff43fa8d5fcbec89021bec017faa04ae`. Original TRX SHA-256: cold `88d35b03ce348bf157779b9116e5ba845ae8b3a532782ba21609e232a49bb9a1`; warm `a58e4a1c6d21135343498894070747fa9f91fdd0444f07233568ea2c150c4038`. Originals remain in protected local evidence. The public DotNetTrx packager rejected this runner's `simulation-core-validation/v1` manifest because it is not the accepted source-context schema; no context was fabricated and no packaging rule was weakened.
- Focused `P12PopulationLifecycleInvalidationTests`: 23/23, run `18802988060e4103a2e6af832d464fcc`, XML SHA-256 `5cbd86d2c3bc1613280f515afc8ebf3b1738e5b4e33397ca068efa7565a787aa`.
- Full Unity EditMode: 3,306/3,306, zero failed, skipped, or inconclusive; run `bf55d7e54cc7499eaffebd8f830abc3d`. The seven existing LongRun cases all passed. XML SHA-256 `7c68e29c330f3690044af0e4182175344f0bb049a6141e2b2ac1a6498c7554b6`.
- Official filtered Smoke: EditMode `Smoke`, 5/5, zero failed, skipped, or inconclusive; run `d1a6a04f891b46e0b0b8a30f32d3c44d`, XML SHA-256 `10a67ca3bf5f659f7f207f7ea289a3f75001ddcb06e42b916567c97291724bdd`.
- The P12, full EditMode, and Smoke runs used the same 1,978-file input digest `65840e880c977b3cee6e110eab1a08632791a504f4e5d1e3710e06af1b949f73`; their before/after input inventories were stable.
- The approved packager produced and verified the normalized full EditMode evidence package: `Unity/coref-selected-person-death-exact-source-20261111.zip` (SHA-256 `bdbb2704d23ee9828361b13ac3b35bd59913bf739b9d27f366ce47e3abee5433`); manifest `Unity/coref-selected-person-death-exact-source-20261111.manifest.json` (SHA-256 `514c8ff9f2daedf9dfd7953249419a1e05b773a14597ffc1590dd943746e153a`). The package includes the five Smoke identities from the full report.
- `git diff --check`: PASS.

A separate PlayMode filter invocation returned valid XML with zero cases and is excluded from validation claims. The five-case EditMode Smoke above is the accepted Smoke run. The local `ProjectSettings` edit was left untouched and excluded from the implementation commit.

## Review

Independent read-only source/contract review: PASS, with no actionable correctness or test-integrity findings. The reviewer confirmed exact Person/NPC/City ownership checks, preflight before owner writes, exact changed P12 sections, a single successful shared-epoch notification, and fail-closed admission on postcommit projection failure. The reviewer also confirmed that the 89-family census and mixed-roster fallback are retained. No new projection-failure fault-injection test was added; this remains a coverage limitation. Exact-tip file identity is the implementation commit above; tests were not rerun by the reviewer.

No Core-canonical promotion is authorized or claimed.
