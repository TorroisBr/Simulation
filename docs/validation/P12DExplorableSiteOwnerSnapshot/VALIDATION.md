# P12-D ExplorableSite owner snapshot validation

**Status:** Candidate validation PASS. This isolated owner slice is not P12-D
integration, P12-A readiness, P13 readiness, or Phase 12 closure.

## Candidate identity and review

- Implementation candidate: `f4f0f5d6e54c87638ce00261fb4d0add0803c5ef`.
- Exact implementation base: P12 canonical `63cb5e7156ce703f73f78b8837a13889d7f92492`.
- Reviewed `Assets` tree: `71d917e5bd4090368f5be1536a6cbb2e789bed64`.
- Exact-tip implementation review: PASS, review branch
  `codex/phase12/P12DExplorableSiteOwnerSnapshotExactTipReview`, record commit
  `8290a9a9fa19409a48349a053bf3c9794aba28de`.
- Current-base integration branch: `codex/phase12/P12DSiteCurrentBaseIntegration`,
  based on current P12 canonical `dbba3e9a227f66da0381e3e042e826518d63c240`.
  Integration changes only the P12-E design/State and the Site review document;
  the `Assets` tree remains exactly `71d917e5bd4090368f5be1536a6cbb2e789bed64`.
- Independent current-base review: PASS, classified `BASE_DRIFT_ONLY`, review
  branch `codex/phase12/P12DSiteCurrentBaseIntegrationReview`, record commit
  `16dce50d34969391da99364c3414a0845af42187`. It confirms review `8290a9a`
  remains included and validates compatibility with the promoted P12-E
  `NOT_COMPOSED` correction. All validation below applies to that same
  unchanged `Assets` tree.

The implementation changes only the Site owner snapshot, `ExplorableSiteStore`,
its `.meta`, and census tests. Capture rejects duplicate runtime/site-instance
identity state without changing existing live `Add` behavior. The selected
Daily-v1 composition proves the installed site owner remains exact-empty. No
P10/LocalTopology, profile, runtime, bootstrap, P12-B capture-token, registry,
cross-owner graph, or publication integration is included.

## Validation

All results below are from the integrated candidate and exact
`Assets` tree `71d917e5bd4090368f5be1536a6cbb2e789bed64`. No failed, skipped, or
inconclusive tests were reported. `git diff --check` against the current
canonical base passed.

| Gate | Result | XML SHA-256 | Compressed log SHA-256 | Original log SHA-256 |
|---|---:|---|---|---|
| Site census | 12/12 | `27C6B24E81948B6CA876922C91971200B86E149BBD09B9973372389B269C67D9` | `CDB45CAD421F0D09E82BD49F7349CBE841B11D97924BB923A1BD1396D1A0FFFF` | `5E8071711C0E3A10F1D848C483510C79AF757004AAA458930E9F280910538393` |
| Selected Daily-v1 exact-empty profile | 1/1 | `394B89353AFF98AA7AB5F057DBAC23593CCCE86E203AA7D715255A1B60A60EE1` | `BC2A62F97986C90E00E74D43BDD32591D4FDB715638C214CDE0A052FA25DF965` | `3859754E4C2F2597F50247CF10FF79FB31D88CD46C6011AB73C7743D1B754786` |
| ALL EditMode | 2556/2556 | `9237DDFB271FAE6A92C0DAE4EC3553BA6684F0F0D789B1FA3292F96B85E547ED` | `6FC9D427E72D0ADF65176FCDDBFA87F45226A07A37E046B6D74459340234D1FF` | `3BF05E5ED4E83F96DE71C8100A16112A18B8E9679F48BC2C4FCC4C52449351C3` |
| Official Smoke | 5/5 | `DA440AEC6573CB7B7FE8C823FE95B3E6FB5C9F859348640EA45ACD731FAFFF4E` | `DF5FFCE67DFD7D60A8689381708E934B2F758918F7EC871F9F02D3BE1D64D0D7` | `91D30E5D242B8E39D764459F94C16E8BB7FC7E63EE343DB8C2FE78924E2571BB` |

The XML files and losslessly compressed `.log.gz` files are retained in
[`Raw/`](Raw/). Each compressed log was decompressed through a SHA-256 stream
check against the listed original-log hash before the uncompressed copy was
removed.

## Scope boundary

This isolated factory does not validate the complete cross-owner graph, use
P12-B capture tokens/vectors, update the P12-C `RuntimeIdentityRegistry`, or
publish an enclosing staged runtime. Later P12-D integration remains
responsible for staged registry registration, reference concordance,
cross-kind runtime-ID collisions, cross-owner validation, and atomic
publication. P12-B remains complete only within its recorded bounded contract;
P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
