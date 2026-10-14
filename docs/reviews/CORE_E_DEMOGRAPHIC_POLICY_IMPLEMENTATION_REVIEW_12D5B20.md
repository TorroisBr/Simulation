# CORE-E demographic policy — independent implementation/evidence review

**Date:** 2026-10-14. **Verdict:** PASS.
**Reviewed candidate:** `codex/core/CORE-E-demographic-policy` at
`12d5b203eec15dd70909e0e16093b77d92650622` (tree
`ac50869d65b35228c7c8b927fc56e466f9a9a7e4`).
**Exact base / current Core canonical:** `6950f596cab9f96921290e24a9aeb091229dcc6d`
(tree `fe5ec35243530dc132758ff2ee9198cf2c982ab5`).
**Validated implementation source:** `2dcd4933f214d797d4e064b6cea26cdbe5bd5cba`
(tree `17543d1f9f318a77b5e6155f64c62d769e6b6557`).
**Approved handoff:** architecture canonical `7166ed088483c69d37300b4f12528ebb4b27be84`,
including its exact CORE-E demographic-policy boundary and validation requirements.
This is the full base-to-candidate review; no candidate or protected evidence files
were modified.

## Implementation review

- The diff makes the exact approved one-copy extraction: `PersonId` and its `.meta`
  move unchanged to the Core package with GUID
  `7b6e0a8b2d5d4de8a1a4c7e9f3b2d601`; `AggregateDemographyChange`,
  `AggregateDemographyContext`, and `IAggregateDemographyProvider` move to one new
  package source; the sample interfaces, enum and complete deterministic sampler
  move to one package source; and the deterministic aggregate policy moves to one
  package source. No duplicate rule/facade remains in Unity.
- The retained Unity contract file contains `AggregateDemographyFailure` and
  `IAggregateDemographyRandomSource`; `DailyDemographicSystem.cs` retains the
  represented-resident floor, diagnostics, report and coordinator. No owner,
  transition, PopulationChangeSet/receipt, PersonStore/lifecycle, configuration,
  genesis, runtime, P12 census/capture/codec/restore or admission code changes.
- The only behavior-facing accessibility change is the existing
  `AggregateDemographyContext` constructor becoming public with the same
  signature body, defaults and value semantics, exactly as the handoff requires
  for the retained validated Assembly-CSharp caller. The policy still has no
  mutation authority and constructor validation/normalization was not added.
- `Simulation.Core.csproj` adds exactly four production links for moved
  production sources. `Simulation.Core.Tests.csproj` adds exactly one link for
  the shared characterization source. Existing Unity assembly definitions and
  framework/package settings are untouched. The CoreValidation source inventory
  includes the linked source files, their package folder/source metadata and the
  shared test source/meta.
- Shared NUnit characterization covers recorded production sample bits, UTF-16
  including surrogate pairs, null/blank and boundary days, culture/call-order
  behavior, outcome fallback, aggregate rates/strict threshold/saturation/death
  cap/validation and exception ordering, context/change value semantics, and
  PersonId construction/equality/assembly identity. Its reported pre/post move
  comparison preserves exact sample vectors. This extraction does not replace
  the original Unity integration/owner/P12 suites or claim population-owner
  portability.
- The candidate remains within the approved demographic policy/sample/PersonId
  closure. It does not add natural-mortality queries, Person or settlement
  owners, gameplay, a new random stream, save state, P12 profile membership,
  P14/P18/P20 behavior, a public extension API or a Daily-v1 host.

## Unity validation reconciliation

The implementation validation record reports focused demographic/person/P12
regressions plus ALL EditMode 3,132/3,132, Smoke 5/5 and unchanged 100-, 1,000-
and 5,000-day LongRun cases 7/7. I checked all 15 referenced protected Unity
run-manifest hashes against the record: all hashes match, every run status is
Passed, each input snapshot is stable, and the final attempt totals match the
recorded passed counts with zero failures, skips or inconclusive cases. The
LongRun source was not changed in this candidate.

## .NET package verification

I independently invoked the delivered `DotNetTrx` Verify command using the
private source context for run `94a4b82c74494b8fa3f94f7094fa55be`. The verifier
returned `VALID` with `originalComparisonVerified`.

- Package source commit/tree: `2dcd4933f214d797d4e064b6cea26cdbe5bd5cba` /
  `17543d1f9f318a77b5e6155f64c62d769e6b6557`, matching the requested validated
  identity.
- Package and private-run input digests both equal
  `a011b76010ca6e330126fa65619c3c75ff0abf47958dd6575a7f06d3e59b9d92`;
  `inputStable` is true.
- Cold and warm reports each contain 125/125 passed results and zero
  non-passing results. The archive contains exactly those two normalized TRX
  entries.
- Archive SHA-256 is
  `4b74838a29be7fbe63eb83aaff2dc8e015e54931c6011a4cd09dae89d827628f`
  (55,255 bytes). Manifest SHA-256 is
  `72bdcb177ca05af1caa5f51a7e9dfe2e928e7fc51842ee668dd3a17f051f91e1`
  (53,352 bytes). Both actual hashes match the committed validation record and
  manifest declaration.
- The decompressed entries contain no absolute drive/UNC, user-home, or
  `AppData` paths. The verification context's private location is intentionally
  omitted from this record.
- Comparing the validated source commit to the final candidate shows only three
  additions: this detailed validation record, the package manifest and the
  package archive. The implementation and validation-input source trees are
  unchanged after the packaged code tree.

`git diff --check` passed for the full base-to-candidate diff. No actionable
findings remain. This PASS approves the reviewed candidate content for bounded
CORE-E promotion consideration; it does not itself perform promotion or change
the canonical branch.
