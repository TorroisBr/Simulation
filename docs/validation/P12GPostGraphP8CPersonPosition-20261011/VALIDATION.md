# P12-G post-graph P8-C Person position validation

- Canonical base: `6d334011ed428873c92cc66f492ee94c2cf2152b`.
- Test-only code commit: `b1a854cf2d2b621240cb3fab4bcce5d4bce5d72d`; Git tree: `67efac7bb24d3109178578a647e6d69161a0313f`; `Assets` tree: `2a0d93e07898deecca2600db8b93a13ae7ffb4da`.
- Test source blob: `42e08eef6f470f2c86ec7e2009405c6122072f52` (`Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`).
- Production code, profile, and runtime behavior are unchanged.
- The code commit was validated before evidence packaging; artifact sanitation changes no executable or test-source bytes.
- `git diff --check`: PASS.

| Run | XML totals | Test cases | Original XML SHA-256 | Sanitized XML Git-blob SHA-256 | Original log SHA-256 | Sanitized log content SHA-256 | Sanitized gzip Git-blob SHA-256 |
|---|---|---:|---|---|---|---|---|
| focused | 1/1 passed, failed 0, skipped 0, inconclusive 0, assertions 0 | 1 | `efdc985f9d6027ef2e39c6e767d2cd84a36bf1afc3bc1c256d2b11994d7d0cdb` | `9a2c5a3539b73df0bb6c8fa04ef9624c5e785b24` | `5d8f8c0444a3152fab4e4a49976b1d1dfac7047987c9dace6b14d97f48c62389` | `2d11d606f384cf6496282362cb3d4da9952954b21af6e7556246d3be7dda3061` | `3007570933a12c67b4e255a15ea6194b60a4194f` |
| admission | 195/195 passed, failed 0, skipped 0, inconclusive 0, assertions 0 | 195 | `9fb12c291457969299499d94e7d5cea54f8d6cead4ccfc347626e42464da7fd3` | `19529fbd6c49e8a8039e5bf0189cfe81ad3010bd` | `4ff97354e88944d65579e7c91288a1f1e4b5ca3de536166184ee164ba216836a` | `fa0b91f6219f75a477fed0061fe8876d3d5e6fd5c00a22e5fc934b9a741ffad4` | `9f36e205597e80136f718bb4b679432e92d7e77c` |
| editmode-all | 2874/2874 passed, failed 0, skipped 0, inconclusive 0, assertions 0 | 2874 | `996f8a986d42435465bc7f0b0679d4f98f4a78b2ace407a6ab753d5604a24a47` | `b7aaed07f6b719a9d1309b6935fdf4275b5ee827` | `126f4d5c71180675f24095bf4ac3f824be290c8938cc8ab516b3fffd98a94468` | `df790a83567b662d014741d0e2dc9bf7b00e631aa716d1112fee246383107a3a` | `a94d41344f3f8492f7da1cb38c8a3fc44bc1faf6` |
| official-smoke | 5/5 passed, failed 0, skipped 0, inconclusive 0, assertions 0 | 5 | `93c5391fac2bcaf3fe658cc902b83089f1422cb3f5cb5b3acbbaf6c781ecb063` | `e1dee61717f322e63ee6beba5c092553f51280d2` | `a12ffe4b69fd5104437e4bed81e3fb56dff5db7d3283911023b82b215b8c2885` | `c0ce9e65dd9a8a0929396ff1140afef5cdee86a828624330d09ed1b1a18ec974` | `99b82b5ed54d5dacf2956eb5fcbd38d327851b28` |

The focused test proves one valid P8-C Person position write after restored-candidate guard binding changes only the exact-empty owner row's cardinality/revision witness, with the observed shared census epoch unchanged. The final target census rejects the private candidate before boundary admission or publication; source preservation, continuation parity, and retry are covered by the same test.

P12-G remains `WAIT_DEPENDENCY`; this is one bounded target-owner recapture witness. It does not establish complete P12-G owner coverage, global shared-epoch coverage, runtime-wide quiescence, capture eligibility, export/hydration, P12-A/P13 readiness, or Phase closure.
