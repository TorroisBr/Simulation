# CORE-F null-roster provider switch validation

**Status:** promoted to Core canonical at `3e817bf14c2d4e9e0f5fc30e48d14197d4450875`. This record covers only the bounded P18-D provider call-site switch. It does not claim full CORE-F completion.

## Candidate identity

- Core canonical base: `64610d6c4a5cfaec2c9e14fdb70b5e7ed5d5aeaa`.
- Implementation candidate: `6a1e3a5f49284152159086e2bb22aa50cda510ad`.
- Candidate tree: `4aae462cfee091705bf3e84a5611ee39202507ea`.
- Architecture contract: `4b69e43516b3fc548611ce704ef45af9e54e3d0e`.
- Shared codec prerequisite remains the already promoted P18-D codec and numeric-helper implementation; no shared codec, owner, runtime, persistence or P12 source was changed here.

The implementation switches the frozen Plan/Edge encode and decode call sites in `P18DCommercialSharingBoundaryProvider` to `P18DCommercialSharingCodec`, using lossless private DTO conversions. The oracle fixture now uses test-owned mirror DTOs and a test-owned legacy reader instead of reflecting over provider-private types. No change was made to raw activation-payload comparisons, validation order, transaction boundaries or owner authority.

The null-row test verifies that `runtimeRoster:[null]` decodes to one non-null member with both IDs null, while provider validation rejects the invalid member before preparation or owner mutation. Both the tampered-payload and aligned-evidence test paths assert no prepared commit, unchanged retained state and unchanged sender/recipient Knowledge observations and revisions.

## Unity validation

Unity `6000.3.9f1`, Test Framework `1.6.0`, evidence packager `1.1.0`. All listed final runs bind to implementation commit `6a1e3a5f49284152159086e2bb22aa50cda510ad`, tree `4aae462cfee091705bf3e84a5611ee39202507ea`, with stable input snapshot SHA-256 `0d02b39ec8938787f9c89cc3d52588b3dc222e172383de5e686b50bdd6029a4d`.

| Gate | Run ID | Result |
|---|---|---:|
| `CommercialKnowledgeSharingTests` | `2c63fc4b209047da90e22a5f9acf44d9` | 11/11 |
| `CORE_F_SharedRuntimeOracleCaptureTests` | `e885cc0b70a94b76acb6e0e1d89b226c` | 4/4 |
| `CORE_F_CompatibilityParityTests` | `0bdb984e7da04a2caf19224efe1399e9` | 2/2 |
| `P18DConsumerIntegrationTests` | `fa5756088d4845b98e103a4b3dd216bd` | 10/10 |
| `P18DDailyBoundaryOwnerTests` | `e17a8bd4a2304f9d8458462304c746ee` | 2/2 |
| `P18DDailyBoundaryStepProviderTests` | `1e6adb9f0d5e4092800b025b61a1bd31` | 6/6 |
| Official Smoke filter | `019d04646dd54c89abf06ef1eb2258fb` | 5/5 |
| Unfiltered ALL EditMode, including retained LongRun cases | `fbe1bf9a0eeb4fbb99c3e7d628bd5eb5` | 3,176/3,176 |

Every run passed with zero failed, skipped or inconclusive cases and stable before/after input snapshots. The full run derived its 3,176-case count from the current test inventory. The approved evidence packager normalized and verified the full EditMode report against its protected original; the ZIP contains only normalized result XML, while raw reports/logs remain in protected local retention.

- Public Unity package manifest: [`core-f-null-roster-all-editmode.manifest.json`](core-f-null-roster-all-editmode.manifest.json), SHA-256 `1ebed0f93882fe404f66fb7ec67805511a391bfb23f2d7eca4c3c6e5c6b9c2d2`.
- Normalized result archive: [`core-f-null-roster-all-editmode.zip`](core-f-null-roster-all-editmode.zip), SHA-256 `236f5f9ffc4d03ecc955df49d8aff8f22e4385a208c7dd3c390cec0c7511da24` (166,948 bytes). Verification passed with one run and one result entry.
- Additional-input sidecar: [`core-f-null-roster-extra-inputs.json`](core-f-null-roster-extra-inputs.json). It binds the three tracked P18-D oracle/parity CSV inputs by byte count, SHA-256 and Git blob before/after the run; all three remained unchanged.

## .NET regression and evidence reuse

Fresh CoreValidation run `cd83d30bff0f462193ac17a8d127bf21` passed cold and warm: 171/171 each, zero failed, skipped, inconclusive, timed out or aborted. The source and input inventory are stable. Its 142-file effective .NET input set has digest `4d0cb6088db8d71986ec076ef150fb15fedff80d60dacdab562470d57c80d3bb`.

The reviewed prior cold/warm TRX package is [`core-f-compat-6431fad3-cold-warm.zip`](../CORE_F_SharedRuntimeSource/core-f-compat-6431fad3-cold-warm.zip), SHA-256 `6917029217d029e4bf7c8f752aced893bdf0a4a476cd242eec5ab539d05ec878`; its public manifest SHA-256 is `a52bc16fa63fe1c6ed7a3a328e450b68e03a72b40c19962846cfb077d059d67a`. Current and prior CoreValidation manifests have identical 142 path/hash entries and the same stable input digest. The provider and its Unity Editor tests are outside that effective .NET input set, so the prior privacy-safe cold/warm package remains applicable. The fresh rerun's raw TRX remains in protected local retention; no unsupported source-context adapter was fabricated.

## Gate disposition and boundaries

- `git diff --check` passes for the implementation candidate.
- The protected `ProjectSettings` worktree changes were excluded from the candidate and left unstaged/uncommitted. They remain outside the implementation source identity.
- Unity provider-switch, null-roster rejection, failure atomicity, codec parity, P18-D integration, daily boundary, consumer, Smoke and full EditMode gates pass.
- Independent exact-tip implementation review passed for evidence tip `cc7c579c64097ec90eac8356e8f6ea61483b62f7`; the source/test tree remained unchanged in the promotion commits.
- The implementation was fast-forwarded to `codex/core/canonical` at `3e817bf14c2d4e9e0f5fc30e48d14197d4450875` after refreshed remote, ancestry, source-tree, package-integrity and diff-check preflight.
- CORE-F remains open and `WAIT_DEPENDENCY`. Selected-owner migration, shared runtime/restore composition, detached restoration and .NET host continuation remain separately gated. No broader P12 profile or CORE-X/P13 readiness is claimed.
