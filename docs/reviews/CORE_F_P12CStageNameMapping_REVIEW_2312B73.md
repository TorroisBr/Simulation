# CORE-F P12-C restore-stage mapping review

## Verdict

PASS — exact-tip candidate review.

- Candidate commit: `2312b73eaa8cb80a0a4a73fd9d599a1057ff015c`
- Candidate tree: `aa260845ec432d5c31ce33af2e85e6b836b16c86`
- Parent: `14688508c331a681e47f161726e60922f57d7eb4`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Independent reviewer: separate reviewer, no implementation changes

## Finding and correction

The preceding review found that enum parsing could accept an undefined numeric stage such as `"999"`. The candidate now requires a defined `P12GDailyV1RestoreStage` and an ordinal round trip to its canonical enum name. Empty, wrong-case, unknown-text, and undefined numeric values fail and reset the output value. The added regression covers a known stage, `"999"`, wrong case, and unknown text.

The independent review found no remaining actionable issues. The reviewed diff contains only `P12CContinuationRootStager.cs` and `P12CPrivateRootCompositionTests.cs`; no API visibility or staging behavior changed.

## Validation evidence

The official Unity runner captured the exact two changed-file hashes that are present in the reviewed candidate. All cited runs report stable inputs:

- Focused P12-C composition: 55/55 PASS; run `009a445e54ed44ac8aa5e6a121143f30`.
- Filtered `GMConsolePlayModeSmokeTests`: 2/2 PASS in EditMode; run `7576231c80044e84969d7ca134ca5ded`. This is a filtered regression result, not the complete official Smoke suite.
- Full Unity EditMode: 3261/3261 PASS; run `abcf10ff4a624635b25c9aa3f3a1610f`.
- `git diff --check`: PASS.
- Full EditMode evidence archive SHA-256: `2474d0066ab3c522774bc421590e8ec411ce3d691064d096fc7357fa1697af3e`; official package verification returned valid with one run and one entry.

The Unity runs preceded commit creation: their repository HEAD fields identify parent `14688508`, while the captured source hashes match the reviewed candidate blobs. CoreValidation was not rerun because this correction changes only the Unity adapter and its test; the preceding Core stager commit's CoreValidation evidence remains separate and is not represented as evidence for this Unity-only fix.

Private original XML/log files remain in the protected local evidence store. This public record contains no workstation paths or private metadata.

## Scope boundary

This review accepts only the P12-C Unity stage-name mapping correction and its regression test. It does not complete P12-C, P12-D, the CORE-F application/restore package, the detached .NET host, or CORE-F; it is not Core canonical promotion. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
