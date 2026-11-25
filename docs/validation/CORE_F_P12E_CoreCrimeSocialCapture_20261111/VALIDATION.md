# CORE-F P12-E Core-owned Crime/Social capture

Status: validated implementation checkpoint; CORE-F remains incomplete and
unpromoted.

## Source identity and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Base: `74c2788f83b5fe2176eb615cf57cb48fa534c75e`.
- Tested implementation commit: `c1bf84233de9e7fdd7b28f164c4724862fc95dfc`.
- Tested commit tree: `287676e58399f04823bd8bdcaddc60815dacf976`.
- Scope: the active continuation-save operation now captures the exact
  `CrimeSocialAppraisalWorldState` held by its selected Core owner graph and
  Core encodes its three existing P12-E sections. The Unity compatibility
  adapter remains for the legacy path that does not have an active Core save
  operation. Section identities, owner stamps, revisions, restored clock
  identity, and the 89-family census contract are unchanged.
- Changed source/test files: `DailyV1P12ECoreOwnerSourceCapture.cs`,
  `P12EDailyV1OwnerPackage.cs`, and
  `SimulationRuntimeAdmissionTests.cs`.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold | 1,092/1,092 | Run `3c5d069e2b2645e493391b7a0f2c6edb`; stable input digest `70a2544e202d9b8eeba71301f6ee5499056c3daa9c8beaf67db7346e4c478812`. |
| CoreValidation warm | 1,092/1,092 | Same official run and input digest. |
| Unity full EditMode | 3,306/3,306 | Run `77d2c144505640529e437d112f91e91f`; stable input digest `f20bb523f6eac64286a1011ac6b5d422e42305c6f0354789c3bcfbca23c4a9dc`. |
| Official Smoke | 5/5 | Run `2ab71c8dd2264282af438a13d6bb1687`; same stable input digest as full EditMode. All five Smoke identities also appear in the full EditMode result package. |
| `git diff --check` | PASS | Source/test candidate. |

The three changed source/test files match the full EditMode run input hashes:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/P12EDailyV1OwnerPackage.cs` | `708d54cf1a0c491f373e85143b03a78f7a350148959370ef739c45eba57c539d` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `5a4395253c027cb0efda229302a68c7484bc4ddfa16f57546740d0715bad3e84` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12ECoreOwnerSourceCapture.cs` | `d9ac869dedabfcaec663227a2ceb75d9eac9ecc44fedefc9ae1d802e7ccc64a9` |

The CoreValidation input manifest also matches the Core capture source hash.
Original CoreValidation and Unity run manifests/XML/logs remain in protected
E:-local evidence storage. The CoreValidation run observed a Unity `Library`
in this checkout, so no Library-independent .NET-host claim is made. The
Unity input snapshot included the pre-existing local `ProjectSettings.asset`
edit; that file was not staged or committed. The filtered Smoke original is
retained privately; the full packaged EditMode report contains those five
cases and is the normalized public result evidence.

## Unity evidence package

- Package: `UnityFull/coref-p12e-crimesocial-capture-20261111.zip`.
- Archive SHA-256: `5efb773a9dee3756ff3c6887322f06a4b27e9537c439ddf95f6bfc5b5bdd378c`.
- Package-manifest SHA-256: `e856398795fc956049e9e78aecdbf4e65c12fff32a94253f36fc13938cbe9b07`.
- Official verification against the protected run manifest: PASS.
- Protected source run-manifest SHA-256: `389d2acdadf87ce9ad684435d7436394c0d0f5c4a57f4f7bd96f1dd6c4310266`.
- Protected Smoke run-manifest SHA-256: `abad46b3d42a74999aa493455de7b0780f19fb4f335f3fbaa80c23ba05ff3a23`.
- Protected CoreValidation manifest SHA-256:
  `d23b7f87405b37d863ed77e61b0b6e52b2b3d4e60f755177c020a68c64120594`.

Independent exact-tip review passed for implementation commit
`c1bf84233de9e7fdd7b28f164c4724862fc95dfc` and tree
`287676e58399f04823bd8bdcaddc60815dacf976`; see
[`IMPLEMENTATION_REVIEW.md`](IMPLEMENTATION_REVIEW.md). The later candidate
commit `d4b7bf20205cf600cb5d5ee5f85c73d305db0779` and this review record add
only documentation and the normalized Unity package; production and test
sources remain unchanged. This checkpoint does not complete CORE-F, establish
detached .NET continuation, or authorize Core canonical promotion.
