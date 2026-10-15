# CORE-F null-roster provider-switch implementation review

## Verdict

**VALIDATED_CANDIDATE — exact-tip independent implementation review.** Independent reviewer `/root/core_f_design_review` reviewed evidence tip `cc7c579c64097ec90eac8356e8f6ea61483b62f7` against Core canonical base `64610d6c4a5cfaec2c9e14fdb70b5e7ed5d5aeaa`. The implementation commit is `6a1e3a5f49284152159086e2bb22aa50cda510ad`, tree `4aae462cfee091705bf3e84a5611ee39202507ea`. Current architecture canonical is `4b69e43516b3fc548611ce704ef45af9e54e3d0e`.

The reviewer confirmed that the evidence-tip changes after the implementation commit are documentation and validation artifacts only; production and test source remain identical to the reviewed implementation tree. No finding blocks promotion, subject to the normal immediately refreshed canonical/ancestry/evidence preflight.

## Scope and behavior

The implementation switches only the frozen P18-D Plan/Edge provider encoding and decoding call sites to the promoted shared codec, using lossless private DTO conversions. The independent Unity oracle fixture uses test-owned mirror DTOs and a test-owned legacy reader. Review confirmed that activation-payload byte comparisons, provider validation order, IDs, ordering, replay semantics, owner authority and commit boundaries remain unchanged. The shared codec, numeric helpers, owners, runtime, P12 persistence and host/restore code are untouched.

The `runtimeRoster:[null]` test confirms decode succeeds with one non-null default-valued member whose two IDs are null. Provider rejection occurs before a prepared commit or authoritative mutation. The tampered activation-evidence and aligned test-seam paths retain the populated snapshot and Knowledge state, observations and revisions unchanged.

## Validation identity

- Unity `6000.3.9f1`, Test Framework `1.6.0`; focused provider, oracle, codec parity, P18-D consumer and daily-boundary suites passed as listed in [`VALIDATION.md`](VALIDATION.md).
- Unfiltered ALL EditMode run `fbe1bf9a0eeb4fbb99c3e7d628bd5eb5`: 3,176/3,176 passed, zero failed/skipped/inconclusive. The complete report includes the new rejection test, oracle coverage and all five official Smoke case identities.
- Official Smoke filter `019d04646dd54c89abf06ef1eb2258fb`: 5/5 passed.
- Unity input snapshot SHA-256: `0d02b39ec8938787f9c89cc3d52588b3dc222e172383de5e686b50bdd6029a4d`, stable before/after at implementation commit/tree.
- Unity result package archive SHA-256 `236f5f9ffc4d03ecc955df49d8aff8f22e4385a208c7dd3c390cec0c7511da24`; public manifest SHA-256 `1ebed0f93882fe404f66fb7ec67805511a391bfb23f2d7eca4c3c6e5c6b9c2d2`. The approved verifier passed against the protected original. The package has one normalized XML entry and no detected local workstation paths.
- Extra-input sidecar [`core-f-null-roster-extra-inputs.json`](core-f-null-roster-extra-inputs.json) binds the three tracked compatibility CSVs by bytes, SHA-256 and Git blob before/after the full run; all identities match.
- Fresh CoreValidation run `cd83d30bff0f462193ac17a8d127bf21`: cold and warm 171/171 each, all other result counters zero. Its 142-file `(path, bytes, sha256)` input inventory exactly matches the prior packaged run; both have stable digest `4d0cb6088db8d71986ec076ef150fb15fedff80d60dacdab562470d57c80d3bb`. The prior privacy-safe TRX package remains applicable under the handoff's unchanged-input rule; its archive hash is `6917029217d029e4bf7c8f752aced893bdf0a4a476cd242eec5ab539d05ec878`.
- `git diff --check` passed for the candidate. No `ProjectSettings` files or unrelated worktree changes are in the candidate; protected local changes remain unstaged and uncommitted.

## Promotion boundary

This verdict validates the bounded provider-switch candidate; it does not itself promote code or close CORE-F. Full CORE-F remains `WAIT_DEPENDENCY` on selected-owner source/writer/epoch closure, shared runtime and restore composition, detached restoration, and the .NET host/continuation contract. No broader P12 profile, CORE-X, or P13 readiness is claimed.
