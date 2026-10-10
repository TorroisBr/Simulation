# P12-G P8-D populated target — current-base revalidation

**Classification:** `BASE_DRIFT_ONLY`

The original validated candidate was based on P12 canonical `cabf47185bf4b5d157157a4cb48b4f897db25127`. Canonical advanced to `4a11f19c791d9be08e11d52a6f28f26bff05bc70` through the documentation-only Person-position State correction and its exact-tip review. That change records an already-promoted P8-C witness and does not affect P8-D semantics or executable files.

The P8-D implementation was recomposed additively on the current canonical. Current-base code commit: `f21c2e11ceb2829ca736434aacd4d1c8ce78fb03` (tree `9a1bd4c6befbb1ea4a19ca5d6612130b19d5471d`). Its `Assets` tree remains exactly `fb53856cec5ed6ca36c170aab7eb6dee90ca3d46`, and the tested `SimulationRuntimeAdmissionTests.cs` blob remains exactly `27d1be30ba8aa02d02f476c8f737021e20c80b78`. The focused, full EditMode, and official Smoke artifacts in `VALIDATION.md` are bound to that unchanged `Assets` tree. No Unity tests were rerun because the executable tree and tested source blob are unchanged; fresh exact-tip review is required for this recomposed candidate.

The scope remains only rejection of a populated `p8d.spatial-route-observations` target. No P8-D populated hydration capability or P12-G/P12-A readiness is claimed.