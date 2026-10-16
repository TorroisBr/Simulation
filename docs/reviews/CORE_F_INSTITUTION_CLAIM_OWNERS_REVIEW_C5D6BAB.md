# CORE-F Institution and Claim Owners — Independent Exact-tip Review

Verdict: **VALIDATED_CANDIDATE**.

Reviewed candidate: `c5d6babf15480b9ac44174ce43d11e1a2eabc397`.
Production base and current Core canonical at review: `b2c2c0e94c7a8dd9175a8db3fab4fb9d78dcfb2a`.
Approved architecture: `797eb02f06fc5ca759a7e47283589d97a5efe73d`.
Reviewed implementation/test tip: `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`, tree `e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`.
Evidence/documentation tip: `c5d6babf15480b9ac44174ce43d11e1a2eabc397`, tree `90e397944c70b94334e770ef88955ea6a51e13df`.

The independent review found that the prior findings are resolved. The Daily-v1 restore regression now exercises capture, encode/decode, `TryCreateRestoredSessionFromImage`, target owner-vector recapture, owner parity and continuation across two later boundaries. It checks the same-day closed office tenure and political-claim revision/history. The unapproved `CreateTenureRecord` bridge API was removed; the approved public constructor is used. No other implementation drift from the authoritative CORE-F handoff/design was found.

Evidence verification passed. The Unity EditMode package matches its manifest and binds to the reviewed code tree; run `a049ae0481be45489d9712e5aa4a24cc` reports 3179/3179 passed. The context-bound .NET package matches its manifest and source context; run `5d026bdb1d64473fb3d8b004ec8f3151` reports 188/188 cold and 188/188 warm. Package scans found no local absolute paths or credential-like strings.

The separate official filtered Smoke run `7700e3bd41154ec58370c33fdf06945b` passed 5/5. The strict packager rejects its filtered XML because the parent `Simulation` suite count reflects full discovery, so it remains privately retained and is not claimed as a separate public package. All five exact Smoke identities and passing outcomes occur once in the packaged full EditMode report. This evidence boundary matches the documented CORE-B/CORE-C and Core State precedent; no filtered XML was modified or reinterpreted.

Final focused Unity coverage is 11/11 for `P12GPoliticalBindingRestoreTests`; full EditMode is 3179/3179; official Smoke is 5/5; .NET CoreValidation is 188/188 cold and warm; `git diff --check` passed. The full validation record and package identities are in `docs/validation/CORE_F_InstitutionClaimOwners/VALIDATION.md`.

The candidate remains a clean fast-forward from the reviewed current Core canonical, and the evidence-only tip does not change the reviewed implementation tree. No candidate files were edited during review. The three pre-existing local ProjectSettings edits remain outside the candidate and untouched.

Scope is limited to the approved CORE-F institution/claim/property owner extraction and the documented restored-clone revision correction. This does not close CORE-F or authorize downstream shared runtime composition, detached hosting, or continuation work.
