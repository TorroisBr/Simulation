# CORE-F P12-G Core-bound owner sources — validation record

Status: **VALIDATED_CANDIDATE** for this bounded source-capture slice only.

## Source identity

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Commit/tree: `7a34e1c1c5b698e9bf9761ae491a9fe5b9a9f447` / `3cdf1e4c686653ca14d796df5466d46c272b2387`
- Parent: `2f3892973c59e41fb4c08ef4f00a2c40ea82c316`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The change binds the exact genesis-created `ExpeditionStore` into the Core selected owner graph and captures detached P12-F owner snapshots for ActorChoice, ScheduledDirective, TravelParty, and Expedition against the active Core save witness. PoliticalKnowledge is not included because it is not bound to the Core session graph.

## Validation

### CoreValidation

- Run `33784477859d41d79327f264c2600928`: cold and warm **811/811 PASS**, builds passed.
- Stable working-input digest: `27a7769ae59534a144c24cc83e9321b36d02647a55aa1cf1ad45a92216941184`.
- Private manifest SHA-256: `3a5bc7242c5104bdffc1211dc7b271eef24d0c0f5db08a8a908078fcb874dbf7`.
- Original TRX reports and logs remain in protected local evidence storage. No public .NET package is claimed.

### Unity EditMode

- Unity `6000.3.9f1`, run `9935eb53b2e94b39994814d0cbac717c`: **3264/3264 PASS**.
- Input was stable before and after the run. The runner snapshot was based on parent `2f389297` plus the in-progress package source; each changed `Packages` source/metadata hash was verified to match commit `7a34e1c1`.
- Public normalized-result archive: `coref-p12g-corebound-sources-7a34e1c-editmode.zip`, SHA-256 `b33d767dd612f163b425c1b2d3717e790da9bdfb9befd3f41bc756e9ed5dfc03`.
- Public manifest SHA-256: `f03b895ba6d72285841bae9760bddb98113cba769aae0dde50cb9ae2d1e3ade0`.
- Package verification passed: one run, one result entry; archive hash matched the manifest.

### Official Smoke filter

- Run `ff4f7e51f35a4807872312358f47c8fc`: **5/5 PASS** on the exact candidate commit/tree `7a34e1c1` / `3cdf1e4c`.
- Stable input digest: `ddee32d5bea41bbf87e7ddb381d70724401fa336a9951d190961244295cfecdb`.
- The same five Smoke results are contained in the full EditMode archive; no separate Smoke archive is claimed.
- `git diff --check` and staged-diff check passed before the code commit.

## Scope limits and preservation

This candidate adds a Core owner binding and partial snapshots for four owners already joined to the Core session. It does **not** capture PoliticalKnowledge, compose a complete P12-G continuation image, stage and validate a detached target, publish through the application store, or complete P12-G/CORE-F. It does not advance Core canonical. The accepted P9/Daily-v1 fixture remains empty for Expedition participation; the binding preserves its exact owner identity and does not admit P10 content. Protected ProjectSettings edits, untracked user files, and private original evidence remain outside the candidate commit.

Earlier unsuccessful validation attempts on this source slice remain preserved in protected storage and are not counted as passing evidence.