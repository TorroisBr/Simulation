# Test case identity map — admitted travel/P12 boundary preflight

The two affected test identities are from the official CoreValidation cold run and are recorded here additively; prior identity maps remain unchanged.

| Test ID | Test name | Outcome | Role in this checkpoint |
|---|---|---|---|
| `88c786ad-69c8-d3cf-9621-2b66d12a8bbd` | `AdmittedTravelBoundaryFailurePrecedesLoggerAndEnabledDemographyWriter` | Passed | Proves a missing admitted selected travel/P12 context rejects before logger and enabled demographic-writer callbacks, and publishes no daily result. |
| `678aa62a-a3a7-6508-0f9c-746c26fc6f76` | `StateBackedDayStartLetsCoreSelectCompatibilityForUnselectedLiveMember` | Passed | Retains mixed-roster Core selection of the established fallback and asserts that fallback begins before the later travel-context acquisition. |

The cold and warm CoreValidation runs each reported 909/909 passed with stable source inputs. The original TRX reports remain in protected local evidence and are identified in [VALIDATION.md](VALIDATION.md).