# P12-G evidence-packager local-file URI revalidation

Date: 2026-10-12

## Candidate identity

- P12 canonical base: `eef376d51f36641918a13b2e937e704051814820`
- Candidate branch: `codex/phase12/P12GPoliticalBindingCompletion`
- Revalidation code commit: `32341e71510e5c9d4b1c6eb9b878bb1eb286e359`
- Candidate Git tree: `27bd29e9a0fbe5f331367f0d8be13fab57d5606e`
- Unity `Assets` tree: `a61dad9e1755c28c87fc3f7e01e6e30bb1f9964f` (unchanged from the previously validated P12-G candidate)
- Evidence module blob: `2e2bde5f3b51f465dca65bc2513e7bf2a8bd6c22`
- Packager test blob: `3cf4d911e39e3b75589f96e4f7f6d75814c3d0d5`

## Bounded correction

Independent review found that the prior scanner accepted a localhost file-scheme
URI value in public text and did not classify that Assembly `fullname` as an
absolute path. The module now rejects file URI forms in public evidence and
recognizes them as absolute Assembly paths; because the existing redaction
policy permits only its declared Windows Unity editor assembly path shape,
file-URI Assembly identities fail closed. The existing Windows path projection
and assembly-relative identity behavior are unchanged.

No package format, Unity source/test file, result XML, source identity, assertion,
result count, or prior validation artifact was changed.

## Revalidation results

- `Package-UnityValidationEvidenceTests.ps1`: **64 assertions passed**, including
  direct scanner rejection of a localhost file URI, Assembly path-policy
  rejection of a file-URI identity, and end-to-end package rejection.
- `Invoke-UnityValidationTests.ps1`: **33/33 passed**.
- `git diff --check`: **PASS**.
- Independent exact-tip code review: **PASS** for the bounded URI/path-policy
  delta, recorded on `codex/review/phase12/P12GFileUriPathPolicy32341e` at
  `0a41988fec13b0d036c4e286c8ce2f83f5cd7d89`, based on P12 canonical
  `eef376d51f36641918a13b2e937e704051814820`. The record does not review the
  full P12-G candidate and does not clear its separate validation-record
  identity-label blocker or authorize publication/promotion.

Unity was not rerun: the code and test trees under `Assets` are unchanged, and
this revalidation only changes the packaging module and its own self-tests.
The previously recorded exact-tree Unity reports remain evidence for their
original Assets tree only.

## Evidence boundary

This closes the identified local-file URI scanner gap for the tested forms. It
does not alter or replace the prior validation record, claim that every URI or
host-specific path representation has been tested, or establish P12-G
completion, P12-A readiness, P13 readiness, or Phase 12 closure.
