# CORE-F Daily Advance — Validation

## Source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `dcd3b83a697881506b31646b8d17b41c576e10be`
- Candidate tree: `6572998eeb8f4bca286ff78e4272fd9dfeadb2c3`
- Core canonical baseline: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Repository origin: `https://github.com/TorroisBr/Simulation`
- E-first workspace guidance commit `dc9a01e2` is an ancestor of the candidate.

## Validation results

All successful runs below identify the exact candidate commit/tree. The official runners recorded stable effective inputs.

| Runner | Result | Run ID | Evidence |
|---|---:|---|---|
| CoreValidation cold | 697/697 passed | `44dc6cfcf25f4620836ceaa19e14b7b6` | Original TRX and logs remain in the owner-only E-local retention area. Cold TRX SHA-256: `94cd5f40e4d59f326770533abbc572bc7c99cc537c55e605ffc81cf31c3d2175`. |
| CoreValidation warm | 697/697 passed | `44dc6cfcf25f4620836ceaa19e14b7b6` | Original TRX and logs remain in the same protected run. Warm TRX SHA-256: `eb2f9a120bfe7b1054297724c3007eb1c1655966d1afd983fd3ee398b0cabd8a`. |
| Unity full EditMode | 3258/3258 passed | `b9f1f4a1b53c4190ab69b212347e6e28` | Official normalized XML package is in `Evidence/`. Original XML is retained in protected local evidence. |
| Unity focused admission | 223/223 passed | `b5119ccb347e4c048ef92625ac8af479` | Diagnostic subset; every case and outcome matches the full EditMode report. |
| Unity P12 regressions | 556/556 passed | `d02b0b2f950740eb8089e97c6d9d8eb1` | Diagnostic subset; every case and outcome matches the full EditMode report. |
| Unity official Smoke | 5/5 passed | `7ce84637b28a4bec93a165460da7b2c7` | Diagnostic subset; every case and outcome matches the full EditMode report. |

Unity used Editor `6000.3.9f1` and Test Framework `1.6.0`. The official package contains the full EditMode normalized report only. The packager rejects filtered reports because their parent-suite discovery totals differ from the filtered case totals; those original filtered reports remain unchanged and were not represented as standalone packages.

The official CoreValidation runner reported stable source inputs and generated dependencies, with no Unity `Library` in its isolated .NET worktree. Protected ACL checks passed for the CoreValidation run directory, nested directories, files, TRX reports, and manifests. The Unity ACL checks passed for its retained run files. The independent evidence audit confirmed report hashes, byte counts, test identity/outcome vectors, source identity, and package verification against the private run manifest.

The Unity archive SHA-256 is `b5f5b1699cff88a20604fe4cc820b82083c0dbd93ab52e6bdce43695bc9f7160`. Its normalized XML entry SHA-256 is `3642e9c93c21c134f28e9d036b539df6d368d23cdf49a54b9f7cd75bac6d78d8`. The public package manifest SHA-256 is `accdb04085acbcd4b437eb40a37e5750968783185973ebeb4e6c745be94e00fc`.

## Attempts not used as passing evidence

- A first Unity import normalized two ProjectSettings files, so the focused run was marked input-unstable and excluded. The stable rerun and subsequent full run passed; the candidate worktree's unrelated ProjectSettings changes were preserved.
- Two initial sparse CoreValidation attempts stopped before test execution because the sparse checkout lacked required project-reference or fixed policy inputs. Their diagnostics are retained privately. The completed sparse inputs were then validated by the successful run above.
- Filtered Unity reports are retained as diagnostic originals only. The successful full EditMode package is the published Unity result.

## Evidence boundary

The .NET cold/warm originals and runner manifests are protected in E-local owner-only retention and were independently checked, but this run has no compliant public TRX projection package: the approved adapter requires its private TRX source-context manifest, which the CoreValidation runner did not produce for this run. No raw TRX, logs, absolute workstation paths, or improvised context were published. This limits publication of the .NET evidence package; the official test result itself passed and remains available in protected retention for the approved review process.

## Scope and status

This validates the Core application session's exclusive runtime-advance reservation and Unity forwarding through the existing Core `DailyAdvance` boundary. It preserves that existing scope/lease/completed-boundary/P12-token protocol; it does not add another P12 scope.

The candidate remains an incomplete CORE-F integration. The application advance still receives Unity's concrete runtime boundary, and selected daily execution adapters remain Unity-owned. This evidence does not establish Core-owned selected action execution, public `DailyV1Application.TryStart`, shared detached restoration, descriptor-v2 .NET persistence, a fixed .NET simulation host, or separate-process T/T+1/T+2 continuation. No CORE-F canonical promotion or completion is claimed.
