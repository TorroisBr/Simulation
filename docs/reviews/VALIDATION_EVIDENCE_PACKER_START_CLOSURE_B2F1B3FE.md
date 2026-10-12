# Validation evidence wrapper start-closure review

Date: 2026-10-12

- Reviewed base: `eef376d51f36641918a13b2e937e704051814820`
- Reviewed code candidate: `b2f1b3fe326d83b651e8daa31225014f8f9519ee`
- Reviewed Git tree: `69b4e41d2e7ae42e3965410b22194cdfa69bf479`
- Exact-tip verdict: **PASS / VALIDATED_CANDIDATE**
- Independent review: `/root/p12g_candidates_audit`

The candidate changes only `Tools/UnityValidation/UnityValidation.psm1`. It captures the module-resolved `FunctionInfo` for `Start-UnityValidationProcess` before creating the closed start action, then invokes that command object with the original command argument. The process starter, working directory, hidden-window behavior, timeout, retry, ownership checks, and cleanup contract are unchanged. Packaging, report interpretation, privacy, and historical evidence are unchanged.

Validation tied to the reviewed code tree:

- `Invoke-UnityValidationTests.ps1`: 32/32 passed.
- `Package-UnityValidationEvidenceTests.ps1`: 41 assertions passed.
- Harmless module process-launch smoke reached normal `NoResultXml` classification without a closure-resolution error.
- `git diff --check`: passed.

Coverage limitation retained: the two self-test suites do not exercise a successful start through the nested evidence wrapper; the process-launch smoke covers the start closure with a short-lived shell executable. The integrated P12 candidate validation will exercise the actual Unity runner through the evidence wrapper.
