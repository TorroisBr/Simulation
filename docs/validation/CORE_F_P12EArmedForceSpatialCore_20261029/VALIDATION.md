# CORE-F P12-E ArmedForce spatial authority validation

Candidate code: fc9eb2419185a90b14eb46eb8deb58124ad6291a
Candidate tree: 4e882533a0190f719348f256027bd8beff3e0d3a
Code parent: fb2a127b237a5b4b3f675f02a97999423a9b6c69
Core canonical during validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

This checkpoint moves ArmedForce spatial-position authority and the P12-E military census, codec, and snapshot into Simulation.Core. P8 spatial references continue through SpatialAuthorityStore; P10 SubLocation projection is provided by the Unity topology adapter. P12 capture rejects composed LocalTopology and unsupported spatial bindings. This is a bounded candidate checkpoint: it does not claim P12 or CORE-F completion.

## .NET CoreValidation

Official CoreValidation run 0d72be0b2f2442c0be2409941686f09d passed cold and warm: **922/922** each. The cold and warm CalendarDemo gates also passed. The separate Lab gates were omitted with the runner's supported -SkipLab option. SDK 9.0.315; the isolated worktree had no Unity Library.

The run is bound to candidate commit/tree above. Its stable input digest is cc3615e59d51fd44ed2cee3f51ba009ae8d0aaf2edee6acad12e2cd94871bedd.

The privacy-safe package contains the complete cold and warm result reports:

- Archive: DotNetEvidence/coref-p12e-armedforce-cold-warm.zip
- Archive SHA-256: 1f3ff9a468c3129f9bf7c5593c5b21feb427f5100c379106d42c488a79fa13fa
- Manifest: DotNetEvidence/coref-p12e-armedforce-cold-warm.manifest.json
- Manifest SHA-256: 781ba78e68802653cd1c73108b544bde2df9e7a6098a831e266f071049fb5f9b

The official verifier passed with originalComparisonVerified, comparing both normalized entries with their protected original TRX reports and runner manifest. The package contains two result-only XML entries; its archive scan found no absolute workstation paths.

## Unity validation

Unity 6000.3.9f1, Test Framework 1.6.0:

- Focused ArmedForceSpatialPositionTests: 12/12 passed.
- Focused P12EMilitaryOwnerSnapshotTests: 10/10 passed.
- Full EditMode: **3279/3279 passed**, 0 failed, 0 skipped.
- Official Smoke: **5/5 passed**; the full EditMode report also contains all five passing Smoke cases.
- git diff --check: passed.

Full EditMode run 3c68046ea2864a9cb699476483a386ea is retained as a privacy-safe package:

- Archive: coref-p12e-armedforce-spatial-editmode.zip
- Archive SHA-256: d8bdd62c41a20f032f31d8c50f9a4aa7312d47b23365d0058d9be749fcfce287
- Manifest: coref-p12e-armedforce-spatial-editmode.manifest.json
- Manifest SHA-256: dc83bcfa8a4026135a6ca48d156ca69c652d9c223ca5a12bde58404d983cea55
- Original XML SHA-256: 4414dc38672ee6c728a4de29fb3d624761e1154d93af21f22ffbd5bde842585d
- Normalized XML SHA-256: af812280143ec2c86ffe22399c6a566cf939ca0f3892164c7935f2c3dc0d0505
- Input snapshot SHA-256: aef78db4540bd01d8dc6806ca48bd2d1b65c997c6cd0bb2a717b136c6f230e5e

The Unity run's recorded repository anchor predates this candidate commit. The manifest separately binds the complete 1,965-file validation input snapshot. After the code commit, all 1,965 recorded file hashes were checked against the candidate worktree: 1,965 matched, none were missing or changed. Three user-owned ProjectSettings edits remained local, unchanged, and outside the candidate commit; their tested identities are included in the input manifest.

The Unity archive scan found no absolute workstation paths. Original XML and raw logs remain in protected local evidence; no raw reports or logs are included in these public packages.

Independent exact-tip review is pending. No Core canonical promotion is claimed.