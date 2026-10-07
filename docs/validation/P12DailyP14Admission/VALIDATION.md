# P12 Daily-v1 P14-A admission correction — validation

**P12 canonical base:** `2d61e19e8c82bfc729d613d3462a087dfba8ac8f`

**Implementation candidate:** `65315888a080d2df3fcddd7f977fe8136566a9bb`

**Candidate `Assets` tree:** `ea356e4e1241b82395cfeb3821705589252d87a4`

**Unity:** `6000.3.9f1`

The candidate rejects P14-A authored ExogenousDaily material-flow configuration
when the selected `UnityBootstrap-Daily-v1` admission context is active. The
guard runs before WorldId allocation, runtime owner construction, or publication.
P14-B finite-source rejection is unchanged. Standalone unscoped P14-A remains
supported. The selected Daily-v1 profile inventory remains at 275 sections and
P8-C City/site bindings remain `ExplicitlyEmpty` at day zero.

`Assets/Scenes/SampleScene.unity` references GUID
`629f53cb3f2547efac2685370c3617e5`, which resolves to
`Simulation-DailyV1.asset`; the selected-profile test passed on this exact tree.
The validation checkout's tracked `Assets` tree matched the candidate tree after
the candidate's two source/test changes were applied. The unrelated modified
ProjectSettings files and untracked `.meta` files in the primary worktree were
not used by or written during Unity validation.

| Suite | Result | NUnit XML (committed SHA-256; original SHA-256) | Unity log.gz (committed SHA-256; original SHA-256) |
|---|---:|---|---|
| Runtime admission, including new P14-A rejection and unscoped P14-A regression | 59/59 | [`XML`](Raw/RuntimeAdmission/EditMode-20261007-125604-c9c7e5013efc43d2a6ad452f04d13794.xml) `24DE192D84C6550D805C43F65586DF8C8CFA62E84B14075994F252C3EC6E535B`; raw `F714CE55E32814ABB751B827E4ED8A5F6CC79EE1265D98619F1501EEB32DDB48` | [`log.gz`](Raw/RuntimeAdmission/EditMode-20261007-125604-c9c7e5013efc43d2a6ad452f04d13794.log.gz) `A4B450B240E47F7A49BA1AA67DCD173CB0B9FBD7745BB91CBAEB6E0D7AC9877F`; raw `535EB907F4555A3436D2516A75549D46BC0DF0B81E6FDAA99EC3CB5847F458FC` |
| Bootstrap composition | 24/24 | [`XML`](Raw/Composition/EditMode-20261007-125619-d057d9e5aca5484cb65c25b44a926127.xml) `E03703DA1568833F03691B8FC0B561917088355D6EAE504702D48C0AFA369A0B`; raw `97CBBE9774AFDF2152C383B44E0EC09E83AD45D2A0AE3DE249BD56B53A8FE3FF` | [`log.gz`](Raw/Composition/EditMode-20261007-125619-d057d9e5aca5484cb65c25b44a926127.log.gz) `A31FC0A9549D7527F05E7F84471459943138BCB58BFA591FC3E62E7F4E0AD71F`; raw `5E99DF32B56B3F2CF5FE39A69D43566B7BA7FDC2B5889A1C09AB80F64DE9533D` |
| Exact selected Daily-v1 P8 geography and owner/cardinality inventory | 1/1 | [`XML`](Raw/DailyInventory/EditMode-20261007-125635-5bf8d0ec23554889b3280bada4a3aca5.xml) `22148A6469EAF146371247DBF64A3C30218ED74E6AC41EFCCA653008B5A0AA6E`; raw `F29E0F352EB14AA103A908C42178673807F442800885AD06927ABEBA85C5DE46` | [`log.gz`](Raw/DailyInventory/EditMode-20261007-125635-5bf8d0ec23554889b3280bada4a3aca5.log.gz) `0D381BC54690E85AD6C82ACB9FEDC948C8DC0DA3A60BB99E1342E480DC829577`; raw `6D20F44FB77650BD0BB4C379549A5575B7DBA00CC4B6290FC1E7520A0E0D0F48` |
| ALL EditMode | 2443/2443 | [`XML`](Raw/AllEditMode/EditMode-20261007-125653-505715babfca4a51bd1a452bf566c368.xml) `F49679D4C2C34A9572CF706E3F9E4DB28E8FAEDCB56AE14B20CFA9FB0B4E9460`; raw `3FA7E23CB44A1CE51560D25936F16B83B3B43FBBF0CD6384D62C5F974644B6C7` | [`log.gz`](Raw/AllEditMode/EditMode-20261007-125653-505715babfca4a51bd1a452bf566c368.log.gz) `9C61EEED1927D1D71B2556DE06984C9BF0EA1FF231989F7CB282165166D8CDF3`; raw `88D5B6A1E266F63A5C94AB98D35CAD1B48DC8B9CA73A58CD73DD0FC8043C9398` |
| Official Smoke | 5/5 | [`XML`](Raw/Smoke/EditMode-20261007-125728-87700d3365734f5797123fe20593f796.xml) `968BF4B834A2D60CDF311FEB99E12658D902C7BB713CEF9FD043A2955CD41CB5`; raw `AE16357385A2E02CFA4194C1814CD7B42E30B899AB532E461595002818F1A3FE` | [`log.gz`](Raw/Smoke/EditMode-20261007-125728-87700d3365734f5797123fe20593f796.log.gz) `6A20E628A686D5D92BC7AFD26C1E1315925081F99A11034F3229C49BF6AA0622`; raw `1D2EF49321D85D0BC0CB6732995D2EDF0132A5023F70D531DDB1817CDD4E09FF` |

All XML results report zero failures, skipped tests, and inconclusive tests.
Committed XML/log artifacts redact machine-specific user/worktree paths and
Unity network-discovery lines; their pre-redaction source hashes are retained
in the table. Redaction does not change test counts or results.
`git diff --check` passed for the candidate code. The change adds no census
section, operation, mutation callback, shared-epoch claim, capture eligibility,
export/hydration capability, or P12-B completion claim. P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked.
