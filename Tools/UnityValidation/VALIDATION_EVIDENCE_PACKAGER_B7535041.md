# TRX evidence packager validation — b7535041

Date: 2026-10-14

## Candidate and promotion

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/tools/trx-evidence-v1-private-context-fix`.
- Reviewed implementation commit: `b7535041a8aeb6e341fe35c6fcc465da48ce313e`.
- Reviewed implementation tree: `67b595da4c1337f4da29116cb4d3e791ada54d91`.
- Candidate parent: `b9c73dd61f09aa95476f39df3784b77ce45c4e31`.
- Prior `codex/core/canonical` tip: `ba65e5ad75d8a604d559f3bffb90aa3fb42a4fef`.
- Review record: `ba7c5c463a4754fe1f2b174b47fb6d79cdb30df5`, on
  `codex/tools/trx-evidence-v1-review-b7535041`.
- Core promotion fast-forwarded through that review record. The promoted code
  and exact review record are preserved in `codex/core/canonical`.

The candidate delta from its previous reviewed tip changes only
`TrxValidationEvidence.psm1` and `TrxValidationEvidenceTests.ps1`. It enforces
the contractual cold-then-warm positional order and rejects reversed attempts
even when their exit codes and durations match.

## Exact-tip validation and independent review

- TRX evidence self-tests: 144 assertions passed.
- Shared evidence packager tests: 64 assertions passed.
- Unity validation runner self-tests: 33 passed, 0 failed.
- `git diff --check`: passed.
- Independent exact-tip review: PASS; see
  `INDEPENDENT_REVIEW_TRX_EVIDENCE_V1_B7535041.md`.

No Unity EditMode/Smoke or .NET domain suite was rerun. This candidate changes
only evidence packaging code and its tests; domain source, test source,
execution inputs, fixtures, and runner behavior are unchanged.

## CORE-A package trial boundary

A package trial was generated with this implementation before canonical
promotion. Its archive was 13,867 bytes with SHA-256
`540026a1fe878a927bd2ba70c51dc5e03b87526f43f474467f60eb68cb5a43ed`.
Exact-context verification reported `originalComparisonVerified`; independent
archive-only verification reported `originalComparisonNotPerformed`. The
archive contains two ordered normalized result reports and passed the privacy
scan. The package trial is not the final CORE-A evidence package because it
predates tool promotion.

The final CORE-A evidence gate remains open until the retained cold/warm
evidence is packaged using the promoted tool, the private original comparison
is recorded, and the current CORE-A candidate receives exact-tip applicability
review. This tool promotion does not promote CORE-A or imply P12-A readiness.
