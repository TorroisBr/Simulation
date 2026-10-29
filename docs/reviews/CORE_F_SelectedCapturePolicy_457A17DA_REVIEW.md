# CORE-F Selected Capture Policy — Independent Review

**Verdict: `VALIDATED_CANDIDATE`**
**Review scope:** exact committed candidate diff only; read-only review.
**Review date:** 2026-10-29

## Exact Git identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `457a17da8a7d1b0df0a852af9a65fc8062204244`
- Candidate tree: `93d1332c1f5793517b838eebf80fb229d13e5259`
- Candidate base: `b0872eddd7bbcf89d702e80434f066eb6ca948a5`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The candidate is a descendant of both the stated base and current canonical. The remote candidate ref matched the exact candidate commit at review time; Core canonical remained unchanged.

## Review result

The four-file change moves selected Daily-v1 capture eligibility decisions into the Core boundary while retaining the same live owner census protocol in the Unity host adapter. Core validates the admitted profile, owner thread, advance lease and allowed operation scope, world publication, mutation guard, and host P12 operation context before requesting the census. It rejects and clears incomplete or failed evidence, then rechecks scope and host operation context after the census to discard evidence if a reentrant operation appeared.

The Unity adapter reports only host-owned facts (factual-world publication and active Unity daily-operation context) and reads the existing `ContinuationCensusProtocol`; it does not introduce another owner vector or mutation authority. The existing protocol remains responsible for exact owner coverage, identity, revision, mutation-epoch stability, ordering, and dynamic-family checks. No P12 profile or persistence semantics are expanded.

The prior review finding is resolved: off-owner-thread capture returns `WrongOwnerThread`, clears outputs, does not request the host census, and does not fault runtime admission. The regression `CoreRejectsOffThreadCaptureWithoutFaultingRuntimeAdmission` verifies those outcomes.

The complete candidate diff was reviewed against its exact base. No actionable implementation defect or scope issue remains.

## Validation evidence

All runs below identify candidate commit `457a17da8a7d1b0df0a852af9a65fc8062204244` and tree `93d1332c1f5793517b838eebf80fb229d13e5259`; manifests report stable inputs. Input SHA-256 entries for changed source files were checked against the candidate bytes.

| Gate | Run ID | Result | Evidence SHA-256 |
|---|---|---:|---|
| CoreValidation cold and warm | `83e982f16c8546b192bebd4fade64366` | 918/918 cold; 918/918 warm | Manifest `4e960bafb37c5eca06a331bb60f0e2066be37b974801c53d4acf7a77fc0eb70b`; cold TRX `b336c90dd258f230c5dbfd943ce3be8cf533e1287538c503e2d04a2d3f592272`; warm TRX `515a4c215485885f068d109efec9d7a5198fb7fee4e7dbdd8446b985fd6e5b1a` |
| Unity P12 admission/capture | `7d0c5fdde1184cad8f0dfba677a62a71` | 228/228 | Manifest `de84846666ca2c2b525bab016a172b89e80b1110d045de99eb6e897ca4986efa`; XML `15a6c30d1b90dd12f03a4655f358cee545e8206eda8281b672f0f6e438f76e47`; log `0d3737f421f791af6d8e15ba414e22381de0d60a3bf377ab07e8d1210c7ee785` |
| Full Unity EditMode | `0e943d627606451585734a9dfe060a0f` | 3277/3277 | Manifest `64a6d56f139440c77d72b0718e8bb7118ab58c743c43b7ad42a0bdb799c63518`; XML `e6c4827a3ce38c379e37d8c93ae3200a51ebdacf2c742ddd72e7e5fc2226ff80`; log `a7c41bebcf193283ed7e7fdc8f2c56a4013cb4a57c2b3c75f907e80e81d8ac2f` |
| Official Smoke | `3a9e6e342b3e4848a00baf3d32bd4779` | 5/5 | Manifest `43c311fb3d2b73f114e1aad303a1bb6446a2a3f93b4cddda382d0db734961fa6`; XML `cb121174eaeac560aa2926ada61c8f845360eac60c75101abf8950c514f1a1f2`; log `61f9789ae224dfd5450b5d46af2afe010aba4183ab32bb3316b14451c3ca442a` |

`git diff --check` passed. The review did not rerun validation; the recorded exact-tip evidence was verified.

## Nonblocking note

`CoreOwnsSelectedCapturePreconditionsAndMapsCensusFailures` supplies a `DailyCaptureEligibilityFailure` already mapped by its fake host, so the test exercises failure propagation rather than the `ContinuationCensusFailure` enum mapping. That mapping remains unchanged in the Unity adapter; this naming/coverage distinction is nonblocking for the reviewed change.

## Scope boundary

`VALIDATED_CANDIDATE` records review of this bounded integration only. It does not mean canonical promotion, CORE-F completion, formal closure, detached .NET host readiness, or completion of the shared application/restore contract. Canonical promotion remains a separate workflow.
