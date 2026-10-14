# CORE-A Calendar Shared Assembly — Exact-Tip Independent Review

**Implementation review: PASS.**
**Canonical promotion: BLOCKED on the durable-evidence gate below.**

## Review identity and refs

- Reviewer: independent reviewer assigned separately from the implementation author.
- Candidate branch: `codex/core/CORE-A-calendar`.
- Exact candidate: `2062e616d5a6bc53a2f154f33e9f4887a44972fe`.
- Candidate tree: `e48d68151e6f8fbe9461fba6e345014d9d0b5370`.
- Production base: P12 canonical `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
- Architecture/handoff: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
- Code remediation: `f725fa87c6d1259d822180b84856b747c9e02980`, tree `bdb6b9b63532e780661477430099a835d2b4f086`.
- Latest candidate ref was verified directly on origin before review. Candidate files were not modified.

## Code and scope findings

The calendar extraction remains within the approved CORE-A handoff. The calendar implementation and Unity tests are unchanged from the previously reviewed implementation. The only source changes since candidate `78011db2cbbe36981e30f9a58b2f14a3756d6e5e` are in `Tools/CoreValidation`; the later commit adds validation documentation. The Core package/build boundary and shared-source arrangement remain unchanged.

The prior TRX-reader finding is corrected. `Get-CoreTrxAssessment` now rejects a missing `TestRun/@id`, requires the supported `ResultSummary/@outcome` (`Completed`), retains the run ID/name/summary outcome, and continues to reconcile test cases and counters. The runner records those three fields for both cold and warm test gates. Synthetic self-tests now cover missing run identity, missing summary outcome, a non-completed summary, and identity/outcome retention. I found no further implementation issue in this delta.

`git diff --check` passes for the complete base-to-candidate diff. No tests were rerun for this review; the recorded run below was inspected.

## Validation evidence and applicability

The protected run manifest identifies .NET run `1aa63d7e04854212be037b09d299fccd`, source commit `f725fa87c6d1259d822180b84856b747c9e02980`, source tree `bdb6b9b63532e780661477430099a835d2b4f086`, and stable 27-file input digest `174a3de69f00b1cb370f6b75749b14f5da72a3c8c911d675bc921270e5cbffee`. It records 45/45 passing cold and warm tests, zero failures/skips/inconclusive/not-executed cases, and `Completed` summaries. Both raw TRX root IDs and summary outcomes match the corresponding private manifest gate records.

The private evidence archive hash is `e8a6814929a57a7b04a77fa81e2cd2acc8b12efe1aa5a44818fddde49209d1de`; its cold and warm TRX entries match the separately retained original report bytes and recorded hashes. The pushed candidate tree contains neither raw .NET TRX file nor this private archive.

The Unity evidence remains applicable: the Unity manifest contains no `Tools/CoreValidation` input, and no Unity `Assets/` or `Packages/` file changed after the calendar implementation candidate. The new parser fix therefore does not invalidate the recorded Unity package, P12 compatibility, EditMode, or Smoke results.

## Privacy review and evidence gate

I inspected the original cold and warm reports only to verify their privacy properties. Both include the local Windows account identifier in run metadata and absolute repository checkout references. This review record intentionally omits the identifier and all exact checkout paths. I found no email-like values in either report.

The validation record states 91 absolute checkout path markers. This scan counted 92 occurrences across the two reports (90 in `TestMethod/@codeBase` and 2 in `StdOut`). The one-count difference does not change the privacy conclusion; clarify the counting convention if that number is intended to describe raw occurrences.

The report bundle is preserved in protected local evidence and is excluded from the pushed candidate because its metadata is private. The archive and manifest hashes establish local integrity, but do not provide durable, reviewer-accessible raw evidence in the candidate or another policy-approved shared location. Under the handoff's original-report and privacy requirements, the evidence gate remains blocked until the complete bundle can be retained for required review without exposing that metadata. Do not publish the raw bundle or copy its account/path values into Git.

## Disposition

The exact-tip implementation review passes. The parser correction, .NET rerun, and Unity evidence applicability are supported by the reviewed artifacts. Canonical promotion remains blocked solely by the unresolved durable raw-report evidence/privacy gate. No Phase canonical or CORE canonical was changed by this review.
