# Independent exact-tip implementation review

## Verdict

**PASS for implementation correctness and test integrity.** No actionable
findings. This review does not make the candidate eligible for canonical
promotion because the unfiltered EditMode run has no result XML.

## Reviewed identity and scope

- Candidate: `e60492e3caf7e7eeac4404e7a451a945e5db2e11`
- Candidate tree: `d52901955188b3b61a97785683f87fa958c870ef`
- Base: `f6f09fd44ad09944a03e04ba6a65885fe41969b9`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Changed code/test files: `Assets/_Project/Scripts/SimulationRuntime.cs`,
  `Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs`

The review confirmed the new route is limited to unkeyed deaths in the admitted
`UnityBootstrapDailyV1` profile when a selected owner graph exists and no live
unselected runtime member is present. Mixed-roster execution falls through to
the existing P12 path. The unadmitted keyed-receipt path and admitted
conflict-injury rejection are unchanged.

The existing Unity proposal validation checks the captured day, exact Person
registration, NPC materialization, residence, and population revision/value.
Core then verifies exact graph/PersonStore owner identity and that the prepared
Core plan matches the captured transition before P12 admission. The Core
transaction derives the required Person-life, optional NPC life/action, and
resident City-population sections. The host publishes Unity projection and
advances the reserved mutation epoch only through the existing boundary.

The updated test exercises the public proposal/apply API and checks the captured
snapshot, resulting owner revisions/census, one epoch advance, and Unity
projection. No assertion was weakened. The reviewer performed no test runs;
validation is recorded separately in `VALIDATION.md`.

## Validation boundary

P12 lifecycle: 23/23 PASS; natural mortality compatibility: 15/15 PASS;
official Smoke: 5/5 PASS; `git diff --check`: PASS.

The unfiltered EditMode attempt ended `NoResultXml` after about 16 minutes,
before its configured timeout, with no XML or test summary. The official
manifest records zero test counts and exit code `-1`. Unity startup, licensing,
project load, unfiltered test setup, and many test/log entries completed. The
last visible log sequence was a test followed by scene load and managed-domain
reload. Evidence cannot distinguish a particular test issue, full-suite issue,
or process interruption. This is neither a test failure nor a pass. Full
EditMode remains unvalidated for this candidate; no CORE-F completion or
canonical promotion is claimed.
