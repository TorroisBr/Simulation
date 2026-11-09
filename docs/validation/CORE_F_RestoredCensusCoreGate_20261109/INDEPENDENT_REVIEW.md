# Independent exact-tip review — CORE-F restored census eligibility gate

**Verdict:** `VALIDATED_CANDIDATE` / PASS.

## Identity reviewed

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Exact candidate: `3ac1912a06e2924171e48b3c2c471633ac0549c5`.
- Exact base: `0926a0423db7cbe47ff3e2d3226827e667ff468c`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Code/test source commit: `5dcba571123ceea3ce42eee48e6eaa845947861c`.
- Code/test source tree: `680daae97ba07f4c88f7acb45a39cf01286c397f`.

The review was performed independently of implementation and validation authoring. The reviewer did not edit the candidate.

## Findings

The Core eligibility gate rejects an admitted but non-restored bootstrap session before the host census callback. The review-driven test correction constructs a real `GenesisPrepared` session, confirms selected Daily-v1 admission and non-restored context, and verifies the callback remains uncalled. Core repeats eligibility and mutation-epoch checks after capture.

The exact-source CoreValidation manifest binds the reviewed code/test commit and stable input digest. Cold and warm results are 1,072/1,072. Unity full EditMode is 3,302/3,302 and the filtered EditMode `Smoke` suite is 5/5; the full package verifies against the private source manifest and includes the same five Smoke identities. Evidence hashes match `VALIDATION.md`. The native CoreValidation manifest remains private; no unsupported public TRX adapter was claimed.

The candidate diff is limited to the restored-census eligibility implementation, its corrected Core regression test, and the validation record/package. Protected ProjectSettings changes and unrelated local evidence remain outside the candidate.

**Actionable findings:** none.

## Limits

This candidate transfers the private restored-target eligibility gate only. It does not complete the 89-family census protocol, full P12 restoration lifecycle, detached .NET host, CORE-F, P12 readiness or Core canonical promotion.