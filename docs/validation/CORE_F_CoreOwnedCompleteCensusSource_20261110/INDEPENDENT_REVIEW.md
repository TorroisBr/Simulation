# CORE-F Core-owned census composition — independent review

Review verdict: PASS for the implementation at the exact candidate commit below. This review record does not promote the candidate or close CORE-F.

## Exact identity

- Candidate implementation commit: 2aae45686431b4a6832661498da3e8815a8c22a8
- Parent: 8781d650e830648440ebfbf9a29e2e99506497c1
- Core canonical during review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Reviewed changes: the four implementation/test files and the validation record/package in the candidate commit.

## Findings

The host census port returns the complete quiescent source vector and epoch unchanged. Core validates the epoch, rebinds selected NPC runtime-state census rows to the exact admitted owner graph, checks row identity, role, schema, cardinality, owner-instance identity, and revision, and composes the complete P12 vector. Missing, duplicate, stale, or mismatched selected rows reject. Mixed-roster rows remain on the compatibility path.

The strengthened Unity regression verifies that the host source vector contains the same complete section identities and row facts as the completed P12 token, including selected CurrentAction rows, owner instances, and revisions. No actionable implementation defect was found. The candidate commit contains no ProjectSettings, protected evidence, or unrelated user changes.

## Evidence review

- The verified full EditMode evidence package reports 3,305/3,305 with zero failures, skips, or inconclusive cases. The reviewer verified the package manifest/archive identity and matched all four changed source/test file hashes to the stable Unity input snapshot.
- The filtered Smoke run reports 5/5, and its five identities also pass in the verified full EditMode report. The strict official packager rejects a separate filtered Smoke package because the parent Simulation suite count is inconsistent; the original was preserved and no alternate packaging was used.
- The official CoreValidation run is recorded as 1,089 cold and 1,089 warm, with stable inputs. The original private manifest/TRX was not available to the independent reviewer for direct reopening, so those results are not claimed as independently reverified here. No public Core TRX package is claimed.
- No Library-independent .NET-host claim is made; the runner observed a Unity Library in the checkout.

This is an implementation checkpoint only. Core canonical remains unchanged, and no CORE-F completion or canonical promotion is claimed.
