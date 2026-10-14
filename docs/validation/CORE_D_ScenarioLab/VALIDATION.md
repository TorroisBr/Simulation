# CORE-D shared runtime and Lab validation

## Candidate identity

- Core canonical base: `af084c2028528d8588268801daaee14a905f5f5b`.
- Implementation commit: `e83ea3ba7c9cbddf76280f20e12785ce82383e43` (tree `33a610623a9443e9755874ae88bcc27ebdfedad3`).
- Validated candidate/tooling commit: `e53857e3f157def116136001e78af97e64ff50cb` (tree `cf63c3db6054f749a82c631ea85c7e216001cf04`).
- The `Assets`, Core package, `src`, `tests`, and `tools/Simulation.Lab` subtrees are identical between the implementation and validation commits. The second commit only separates the existing combined Lab check from the current TRX-compatible Core run and adds a dedicated Lab evidence runner/documentation.
- Unity validation input snapshot: 1,584 files, digest `ed083356bd3d9aa5048ecff8a7d92eba97a8f46dff61fa78242b95456546a75b`; every capture reported a stable before/after inventory and bound Git head/tree `e53857e` / `cf63c3d`.
- The candidate worktree retained the user's ProjectSettings and untracked `.meta` changes without staging or editing them. Their actual input bytes are bound by the validation inventory; the public evidence contains only relative paths and hashes.

## .NET Core and Lab

The pinned SDK was .NET `9.0.315`, with selected .NET runtime `8.0.28`, Release configuration, and `net8.0` tests.

| Gate | Run | Result |
|---|---|---:|
| CoreValidation self-tests | candidate and sparse checkout | PASS |
| Combined Core cold/warm and Lab scenario | `01cb94cd56ad40e8a0d674fff0ac7166` | Core 113/113 cold and warm; Lab cold/warm output identical |
| Packager-compatible Core cold/warm run (`-SkipLab`) | `1fe85a46a0754e57ab68ba4cb883b8fb` | 113/113 cold and warm; zero failed, skipped, inconclusive, or other non-passing outcomes |
| Standalone locked Lab restore/build/cold/warm | `8d517ab772114c87bb90705e9c63cf0a` | PASS; exact output parity; no Unity `Library` in the sparse worktree |

CoreValidation's complete TRX run bound 65 committed input files and three generated restore assets. The standalone Lab run used the same source commit, tree, and input digest, and bound its locked restore asset. Both runners retained logs and manifests in owner-only private evidence locations.

The Lab fixture output was identical in the combined and standalone cold/warm runs:

```text
scenario location=location.p14b-lab settlement=settlement.p14b-lab source=source.p14b-lab market=market.p14b-lab item=item.p14b-lab
day=1 status=Applied produced=3 reserve=2 sourceRevision=1 marketRevision=1 stock=5 price=3.2 priceBits=404CCCCD
day=2 status=Applied produced=2 reserve=0 sourceRevision=2 marketRevision=2 stock=7 price=2.2857144 priceBits=40124925
day=3 status=Exhausted produced=0 reserve=0 sourceRevision=2 marketRevision=2 stock=7 price=2.2857144 priceBits=40124925
```

The cold/warm Core TRX package is `cored-net-cold-warm-e53857e.zip` (archive SHA-256 `6a5d194649f719118c4b2289454bc94450f1aeff1beb8d6a2c32cfbd769739f2`); its committed manifest Git-blob SHA-256 is `4bba39e0d1d5d55709b29385cad657f595b89b8fe300f8a0c1dbf324852f33a4`.

Manifest digests identify the committed Git-blob bytes, avoiding platform-specific checkout line-ending conversion. Archive digests identify the ZIP bytes.

The approved `DotNetTrx` packager compared both normalized reports to their unchanged originals and verified the exact source, command, runtime, logger, test identity, assembly/PDB, and retention evidence. Context-bound verification passed with `originalComparisonVerified`; a separate public archive-only verification passed with `originalComparisonNotPerformed`. The public package contains only normalized cold/warm TRX entries, with no local absolute path or private evidence locator. Original TRX and command logs remain unchanged in protected storage.

## Unity validation

All runs used Unity `6000.3.9f1` and the same stable 1,584-file input inventory:

| Suite | Run | Result | Original XML SHA-256 | Original log SHA-256 |
|---|---|---:|---|---|
| Focused `FiniteSource` | `86b373efda6b4f4aa8eff98d6628d3ed` | 41/41 | `6fbe2d80f5ad59cfcab7bc6cacd668d1d5e95ddcc86349ec2fb9327b61f702cb` | `62042b2a2ac9e08b74e3b797c6564f2a1c3991dfbf0a754e80b20323ffe1a31c` |
| Official filtered `Smoke` | `3d8cee82a32c4ea2b6e288dc1aebaa71` | 5/5 | `9675baa813c29d84d978e05601f1f70dbac3a680b3cc869b3a0af517eb380f43` | `3c174d7beb82b355b190c04a678b4b8b75fbfd50fafaf88a1161c8ddc3f745f5` |
| ALL EditMode | `ce16fed9c2a6423e925e2834cf1a763f` | 3,120/3,120 | `6b1be03931809eeda031f58fcf0fc89067acafd1deffe0d3281955bcb6216ce4` | `d87670ffb029b4df6cdbfff37a99c57a5d7d2be0720a40b7f5f6dd2f35828572` |

The deterministic normalized ALL EditMode package is `cored-editmode-e53857e.zip` (archive SHA-256 `85470f220d081cd2b8654f5ba7559855590e85f3018e00812a990d50f8182a5d`); its committed manifest Git-blob SHA-256 is `d4688415d2182fd4c92a1f1df666cf39bf784b9b821acd72c0a6b9aee7c0c426`. Package verification passed. The archive contains only normalized result XML. Filtered focused and Smoke originals/logs remain private and unchanged.

The earlier `cored-editmode.zip` and `cored-editmode.manifest.json` artifacts were preserved unchanged (SHA-256 `3c68fe19d88464c462a1d76acd4c263137799bd6926b43603a8e7c72979d0ee6` and `ab57574b4a183c83b303ab6091ed465cc1e6589d86181916fdf69ddf914c12ca`, respectively).

## Review and promotion status

This record binds the implementation and evidence; it is not an implementation review or promotion record. Independent exact-tip review and the normal Core canonical preflight are still required. No P12 continuation, P13, save/load, population, complete P14-B City economy, general .NET host, or public extension claim is made.
