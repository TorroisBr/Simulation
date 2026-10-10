# P12-G Internal Stage Failure Injection — Validation

Implementation base: `4f270d222988a3da2a5e3219e900d9b8ef079566` (`codex/phase12/canonical`).

Staged `Assets` subtree hash for implementation and tests: `d69382db91a9236a5b539d015c383c6a6e45393c`.

This candidate adds a nullable, attempt-scoped observer on the existing restore staging path. With no observer supplied, production behavior is unchanged. The test observer throws after named private root/owner staging and validation boundaries; tests reuse the existing restore rejection harness to verify that the original active session remains healthy and a later restore succeeds. Existing `P12CContinuationRootStager.TryStage` signature is preserved for reflection-based callers; restore injection uses a separate overload.

## Validation

| Run | Result | XML | XML SHA-256 | Compressed log | GZIP SHA-256 |
|---|---:|---|---|---|---|
| Focused | 19/19 passed; 0 failed; 0 skipped | `EditMode-20261010-190810-6a333f5b246f436f9d290b2eaa96ef75.xml` | `41D3D09EE7679465A887C322332C7ABE9DCAEF053397A6B4302EA5910A7B7BED` | `EditMode-20261010-190810-6a333f5b246f436f9d290b2eaa96ef75.log.gz` | `924799C766B3D7BDB1352CBC67F23606884444AA6A294CAE5DC35FA959AACC36` |
| Focused | 54/54 passed; 0 failed; 0 skipped | `EditMode-20261010-191043-eacc2062f0d34cfb9b09c15943e955f6.xml` | `5C286DD2A36303D3DC55F74848414A6C4F8B3DE05394DE7C65BA6F5954ECFE90` | `EditMode-20261010-191043-eacc2062f0d34cfb9b09c15943e955f6.log.gz` | `11F6BC5565337EB701B00FAB6B31997FC8EEAD5BBE0FBA76900B85BC05CF6C04` |
| CPrivateRoot | 53/53 passed; 0 failed; 0 skipped | `EditMode-20261010-191228-e8e279ba64934ac7bbbd76d401cbba6c.xml` | `2334E91EAF45F2339B68DEC8B2205CB26F5A852981A7912D5859CE5CCF68AA1B` | `EditMode-20261010-191228-e8e279ba64934ac7bbbd76d401cbba6c.log.gz` | `F09AAB595A83C385CF34B7807C4F7F7A019FC012037C0C5E8773CB56F6B85790` |
| AllEditMode-Retry | 2845/2845 passed; 0 failed; 0 skipped | `EditMode-20261010-191244-6052500eadf947ab8193537e51f4f957.xml` | `D6A0EA3AE0D6735D1C8033C9054A78D32CF8A67F6D2CD268553BBBA2AA27D9C3` | `EditMode-20261010-191244-6052500eadf947ab8193537e51f4f957.log.gz` | `2CA72F71916B7D5CA351BF202B6D0FE739D82DF27571FB89D17361CEFE80F697` |
| Smoke | 5/5 passed; 0 failed; 0 skipped | `EditMode-20261010-191355-e78a512000ad4957a5c2e563c09e4ab7.xml` | `C00E0F13A74EDD26672DB430A59B944FDFF975BEFE76F7839A0AF5397EB4E5A7` | `EditMode-20261010-191355-e78a512000ad4957a5c2e563c09e4ab7.log.gz` | `3883F26305945B6A953E341ECF036F9151A5A69E7DB39A69561778ED55DE6AC6` |

Focused restore cutpoint coverage: 54/54 PASS. P12-C private root composition: 53/53 PASS. ALL EditMode: 2,845/2,845 PASS. Official Smoke: 5/5 PASS. `git diff --check`: PASS. Each compressed log was decompressed and compared by SHA-256 to its raw log before packaging.

The initial non-final ALL EditMode attempt exposed 51 failures caused by a first draft changing a reflected `TryStage` signature. The original signature was restored and a separate restore-only overload added. The retained final ALL EditMode report is the all-pass rerun above; the failed XML is intentionally excluded from this manifest.

## Preserved user files

No ProjectSettings or user `.meta` files were staged or modified by this candidate. Post-validation SHA-256:

- `ProjectSettings/EditorBuildSettings.asset`: `58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28`
- `ProjectSettings/ShaderGraphSettings.asset`: `5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7`

P12-G scope remains open. This slice covers injected private-stage failure atomicity; it does not claim complete graph corruption/rejection coverage, all callback non-replay semantics, P12-A readiness, P12-B completion, export, hydration, or Phase 12 closure.
