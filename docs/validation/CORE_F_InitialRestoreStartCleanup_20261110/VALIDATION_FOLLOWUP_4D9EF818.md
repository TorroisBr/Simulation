# CORE-F restore-start cleanup validation follow-up

This additive record supersedes the earlier validation status for the
restore-start candidate. The original `VALIDATION.md` and its package remain
unchanged as historical evidence.

## Candidate identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `4d9ef8186c70a654b2a4e006eb36671c7fee3bac`
- Candidate tree: `a126b7ed58d92288c9341403c3495d1935f216e3`
- Base: `49331cd7b131b8bc33a87d3f9a0a5f6077fae0dd`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Production code is unchanged from reviewed candidate `0c4a4b9319a0bca63771516ea91923533ddb8320` (tree `317e603627de244f038b5110401910efc87a2396`). The follow-up adds a Unity EditMode integration regression only.

## Review finding addressed

The prior independent review returned `NEEDS_CHANGES` because the cleanup test
seeded private fields by reflection and did not exercise an actual
`RestoreRequired` transaction or verify one-use proof revocation. The updated
`PersistedDailyV1ImageRestoresWithoutUsingSourceSessionAndContinuesIdentically`
case now saves a real completed Daily-v1 image, starts a fresh application,
runs `P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage` from
the `TryStartOnExistingApplication` projection callback, verifies the exact
image-bound proof and private prepared references exist, then rejects that
projection before publication. It asserts the active session remains absent,
the prepared image/session references are released, and the exact proof can
no longer be retrieved. No assertions were weakened and no production code
changed in this follow-up.

## Validation

- Official focused EditMode run `15aa79b147d24f948e923f877ecb9b8f`:
  **1/1 passed**, zero failed/skipped/inconclusive; stable input; source commit
  `4d9ef8186c70a654b2a4e006eb36671c7fee3bac`, tree
  `a126b7ed58d92288c9341403c3495d1935f216e3`. Original focused XML SHA-256
  `6b148901e3348e0118151be1542eaf6927345894158c3936b5b5fa69adce7c60` and
  log SHA-256 `cd8cee3e01eac4cce021344a1e684ca02ba15c594c92ad6f46eef968a58e7017`
  remain in protected local evidence retention.
- Official full EditMode run `63e61a16c26f4bc5b7d6817937e9e15e`:
  **3303/3303 passed**, zero failed/skipped/inconclusive; stable input; same
  exact commit/tree; Unity `6000.3.9f1`. Original XML SHA-256
  `3d61bfe7884b88a7062f189ea95f1acbe40d15dbdd4829e13b5e473ff6e68a91` and
  log SHA-256 `cd94554a5a578105beec699a1ea1261f672b47dce4da0b717fbd7b2cf4742adb`.
- All five official Smoke identities are present and passed in the full
  EditMode report. The updated restore-continuation case is included and
  passed. No separate PlayMode Smoke run is claimed.
- The complete public result package was generated and verified by the
  official Unity evidence adapter against the protected original run:
  [`Unity package manifest`](Unity/core-f-restore-start-transaction-4d9ef818-editmode.manifest.json)
  (SHA-256 `49d44831ee23e34b32b28ab6f7d2da9ba0a32797564b0d7f237bd54c29fabf16`)
  and [`normalized XML archive`](Unity/core-f-restore-start-transaction-4d9ef818-editmode.zip)
  (SHA-256 `e5811981150caef4efb43bf9a32b5cb1c638165a235b11fb7d328b30612619ac`).
  The archive contains normalized result XML only; raw reports and logs remain
  in protected local retention.
- CoreValidation cold/warm remains **1088/1088 each** from official run
  `1c96d57919a94dfea9c707b7565ff3dc` on the unchanged Core/test inputs. This
  follow-up changes only a Unity EditMode test; CoreValidation was not rerun.
  The earlier run details and protected raw TRX hashes are preserved in the
  original `VALIDATION.md`.
- `git diff --check`: PASS for the test follow-up.

## Scope and review status

The new test closes the prior cleanup-path test gap; it does not complete
P12-G, establish detached .NET hosting, claim full CORE-F readiness, or
propose Core canonical promotion. Fresh independent exact-tip review of this
candidate and its validation artifacts is pending.
