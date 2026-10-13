# P12-A detached Daily-v1 continuation validation

## Candidate identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Branch: `codex/phase12/P12AContinuationImplementation`
- Canonical base at implementation: `25d2dcf1371137ceb0e158eef4ba02d2e2304612`
- Implementation commit: `5b6b0061b06aa4191db000070aa10ef23abd775c`
- Implementation commit tree: `4ab80fbedad49fbc609c4027ab16c1a808226120`
- Implementation parent: `7438e5a9850641249c076f5a9172d11cf4939c95`
- Unity: `6000.3.9f1`

The final implementation commit contains the tested source and test tree. After validation, `git status` showed only the two protected `ProjectSettings` edits, three unrelated untracked `.meta` files, and this validation/package directory. None of those protected or unrelated files is in the implementation commit.

## Validation runs

All three evidence-captured runs used the same 1,531-input inventory (`1,481` inputs under `Assets`), inventory SHA-256 `d6bfc208d450698782f62766c715896e922ee4300eaa961a1a936078498fa69e`, and reported `inputStable=True`. The executable and test source tree did not change after these runs.

| Run | Selection | Result | NUnit XML SHA-256 | Log SHA-256 |
|---|---|---:|---|---|
| `6be2044743384bc59335ee482473a97a` | Focused P12-A, owner-package, invalid-save, and restored-session suites | 140/140 passed; 0 failed, skipped, or inconclusive | `e9d51eacf9fef4508e4b80277ed6dc8a3ea8622bd266a2e911389c766bf166bc` | `7a2e879b4eaa973e06084d0ac0062acccce06e467990e8ded5ebf7fa7e741feb` |
| `0025dbb157c84d18bc578a79ab24fb5f` | All EditMode tests | 3059/3059 passed; 0 failed, skipped, or inconclusive | `0f93eae6102e7a617783375122d2a2d338aa74190680bbaded20dc8946d7688` | `3a6311f3e6aaf62bda412d326622a2dadad8b5e34aef3c7f359d13c195f855d3` |
| `c28e84b6f4bc4c26a67489545a2f40f6` | Official Smoke | 5/5 passed; 0 failed, skipped, or inconclusive | `535766ce9c5930534a9a451cc7d3f78ead64cad45332ba7a53e98f38fdb2f588` | `5c3a4c3900643e110f4c2f6bda1a9f14eba2c394230b3a9333fb81a6d4e8bcbc` |

## Packaged evidence and integrity

The promoted Unity validation evidence tool successfully packaged and verified the complete EditMode run:

- Archive: `p12a-all-editmode.zip`
- Archive SHA-256: `68ae5a347289b4755f9e5cfec17728f51b3e1d1c56cc7f62b7e38a7c8f0bdf27`
- Manifest: `p12a-all-editmode.manifest.json`
- Manifest SHA-256: `275D1117F0F7A1F69620A6DC771A314D89891DE8F82A892F304BB948E70EC56E`
- Archive size: 159,808 bytes
- Verification: PASS (`runs=1`, `entries=1`; archive SHA matched the manifest and the protected full-run manifest)

The evidence tool rejected separate packaging attempts for the focused and Smoke XMLs because each filtered report retained the full-suite root `testcasecount=3059` while containing only its selected descendants. The XML was not edited and no validation requirement was bypassed. A separate identity/result comparison against the successful complete EditMode report found all 140 focused and all 5 Smoke test identities, with zero missing cases and zero result mismatches; all selected cases passed. The complete EditMode archive therefore preserves the focused and Smoke case-level results while the run records above preserve each selection and its own log/XML hashes.

## Additional checks and scope

- `git diff --check`: PASS. The only emitted notices were line-ending normalization warnings for the protected `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` files.
- Production scope is the accepted selected-profile, same-build/current-host Daily-v1 detached capture, durable storage, load, restore, and deterministic continuation contract. This validation does not claim P13 readiness, Phase 12 closure, or support for profiles outside the reviewed contract.
- The final pre-review Git preflight must refresh remote refs and confirm the implementation branch still descends from the current `codex/phase12/canonical` tip. No canonical promotion is represented by this record.
