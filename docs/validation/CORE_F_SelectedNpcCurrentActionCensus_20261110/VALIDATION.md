# P12 selected NPC CurrentAction census handoff — validation record

Status: candidate validation recorded; this is not a canonical-promotion evidence package.

## Source identity

- Base: `c1a5da9d10b0acbc01a37cd9afe86f90d589397f`
- Tested source/test commit: `729a70c6d265e5bf7ba1abb5e35d438e2ab1ebef`
- Tested Git tree: `b719183d0871ab8aca6a35bd1afc7ec0d9ffdb65`
- Unity runner input snapshot: HEAD c1a5da9d10b0acbc01a37cd9afe86f90d589397f, full snapshot tree 313b77049a82a47d00c4be3787d57001f33e0a2d, as recorded in the protected run manifest. This is the runner's complete worktree-input snapshot, not the committed candidate tree above. Unity-captured implementation/test inputs correspond byte-for-byte to the reviewed candidate paths; the candidate commit records their post-run committed tree identity.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The five committed implementation/test paths matched the tested checkout after the commit. No code or test changes followed the runs.

## Results

- Official CoreValidation run `0df715adeed344a99b3dd24a262f0343`: cold 1089/1089 PASS; warm 1089/1089 PASS. Original TRX SHA-256: cold `1688d83b0e8410b69a4ce3d0a7948425eec26811518bf3c6311f93a11d13e21c`; warm `d46ce2963942631692ff3af277d3d89e5f7d514469095f7ee4f8c0903d4948b4`.
- Official focused Unity EditMode run `b6386fbdb51848c48d89d58d55d4cd49`: 246/246 PASS, zero failed/skipped/inconclusive. Original XML SHA-256: `2c070063b813552c210bdb6282c1fcc399bc82ce7ed5150bf05fcdf169ea9513`.
- Official full Unity EditMode run `385c6c08c593437b8b52b876ae1585bd`: 3305/3305 PASS, zero failed/skipped/inconclusive. Original XML SHA-256: `c41e4b18bbf1e339f39f2ea6142f08dfdea1f39ebd758c447687e6ce927ad4f6`.
- Official Smoke run `62ed72d4eb2e4deabe1fa42915623a7f`: 5/5 PASS, zero failed/skipped/inconclusive. Original XML SHA-256: `503209d2e520a9bd862eafca851796fc5e4463953a5a76959c720f22791732d2`.
- `git diff --check`: PASS.

## Evidence packaging

- The full Unity EditMode result was packaged with the approved `Package-UnityValidationEvidence.ps1` tool on pinned PowerShell 7.6.5. The public package contains only normalized result XML: [manifest](CORE_F_SelectedNpcCurrentActionCensus_Unity_20261110.manifest.json) (SHA-256 `FD413B5A8500DC24BA23EE9034A462C1AEF719EE1BB34701C7F88D6B0F8EC2E2`) and [archive](CORE_F_SelectedNpcCurrentActionCensus_Unity_20261110.zip) (SHA-256 `826300AD593A8811C86D9ADC8FC088597BC62840193AC2FDF81D3745CF6D8A9B`). It records 3305/3305 passed, zero failed/skipped/inconclusive. Original XML SHA-256 is `c41e4b18bbf1e339f39f2ea6142f08dfdea1f39ebd758c447687e6ce927ad4f6`; normalized XML SHA-256 is `a66e9b50f6237532ca1b1c82faf0bcfdeff9d71cc0b02bd7317e55b8a5ecc54b`. Official package verification against the exact protected EditMode run manifest passed; the original report remains unchanged in protected evidence.
- The Smoke runner reported 5/5 PASS, and its original XML SHA-256 remains `503209d2e520a9bd862eafca851796fc5e4463953a5a76959c720f22791732d2` in protected evidence. The approved packager rejected that XML because the `Simulation` suite's declared `testcasecount` differs from its descendant test-case records. The original was not modified and no alternate packaging path was used. This is an evidence-format blocker, not a reported Smoke test failure.
- CoreValidation's cold/warm TRX reports and runner manifests remain in protected evidence and are not yet represented by a verified public TRX package.

The EditMode report is now durably packaged and source-compared, but the Smoke and CoreValidation evidence gaps mean the complete permanent evidence package and promotion gate remain incomplete. No raw XML or private log was added to Git, and no canonical promotion is claimed.
