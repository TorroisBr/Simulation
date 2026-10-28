# Daily-start boundary preflight — test identity

This map is additive and does not renumber or modify the historical 49-case
CORE-B identity map.

| Current .NET identity | Prior identity | Disposition |
|---|---|---|
| DailyV1SelectedDailyExecutionStageTests.AdmittedDayStartBoundaryFailurePrecedesEnabledDemographyWriter | — | New Core daily-stage preflight regression |
| DailyV1SelectedDailyExecutionStageTests.AdmittedStageRejectsMissingSelectedDayStartContextInsteadOfDelegatingToUnityWholeDay | Same identity | Existing regression; assertion now verifies no logger callback before boundary rejection |

No historical test identity was renumbered or removed.