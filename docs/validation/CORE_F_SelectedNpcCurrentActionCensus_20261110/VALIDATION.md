# P12 selected NPC CurrentAction census handoff — validation record

Status: candidate validation recorded; this is not a canonical-promotion evidence package.

## Source identity

- Base: `c1a5da9d10b0acbc01a37cd9afe86f90d589397f`
- Tested source/test commit: `729a70c6d265e5bf7ba1abb5e35d438e2ab1ebef`
- Tested Git tree: `b719183d0871ab8aca6a35bd1afc7ec0d9ffdb65`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The five committed implementation/test paths matched the tested checkout after the commit. No code or test changes followed the runs.

## Results

- Official CoreValidation run `0df715adeed344a99b3dd24a262f0343`: cold 1089/1089 PASS; warm 1089/1089 PASS. Original TRX SHA-256: cold `1688d83b0e8410b69a4ce3d0a7948425eec26811518bf3c6311f93a11d13e21c`; warm `d46ce2963942631692ff3af277d3d89e5f7d514469095f7ee4f8c0903d4948b4`.
- Official focused Unity EditMode run `b6386fbdb51848c48d89d58d55d4cd49`: 246/246 PASS, zero failed/skipped/inconclusive. Original XML SHA-256: `2c070063b813552c210bdb6282c1fcc399bc82ce7ed5150bf05fcdf169ea9513`.
- Official full Unity EditMode run `385c6c08c593437b8b52b876ae1585bd`: 3305/3305 PASS, zero failed/skipped/inconclusive. Original XML SHA-256: `c41e4b18bbf1e339f39f2ea6142f08dfdea1f39ebd758c447687e6ce927ad4f6`.
- Official Smoke run `62ed72d4eb2e4deabe1fa42915623a7f`: 5/5 PASS, zero failed/skipped/inconclusive. Original XML SHA-256: `503209d2e520a9bd862eafca851796fc5e4463953a5a76959c720f22791732d2`.
- `git diff --check`: PASS.

## Evidence boundary

Original XML/TRX reports, raw logs, and runner manifests remain in the protected local evidence store; none were added to Git. Their hashes and run IDs are recorded above. The result reports were locally inspected for complete outcome counts, but this note does not replace the required permanent result package or independent raw-to-retained comparison. Canonical promotion remains blocked until the applicable durable evidence contract is satisfied. No raw output or local filesystem path is published here.
