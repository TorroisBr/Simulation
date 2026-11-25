# CORE-F P12-D City image codec validation

## Candidate identity

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Base: `6f25db05ee22fc32ebe3d48bdd52aad50f7421da`.
- Code candidate: `8c1ec798880c64048df0c7bc9d421212b5006560`.
- Code tree: `d29ca2830238c8442962d9be331e07f3e2a6c0bb`.
- Scope: move the pure selected City image-section codec and dynamic City-set decoding into Core; keep live City capture/projection and conversion to the Unity snapshot type in the Unity adapter.

## Validation

- Official CoreValidation: **786/786 cold and 786/786 warm PASS**. The locked build, CalendarDemo, and Lab gates also passed. Run `567ed30981664973ada3b2a3464f471f`; protected runner manifest SHA-256 `a16231d87f577f5567362a21612422ce28e03888e17493310c0c574d90fe1cff`; cold TRX SHA-256 `55387ae199ebf45e408c21d265850f8b26ca75e6ac4cb723128fbc831a8f66ca`; warm TRX SHA-256 `4862c8b6e3cb9271ad3f948f32c49b916462c356c9f63b557c508eb81c4ed2f1`. The runner recorded stable inputs and working-tree input digest `e876a5a98a80346a0d329c24452cf71130fa6bf7434b3d5866863b2e6b979adf`. Original reports/logs remain in the owner-only E: evidence store.
- Focused Unity EditMode: `P12ACityOwnerSetCodecTests` **2/2 PASS**; `P12DCityRootOwnerSnapshotTests` **18/18 PASS**.
- Full Unity EditMode: **3264/3264 PASS**, run `baac14cf9ec54f87800f4d9fc7c186ac`; original XML SHA-256 `d9aa13eefb69cebe73d10424f54c41a643eb615fb535f13bab4e57a10f4bd892`.
- Official filtered Smoke: **5/5 PASS**, run `ab2c14dad18b4204b5eb198d915504eb`; original XML SHA-256 `e09de715beec36b55acd5b4afccaf39e8122284baba0ed9d4552c99f4588d2cb`. The approved evidence packager rejected a separate package for the filtered report because its `Simulation` suite count did not match its descendants. The original remains protected; all five Smoke identities and `Passed` outcomes were verified in the packaged full EditMode XML.
- The full EditMode XML-only package passed package verification. Archive: `p12d-city-image-codec-all-editmode.zip`, SHA-256 `e24787baf7fb751e41b0bc1050f0b35ed3cc5d45dd30d8cd7c1e5f6a801b856d`. Public manifest SHA-256 `d97af5a630242b869eec16b664472ab4812303bbc825ff88b6098e903ac83231`.
- CoreValidation original TRXs and protected runner manifest are retained privately and identified above; this slice has no public DotNetTrx archive. No raw log or private source manifest was added to the repository.
- `git diff --cached --check` passed before the code commit. The Unity runner reported `inputStable=true`; captured Assets/Packages hashes match the committed code inputs. CoreValidation's input inventory likewise matches the committed Core/test inputs. The CoreValidation run occurred with a Unity Library present; no Library-independent claim is made.

## Scope and limitations

Core now encodes and decodes the existing four City owner sections and validates the complete dynamic City set before returning detached hydration values. Section IDs, schema/alias identities, cardinality checks, ordering, wire payloads, row revisions, receipts, and failure-atomic publication remain covered. Unity still captures live City owners and adapts decoded Core values to the Unity snapshot projection.

This is an intermediate CORE-F candidate. It does not complete whole-image owner composition, P12 restoration, the shared application/session, the detached .NET host, or CORE-F. Core canonical was not promoted. Protected ProjectSettings edits and unrelated untracked files remained outside the code commit.