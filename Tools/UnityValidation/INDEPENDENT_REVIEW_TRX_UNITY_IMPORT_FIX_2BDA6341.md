# Independent review: TRX/Unity verifier import-order correction

Date: 2026-10-14

## Exact target and authority

- Candidate branch: `codex/core/TRXUnityVerifierImportFix`
- Candidate commit: `2bda6341f3c5e599064e10ccda569237b9aea445`
- Candidate tree: `f1d69e18dcf52e4f686116575fceb1729f53b59b`
- Candidate base and current canonical: `c68ca7a9a51df178ba0b889096c762c3fd7f4034`
- Reviewer: independent Codex subagent; candidate implementation was not edited.

## Verdict

**VALIDATED_CANDIDATE — PASS for the exact-tip CLI correction.** The import order restores the Unity/NUnit verifier to the shared CLI while retaining TRX dispatch and verification. No further findings remain in this bounded change.

## Review

Before this change, the entry script imported `UnityValidationEvidence.psm1`, then imported `TrxValidationEvidence.psm1 -Force`. The TRX module itself force-imports the Unity module. That nested forced re-import can remove the caller’s previously imported Unity verifier commands from the entry script’s command scope; the later Unity `Verify` dispatch then fails to resolve `Test-UnityValidationEvidencePackage`.

The candidate imports the TRX adapter first and the Unity adapter last (`Package-UnityValidationEvidence.ps1:15-16`). The last top-level import restores the Unity adapter’s exported verifier in the entry script. The Unity module does not replace the TRX adapter’s format selector or TRX verifier, and the archive-only TRX CLI check below confirms those remain callable after the reorder.

The added Unity regression uses a child `pwsh` process to invoke the real `Package-UnityValidationEvidence.ps1 -Action Verify` entry point against a generated Unity/NUnit fixture package, then asserts exit code zero and `VALID:` output (`Package-UnityValidationEvidenceTests.ps1:25-38,173-174`). It exercises the actual module import sequence and CLI dispatch, rather than calling the verifier function directly.

## Validation

Fresh focused validation ran on the exact candidate:

- TRX evidence suite: 144 assertions passed.
- Unity evidence packager suite: 65 assertions passed, including the real CLI regression.
- Unity validation runner self-tests: 33 passed, 0 failed.
- `git diff --check c68ca7a9a51df178ba0b889096c762c3fd7f4034 2bda6341f3c5e599064e10ccda569237b9aea445`: passed.
- Archive-only TRX CLI Verify on the committed CORE-A package: passed with one run, two entries, and `originalComparisonNotPerformed`.
- The Unity/NUnit archive-only CLI Verify is exercised and passes in the 65-assertion suite.

The CORE-A package retains tool identity `c68ca7a9…4034`. Exact-context verification under candidate `2bda6341` is expected to reject that package’s tool identity because the verifier binds package provenance to the tool commit. The recorded `originalComparisonVerified` result was produced using the promoted `c68ca7a9` tool. These are separate results; the archive-only verification under `2bda6341` does not replace or contradict the prior private-context comparison. No .NET tests, Unity EditMode tests, or Unity Smoke tests were rerun for this import-only correction.

## Scope

The diff changes only the shared package CLI’s two adapter-import lines and adds a Unity CLI regression test. It does not change package schemas, evidence contents, or simulation behavior. This review does not promote the candidate or revalidate the protected original-context comparison under a different tool identity.
