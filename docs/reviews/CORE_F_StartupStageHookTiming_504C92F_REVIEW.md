# CORE-F Daily-v1 startup stage-hook timing review

## Exact candidate

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate tip: `504c92f20fc8f39230b7f12d63ec57b4ea379a2d`
- Candidate tree: `619380ff2ea265ea52edee1b7aafea2c306b746e`
- Implementation commit: `1b0ac0a2189122895eef4e31149efe12886e0ff0`
- Implementation tree: `065d65019cc60b3b65a38036e7445226b262021a`
- Prior candidate: `be3645f69d14609e690671c6ec512fab19cba11a`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Verdict

Independent exact-tip review: **PASS — VALIDATED CANDIDATE** for the bounded startup stage-hook correction. No actionable findings remain.

The review confirmed that the prepared no-save start retains the exact application admission, effective definitions, logger, and private Core session; Core genesis runs within the ordered bootstrap stage sequence; validation/publication callbacks retain their established Unity timing; and an admitted restore remains fail-closed without falling through to genesis. The strengthened regression verifies stage ordering and that the prepared Core session remains unpublished during Core stage callbacks.

The independent reviewer also opened the protected CoreValidation run manifest and cold/warm TRX for run `8e0caf35c8e74e3998b9587d54c6a78b`. Their SHA-256 values match the validation record. Both reports record 1,014/1,014 passed, zero failures/errors/unexecuted tests; the stable input digest is `2eca8e3b2c94386a362506ed27f39c4284f78bce20497ea17579d1f73d908872`. The recorded application source hash matches the exact committed source. The Unity manifest source hashes match the committed source and test files.

## Validation

The exact source hashes and required gates are listed in [the validation record](../validation/CORE_F_StartupStageHookTiming_20261105/VALIDATION.md). CoreValidation, focused Unity EditMode, full Unity EditMode, official Smoke, and `git diff --check` passed. The checkout contained a Unity `Library`; the evidence does not claim Library-independent .NET execution.

The protected original CoreValidation manifest/TRX and local Unity focused/Smoke originals remain protected and unchanged. The independent reviewer verified the CoreValidation manifest and TRX hashes but did not copy private evidence into the repository.

## Limits

This is an unpromoted candidate checkpoint. It does not publish the public `DailyV1Application.TryStart`, complete detached restoration, establish the independent .NET host, or close CORE-F. It does not change Core canonical.
