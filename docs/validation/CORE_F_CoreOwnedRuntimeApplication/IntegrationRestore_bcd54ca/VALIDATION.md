# CORE-F application restore integration checkpoint

## Candidate identity and scope

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Integration commit: `bcd54ca87c7741252ebc26978889a2a5464ae9c0`
- Exact source tree: `ed717d5a74c1ca75b1c8dccc1703d1a3d1b24051`
- Merge parents: `7292e2f4b3ce5d0a46af926932c5b332ec39cd8f` (P12-D City/Location cardinality fix) and `a61b980d3e768ba3e47b152f54bddb6e31dd5e7e` (reviewed Core application restore work)
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

This checkpoint combines the exact one-top-level-City-per-Location invariant with the previously reviewed Core-owned initial continuation admission and P12-C/D/E/F staged restore integration. The invalid initial host-load finding is fixed: invalid initial input now blocks genesis on an unused application, while a completed `NoSave` outcome remains the only load result that permits genesis.

The Core application owns initial continuation-load admission, staged restore boundaries and publication checks. The Unity bootstrap and restore coordinator still contain host responsibilities; this is not the complete public `DailyV1Application.TryStart`, a detached runtime host, or full CORE-F acceptance. No canonical promotion is claimed.

## Exact-commit validation

| Gate | Result | Evidence identity |
|---|---:|---|
| Official CoreValidation cold | 901/901 passed | run `d2d2dbe638ca43909df10c6c9c1b6924`; cold TRX SHA-256 `699a178890c7f096ed22eb50af5dd3f802be25f9944c34b38bef75cc3a0d811e` |
| Official CoreValidation warm | 901/901 passed | same run; warm TRX SHA-256 `dcdf35afc00779a91b564c3d67af45c2d34ba456727736e10bfbdfd3b9182c5a` |
| CoreValidation provenance | PASS | manifest SHA-256 `6417800d69dd29cda332510d6904010161b9e726b095442ade5a7f4f88310dda`; source commit/tree match this candidate; zero worktree changes; Library absent; Lab build and cold/warm scenario passed |
| Full Unity EditMode | 3274/3274 passed; 0 failed, skipped, or inconclusive | Unity 6000.3.9f1; run `5cc8df1da7644db99348d306c0d79d22`; stable input digest `4b27b7b16a32d83848862528758b9813927a2971b0b548664ea0f87de867e3e7`; original XML SHA-256 `7e23251270fc4f99496770c913cd71dfb38611a5245951819ccb15482ebeddc3` |
| Unity normalized result package | Verified | package manifest SHA-256 `75a3f4c8ad10554cba2d22957451b4fe057c57a2b718f7b84e24a0bdf52c667e`; archive SHA-256 `4a2752b219d336f1b9ae8d665e34fb11858a5101009fd5ae85c68b56100b7b57`; official verifier PASS, one normalized XML entry; privacy scan found zero local-path or credential-like matches |
| Official Smoke | 5/5 passed; 0 failed, skipped, or inconclusive | run `71fa1d1d82684e9587fefd109fc070e2`; stable input digest matches full EditMode; original XML SHA-256 `601080ffa6fc0ae4343dae9932513a0f549b1ebcda7ce175b5c50ba7a9712808` |
| `git diff --check` | PASS | checked over the integrated candidate range |

The initial Unity import run reported 3274/3274 but changed only the two generated ProjectSettings inputs; it is not counted. Its private originals are retained. The stable rerun above is authoritative. CoreValidation TRX/logs and Unity original XML/logs remain in protected local retention. The candidate includes only the normalized Unity package; raw reports and workstation paths are not published.

## Independent review

The component commits were independently reviewed on their exact source identities. The initial-load fail-closed correction received **PASS** at review commit `68de89ee418a17d02dfa381300b53c7bb4302b30`, whose record is `docs/validation/CORE_F_DailyV1StartRestore_20261027/FOLLOWUP_REVIEW_b2a834b.md`. That reviewer could not independently access or rehash the protected validation artifacts. Exact-tip review of this merge integration is recorded separately before candidate acceptance.

This checkpoint advances runtime restoration but does not close CORE-F, publish an incomplete `TryStart`, or change Core canonical.
