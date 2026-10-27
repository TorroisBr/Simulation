# CORE-F P12-E/F Application Staging — Exact-Tip Review

**Verdict: VALIDATED_CANDIDATE**

Review completed: 2026-10-27. This record is an independent implementation review. It does not promote the candidate, close CORE-F, or authorize a later integration beyond the reviewed scope.

## Exact identities and applicability

- Repository: `TorroisBr/Simulation` (`origin`: `https://github.com/TorroisBr/Simulation`).
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`.
- Candidate tip reviewed, including its validation record/package: `d27cb342b38bd8b841a325dc7b1c6efbe4640adf`, tree `0ab3ebe948b1d94a80653bc18e58fabeb26607d0`.
- Implementation commit reviewed against its actual parent: `383af5ab8a7261ad6ba9178ef74928936909b8e1`, tree `814bcf1b51637d0a2e9c6bb861ce1a276103bd19`, parent `398bbd50fd43921082ea5f3eaca8d64b29ca5814`.
- The candidate branch is a clean ancestry from the implementation commit to the evidence tip. The evidence commit adds only `VALIDATION.md`, a sanitized EditMode ZIP, and its package manifest; there is no implementation or test-source delta after the tested code commit.
- Core canonical remained unchanged during review.

I inspected the complete six-file implementation diff from `398bbd50` to `383af5a`, current source, the approved CORE-F runtime/restore design, applicable repository workflow instructions, and the exact-tip validation records and package.

## Reviewed scope and findings

The change routes detached P12-E and P12-F owner construction through application-owned staging methods when the restore coordinator receives a `DailyV1Application`. The application checks its established restore authority before inspecting owner inputs. Initial restore requires the application's captured bootstrap context on its owner thread and an unused session gate. Replacement requires the exact current session, its current-thread exclusive reservation, and a healthy completed boundary. Both paths require detached C roots. P12-G passes the same application and replacement reservation through C, D, E, F, and candidate composition.

Core creates the selected P12-E institution/office, property, faction, claim, support, decision, and justice owners. It creates P12-F political knowledge, directives, actor choices, travel parties, and expeditions. Candidate values remain local until the staging method returns a complete owner-stage object. A rejection returns no staged result and does not publish or mutate the active session.

The lower-level no-application path remains available for existing coordinator tests and uses the same Core E/F stagers. It is explicitly a detached staging seam, not application publication. The candidate does not claim complete application publication, `TryStart`, the fixed .NET host, or separate-process T/T+1/T+2 continuation.

Stage order and observer mapping are preserved: E stages institution/office, property, faction, claim, support, political decision, and justice; F stages political knowledge, directives, actor choices, travel parties, and expeditions. Each Core stage callback maps to its existing P12-G stage identity. No test assertion or production behavior was weakened to obtain a pass.

### Identity and owner binding

The P12-F application method constructs its identity witness list from the detached F rows. In isolation, that would not prove correspondence to a staged D roster. The actual production route supplies a P12-D package first, and that package enforces the missing relationship before the F method is reachable:

1. `P12DNpcRootOwnerSnapshot.TryStageForImage` requires D/F row counts to match and each indexed D/F pair to have the same non-empty, unique RuntimeId.
2. With the admitted selected definitions, P12-D requires one staged selected Core owner for every D row, hydrates those owners by the row RuntimeId, and collects one selected-owner object from each staged NPC runtime in roster order.
3. The P12-F caller checks D NPCs for unique valid IDs and requires the D NPC count to equal the F row count. P12-F travel-party staging binds every selected owner RuntimeId and travel-party value to an F row. Expedition staging rejects duplicate row IDs, requires every selected owner identity to be in the row identity set, and validates expedition members and travel-party row links against those rows.

Because the selected P12-D owner collection is complete and the staged D/F roster counts and row identities are exact, this production route prevents a mismatched or expanded F-row roster from being accepted. I found no actionable identity or owner-binding defect in the reviewed path.

## Tests and validation evidence

All evidence is bound to the six exact changed-file hashes in the candidate's validation record. Those hashes match the reviewed candidate. The private CoreValidation input manifest and the Unity input manifests also contain matching hashes for the changed source files that each runner consumes. Unity focused, full EditMode, and Smoke manifests report the same stable 1,956-file inventory digest; CoreValidation reports a stable 504-input digest.

- Official CoreValidation run `b46749ada78d4f288a7988ade151fa9c`: cold and warm **896/896 PASS**. I verified the original cold and warm TRX byte lengths and SHA-256 values against their private run manifest and parsed each report as 896 total, 896 passed, zero failed. The private run-manifest SHA-256 is `9d742feacefd67ffa7f79b7adb4b877ebd8357e3f6ac0c26b54e861a06b6658d`. The TRX files remain private; this candidate does not claim a public .NET TRX package.
- Focused Unity `P12GPoliticalBindingRestoreTests`, run `93d61fcc02bc4bf3837392812f7bd06f`: **15/15 PASS**, zero failures/skips/inconclusives; private run-manifest SHA-256 `3b7a6026b89e2f1a24c69b3b913fce960a3330ff1db2fbe294ee2f8f3cbadc53`.
- Full Unity EditMode, run `2acb6e8542794a63882eae08f55b214d`: **3274/3274 PASS**, zero failures/skips/inconclusives; private run-manifest SHA-256 `9766c45c2554a7f04af7fe295b5c181721814a807270786b8d0fa611b4984113`.
- Official Unity Smoke, run `adb05ae66445473eb821599b561e890e`: **5/5 PASS**, zero failures/skips/inconclusives; private run-manifest SHA-256 `d8ab4f667d5daff8be422fe542a7cd1b9ae3c1e30a2fae87f1799dd121864b7c`.
- The three Unity original XML files match their private manifest sizes and hashes. I compared the 15 focused and five Smoke NUnit case IDs to the 3,274 full EditMode IDs; both sets are exact subsets, and all IDs are present in the sanitized package XML.
- I reran the official Unity package verifier against the committed manifest, ZIP, and the exact full EditMode original run manifest: `VALID`, one run, one entry. The ZIP SHA-256 is `fcb14832d3af38e0a5a78e3fedafe26cff23516eb526b216612cc0013322e1ff`; the package-manifest SHA-256 is `a0863da5dadffe01caa6729bad9aff0a1c4d4ba5f0cdfe2c5794bb6d3e67f0a4`.
- Independent privacy scan of the public ZIP contents and package manifest found no absolute local drive paths or credential-like patterns.
- `git diff --check` passes for the implementation diff.

The focused test's initial run, before the lower-level no-application path was retained, had four failing legacy coordinator cases. That private diagnostic was preserved. The reviewed code keeps the lower-level Core-stager path and adds an integrated application-authorized image restore; the final focused and full regression evidence above passes. No LongRun gate was applicable: this change stages detached owner values and does not alter daily execution or causal behavior.

## Review outcome and limits

No actionable implementation, exact-tip, test-integrity, identity, stage-order, or artifact-integrity finding remains for this bounded checkpoint. The exact-tip review supports `VALIDATED_CANDIDATE` only.

P12-E/F application-authorized staging is not full restore publication. CORE-F remains incomplete and unpromoted. The candidate does not establish `DailyV1Application.TryStart`, Core-owned Unity runtime forwarding, a fixed executable .NET host, or T/T+1/T+2 process-detached continuation. Canonical promotion and the applicability of this candidate to any later canonical tip remain separate workflow checks.
