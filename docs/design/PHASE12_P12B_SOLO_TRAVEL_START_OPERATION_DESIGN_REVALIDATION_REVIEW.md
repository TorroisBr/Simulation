# P12-B solo travel-start design revalidation review

**Verdict: PASS.** This review does not approve implementation or canonical
promotion.

- Revalidation tip: `4b79a5d176dd73e2814dbfdf76b73c6117ec8d23`.
- Canonical base: `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`.
- Prior design tip: `91728e4aee7717c6b002691a9a9e96c4ad22ac71`.
- Prior design review record: `812b04628bd8d3f04c491b1df4adae7e2c3692e2`.
- Independent reviewer: `/root/solo_travel_design_review`.
- Reviewed artifact: `PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_DESIGN_REVALIDATION.md`.

The reviewer verified that the `ScheduledDirective` constructor permits only
the EscapePrison operation and requires an EscapePrison action even in
`RequestAction` mode. The clarification correctly declines to add Travel
scheduling support; the existing runtime dispatch is shared, but no supported
scheduled Travel input can be constructed.

The source-City refinement matches `NpcRuntime.StartTravel`: the projection
section is changed and reported only when `currentCity.ContainsImportantNpc`
is true. The terminal travel-state revision scenario also exercises the
existing TravelSystem charge, `StartTravel` rejection before travel-state
commit, and compensating credit without inventing rollback behavior.

This PASS approves the technical clarification only. The implementation
candidate still requires the requested code changes, exact-tree validation,
and a fresh independent implementation review before canonical consideration.
