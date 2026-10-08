# P12-D NPC D/F root owner snapshot — exact-tip implementation review

**Verdict: PASS — VALIDATED_CANDIDATE** for the bounded NPC D/F value snapshot, projection merge, and private staged reconstruction slice only.

This is an independent code/evidence review. It does not promote the candidate, complete P12-D, make P12-A ready, or claim whole-profile export/hydration, capture eligibility, or Phase 12 closure.

## Exact refs

- Repository: `TorroisBr/Simulation`
- P12 canonical base: `codex/phase12/canonical` at `5acc3fff94f74fcb718610825caadf645423d672`
- Candidate branch: `codex/phase12/P12DNpcRootOwnerSnapshotImplementation`
- Exact candidate tip: `7fb74bbc36bbc954e7ba0160b32657fbf9de2890`
- Candidate commit tree: `fcb19211f9e407fd4fe0553355abe293ae993780`
- Exact candidate `Assets` subtree: `d7e95170c31947fb611a7461133b60ce75ad1e4d`
- Snapshot implementation source blob: `58c44f1b1a0ff1e3ec2a6b7eef3e7ba108067a32`
- Focused test source blob: `bb3243f68bca7e7c49c8568cd6b3ac49023cda94`
- Design authority: `docs/design/PHASE12_D_TECHNICAL_DESIGN.md`, base blob `a6f72aabc26057b46ec8896738c1006c016d880e`
- Current-base owner crosswalk: `docs/design/PHASE12_D_NPC_OWNER_CROSSWALK_REVALIDATION_ED3AAD0.md`, blob `4a25891122a1b97f99f3b7274daf9b559e6b7134`
- Validation manifest: `docs/validation/P12DNpcRootOwnerSnapshot/VALIDATION.md`, blob `0c970ecf689196590f3ca05d8c646759d3dfb839`

The refreshed canonical and candidate refs were verified before review. The candidate is a clean fast-forward: six commits ahead, zero behind, with merge base exactly `5acc3fff94f74fcb718610825caadf645423d672`. Its integration ancestry retains the canonical base as the second parent of `4bf23109a7b62f7c0d21a08672c98cc9b3d7b983`. The final candidate tree retains the exact tested `Assets` subtree; validation documents and artifacts do not alter it.

## Review findings

The implementation matches the accepted P12-D D/F assignment and the current Daily-v1 boundary:

- Capture accepts only a valid completed Daily-v1 token, requires the exact token owner-vector identity, rejects unsupported profile state, and revalidates token freshness after capture. It captures the concrete NPC once and binds the D and F projections to the same token, transient capture stamp, owner vector, and capture-identity envelope. Staging rejects mismatched projections before candidate construction or membership fill.
- Captured values are detached rows with exact owner-local revisions. Staging uses direct unpublished-owner factories and installs those values without replaying inventory/account/action/travel/Knowledge writers. NPC roots and typed cross-owner references are resolved against the private staged Person, City, Location, definition, route, and TravelParty sets. Definition and runtime identities are unique, reciprocal Person binding is checked, and current/destination City-to-Location references must agree.
- Exact-zero LocalObservation and MerchantTradeState receipt-owner evidence is checked during capture and again before staged assembly. Missing, populated, replaced, or stale receipt owners fail closed; no receipt payload is exported or replayed.
- The Daily-v1 site boundary remains exact-empty for ExplorableSite state; LocalTopology is not added. The candidate does not implement whole-D graph integration, runtime/bootstrap publication, P12-A/P13 behavior, export/hydration completion, or Phase closure.
- City membership assembly validates the full relation set and all linkers before the fill pass. The staged NPC roster is returned only after successful assembly and a final token check; failed staging exposes no staged roster. The paired mismatch/regression tests cover no-partial membership publication. The existing City/NPC assembly evidence confirms order and revision preservation.

No implementation finding remains.

## Validation and artifact identity

The manifest’s XML, compressed-log, and decompressed raw-log hash triples were checked against the exact candidate paths. All five triples match. Counts and hashes:

| Gate | Result | XML SHA-256 | Compressed log SHA-256 | Decompressed raw SHA-256 |
|---|---:|---|---|---|
| NPC D/F snapshot | 22/22 | `BB2EA322EE71E299B8C03B2562B7A6FEE282CED5140915B9CA6EC6D72304A261` | `DC24BC767EF19B20871BEF6006FDC67D8A843E53D29C0C8F5B21A1C729D7EBE9` | `0AE61CD7DFC16556D4C5F03FC592B17543594A945894CB38D4774C784D6326F7` |
| City-root regression | 17/17 | `4C69F376F0E4EDD708BB1529C165EE82C0429E2DC2C6C194673AC1CC6A0E13D2` | `26D28A1C57582C9EE3A883172B4FCDDFBD142273A067AD608084F00E35F5EB4E` | `87D23669DB2E750DFB8ED6E29BEEF7934E151A41B419158C74640875F652E6A6` |
| NPC receipt-owner regression | 13/13 | `9152FB459CD358465189F674CEE9ABBA9F0421E05D8DC2F39EFE9D5283EEA459` | `E577D6FC9120F7F2938822BD0FC30E3ECD7891C8DAA0A49E7FB8074E0506B2A0` | `0ABE4FFF4A817F28F8477456A49202A352A0C018E87703B98583BE98A5EF64CE` |
| ALL EditMode | 2624/2624 | `3703BA8DF426990BFCFBDCC88750B9FF0C75AAF58735DB4B153A7FDBB52B7B4E` | `9973B6BBC44F201BB7A864CA940887129FC730AADB62C39755E7219D6EDBC764` | `C39EEE5F50588C301D51D9ADE41694153677D222FA41BCDF94315C9DD6750AE9` |
| Official Smoke | 5/5 | `E882FEE3622D0A5DA9C7429F87675419E22F03308A93A4DF886A74ED26A4A800` | `41938DD6BC242886AE0FAE7DA42D02F3F0E1BF03616AC3EC11F53F7CB1A22486` | `DC019FC8A3CE0F77A4035A5D8D6E469018235E8423726CCEAF077439074A8F76` |

The cumulative candidate `git diff --check` result is PASS. I did not rerun Unity: the retained exact-tree artifacts are present and hash-consistent.

**Correction to the reviewer’s earlier local note:** the prior `5995…` raw-log value came from my custom inflate path and was erroneous. The exact NPC gzip path is `Raw/NpcSnapshot/EditMode-20261008-204245-785596d2d73141ee9b6eaf40d560ac5c.log.gz`, blob `50fee7c962f5a377a012ea5e53e475510afe6f3e`, compressed SHA-256 `DC24BC767EF19B20871BEF6006FDC67D8A843E53D29C0C8F5B21A1C729D7EBE9`. Standard gzip decompression yields 37,193 bytes and the manifest’s raw SHA-256 `0AE61CD7DFC16556D4C5F03FC592B17543594A945894CB38D4774C784D6326F7`. This is the same exact path/blob, not a stale duplicate; the earlier value is withdrawn.

## Candidate hygiene and limits

The cumulative diff contains no ProjectSettings paths. Its only `.meta` files are the paired Unity metadata for the new snapshot source and focused test. The validation record states that unrelated local ProjectSettings and generated `.meta` changes were excluded. No unrelated user data was changed by this review.

This PASS is evidence for the bounded P12-D NPC D/F snapshot/private-staging candidate only. P12-D remains open; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.
