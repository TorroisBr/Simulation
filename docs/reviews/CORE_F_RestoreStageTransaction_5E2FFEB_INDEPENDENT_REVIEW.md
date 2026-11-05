# Independent exact-tip review — CORE-F restored-image stage transaction

**Verdict: PASS — VALIDATED_CANDIDATE**

- **Candidate branch:** `codex/core/CORE-F-restore-stage-transaction`
- **Candidate tip:** `5e2ffeb27df97be00101ae3ce18f4b36b92ed4ed`
- **Candidate tree:** `0f4503a6fb1d7a2d96533b08ad98c629740bc2e6`
- **Implementation commit:** `95486e8e669a8b9018e6c89da442b95c8022354e`
- **Implementation tree:** `6980a266d08ba91f0dce2a07290126764301f70d`
- **Exact base:** `552966366307bde2fe15764a6dfcb77856d7eb38`
- **Core canonical at review:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- **Remote preflight:** candidate, implementation parentage, and canonical matched these identities.
- **Review completed:** 2026-11-05

## Review findings

The implementation moves ordering for the application-backed restored-image transaction into `DailyV1Application`. Core first verifies exact initial/replacement admission, compatibility, census and decoded image, then stages the detached P12-C roots. The Unity adapter projects those private roots and prepares its host-specific definitions/logger. Core then stages P12-D, creates the record authorities, stages P12-E and P12-F, and composes the private Core session. At each completed owner boundary, the existing Unity adapter constructs its compatibility projection; the callback returns only success or a diagnostic, and cannot substitute Core stage values into Core composition. The unchanged legacy non-application path remains separate.

The staged token retains exact application, image, definitions, root, expected-session and replacement-operation identities. Core stage methods enforce the D → authorities → E → F → session sequence. A rejected stage or projection calls `Fail`, invalidating the token and clearing staged owner/session references. The outer restore path still performs owner-graph/binding and sentinel validation, initial target-vector capture, guard binding, post-bind recapture, and restored-boundary admission before returning a candidate. The later application publication gate remains the only publication point. Failed staging leaves the candidate private; it does not publish a session or write continuation-slot bytes.

The review found no actionable issue in stage ordering, P12 failure atomicity, token invalidation, mutation-guard/finalization sequencing, or publication boundaries. The changed .NET admission test retains the prior rejection assertions and adds checks that a missing exact initial admission invokes no decode, root-projection, host-preparation, or owner-projection callback; it does not weaken assertions. The full candidate diff is limited to the two production files, one Core test file, and its validation note/manifest/archive.

## Validation evidence inspected

The manifests, source identities, original report results, and packaged Unity archive were checked. The changed code/test file SHA-256 values match the exact implementation candidate:

- `P12GDailyV1RestoreCoordinator.ImageRestore.cs`: `0a19a3d5b2270dffcc33e3e5a528fb378c6adee776a762278aca58805ee06a1a`
- `DailyV1Application.cs`: `20d88718b03b3ca9ca71d470b22e324f49eccadffcc2998a9addc951ead50288`
- `DailyV1ApplicationHostTests.cs`: `b9e6c58c03e5da2b460ceb69a3df5188c022719f42e21aa20713e5ffd43ea30b`

| Gate | Result | Evidence identity |
|---|---:|---|
| Focused `DailyV1ApplicationHostTests` | 21/21 PASS | Run `e4288d772dc948099181c9ccff291b4b` |
| CoreValidation cold and warm | 1017/1017 PASS each | Run `9213936a22644e5d8ca091c53f39db8e`; stable input digest `f73659c3be8a72273b1881b28df16086a70bed0add7e0e2a9364c6994b4c7506`; both changed Core/test file hashes match. |
| Focused Unity restore rejection | 1/1 PASS | Run `29488bc5d346475eb80483612aecfe2b`; stable input digest `9d1f7eda4f5d1ce207016431086d7cd3379758c8f9228655e78f64e95665dbc7`. |
| Full Unity EditMode | 3299/3299 PASS; zero failed/skipped/inconclusive | Run `682631f373c7425c8efb9d2f79cdd96b`; stable input digest `9d1f7eda4f5d1ce207016431086d7cd3379758c8f9228655e78f64e95665dbc7`. |
| Official Smoke | 5/5 PASS | Run `e5df7701a2e544e887df220fff631666`; same stable Unity input digest. |
| Unity evidence package | VALID | Full-suite archive SHA-256 `5307a513f686f6e35daef17ac4b586937ad6239214546cc7727743ac17657004`; manifest SHA-256 `45e8b2304541056e0b41a54fc3daa80789cb1b7fa998b38801ec7a8d8ca1f5d0`. |
| `git diff --check` | PASS | Rechecked for the exact base-to-candidate commit range. |

The CoreValidation native manifest and original cold/warm reports remain in protected local evidence and are not copied to Git. No public .NET TRX package is claimed: the Core runner emitted `simulation-core-validation/v1`, while the separate approved adapter requires `dotnet-trx-source-context-v1`; the validation note does not fabricate that context or rewrite reports. The Core run also records that its worktree had a Unity Library, so it does not prove Library-independent .NET validation. These evidence boundaries are accurately disclosed and do not change the verdict for this bounded implementation checkpoint.

## Scope limits

This is a bounded integration checkpoint. It does not deliver a Unity-independent restore producer, eliminate Unity host projection/authoring construction from the restore path, establish standalone .NET restore or complete `DailyV1Application.TryStart`, demonstrate T/T+1/T+2 continuation, complete CORE-F acceptance, or promote Core canonical. The existing Core canonical remains unchanged.