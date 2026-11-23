# Independent review: CORE-F selected owner census provider composition

## Verdict

PASS. No actionable implementation, contract, or test-integrity findings.

## Reviewed identities

- Repository branch: codex/core/CORE-F-restore-stage-transaction
- Source implementation commit: 92aa9c03dfe61f0af5e3019e7a30906de5a360b8
- Source implementation tree: 5bbf12ec04f8e63ddf806e87911ea084ebc27873
- Direct source base: 0aa064b74ba884c91f29f245ca303fa9e48ecc3f
- Evidence-record tip reviewed: 9113038698c3771d20ce13f16a1d8c826d4e6cde
- Evidence-record tree: aadc190434d080110e9030e1e31399a91139b0ee
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Findings

The composition consumes the exact selected owner graph and combines the currently Core-owned PersonStore, City, identity/spatial, and selected NPC provider groups. It validates group membership, exact PersonStore owner identity, provider witness schema/role, owner identity, cardinality and revision baselines, deterministic ordering, and disjoint section identifiers.

Unity uses these same composed provider objects for the Core-owned groups after its host projection checks. Graph-unavailable setup remains deferred. Mixed or unselected rows keep the Unity host-provider path. Runtime NPC membership transitions create a fresh provider for the changed membership boundary rather than reusing stale composed registrations.

The implementation and tests do not claim complete 89-family census coverage or P12/CORE-F readiness. The Core-only action test installs the composed provider subset over the fixture's complete census vector and exercises an admitted action; unsupported rows remain fixture providers.

The Unity evidence manifest binds the full EditMode run to the reviewed implementation commit/tree. The normalized archive verifies with SHA-256 175fba0ccf63ee07de9710b04c33fa750e9b8ad9be75b90c5849ed67c96e2f34 and reports 3325/3325 passed with no failures or skips. The local CoreValidation manifest and original cold/warm TRX sizes and hashes match the exact source identity and report 1194/1194 passed for each run. CoreValidation evidence remains private and has no public normalized TRX package; this does not block candidate publication, though the governing evidence policy may require additional packaging before later canonical promotion.

## Scope limit

This review covers the stated bounded provider-composition slice only. It does not approve Core canonical promotion, complete selected-profile census coverage, detached restoration readiness, public DailyV1Application.TryStart, or CORE-F closure.
