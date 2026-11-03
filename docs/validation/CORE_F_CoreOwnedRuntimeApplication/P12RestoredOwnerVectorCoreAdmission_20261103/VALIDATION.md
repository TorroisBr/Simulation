# Restored-image owner-vector revalidation — 2026-11-03

This record validates the restored-image target-vector correction at candidate `84d26ff0b21a7373ea219bc643fb9434112a75b7`, Git tree `bb98684a0559191e9972ff243ce46e7bdea5f126`. It does not promote or close CORE-F, claim P12 readiness, or authorize the detached host.

The production input is `DailyV1Application.cs`, Git blob `3d563365cb25be8821512bf1ac9dd36a616992b4`. The development checkout and exact-candidate Unity checkout both clean to this same blob. Unity checked out CRLF text (166,769 bytes, SHA-256 `c8ca3e8942b8a798871ae35280bd87b1e258d5968572803564df919c862b9a79`); the development checkout has mixed line endings (166,673 bytes, SHA-256 `eb1c1c574b8e494ab2b465ab152ff79ceee28c68ce21eabf33fc856ea6ffd8f8`). The Git blob identity is equal in both and matches the candidate, so these working-copy byte differences do not represent different source.

## Validation

- CoreValidation run `a4430f0d5f1f47d6a0ae2d1da57332cd`: cold 1001/1001 and warm 1001/1001 passed. Manifest SHA-256 `5118566409ebfe85d977dc1c8024bdc595e4f4b4db750afeb05e7e61c226094f`. The runner records its base checkout as `64334695bea39a2a811265425846c207460040fb` and working-tree input digest `b4e56cb367094de70c98a6dd38345bd927f8d4ffeb881082ae94c57c879acabc`; its `DailyV1Application.cs` input clean-hashes to the candidate blob above. Protected ProjectSettings and pre-existing untracked/evidence inputs were not staged or altered by this run.
- Focused restored-image continuation test run `438f8e6d5bfe4956865209e1dcc23072`: 1/1 passed; stable inputs. XML SHA-256 `5cdd8cc5b04b35318c3e1b188212400da0f795136e57f7371ef034f04f1d3d1b`; run-manifest SHA-256 `9948d4d25fd55aa676231ab1b693fcec61e1ee6f59ac767d9409a724674a9a27`.
- Full Unity EditMode run `96f5bdb35ce8452bbd9933371d677f78`: 3296/3296 passed, zero failed/skipped/inconclusive; stable inputs. XML SHA-256 `c7287743a2363771d09ef18f7a81a60ef44360630d24845c1a067f81a6b930d3`; run-manifest SHA-256 `50fed177d826615bcfee04060cbf85ed971e7d61cf034bbed236e11d7861ec1`.
- Official EditMode Smoke filter run `ba7d7db0950c49c9aa1f49f36a7acf6c`: 5/5 passed; stable inputs. XML SHA-256 `94ba0c32c1b6009f98368dd1a9035abde720458ae85d8f7160e99792314be0d8`; run-manifest SHA-256 `7550c638f2d6b8d2d0e5383a011a4442c464ad1218374f152ec34a2a772147d5`.
- An earlier first-import focused attempt also passed 1/1, but its manifest marked inputs unstable because Unity rewrote `EditorBuildSettings.asset` and `ShaderGraphSettings.asset`. It is excluded; those local changes remain preserved in the isolated validation checkout. The stable focused run above followed that import.

## Packaged Unity evidence

The official `Package-UnityValidationEvidence.ps1` package for the full EditMode run is in [`UnityEvidence`](UnityEvidence/). Official Verify passed (`runs=1`, `entries=1`). Manifest SHA-256 `7b7b275fe8cc67ea45218fdde60d05ba23e69f29643f59cb4dbc0b848ca0c2c8`; archive SHA-256 `370bcbbb2c0a910c9cdaa03051cf0067fd65f483c028c1725fd0a4d7558d1b14`. The package scan found no local workstation paths. Original XML and logs remain preserved in private evidence storage; this record does not rewrite them.

The full EditMode report contains the focused regression and the five Smoke identities. This remains a narrow census/restore validation result; full CORE-F integration and its detached continuation contract are incomplete.
