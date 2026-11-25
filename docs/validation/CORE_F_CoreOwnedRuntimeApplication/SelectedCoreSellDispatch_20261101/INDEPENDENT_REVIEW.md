# Independent Exact-Tip Review — Local SellGoods Projection Recovery

## Verdict

PASS. No actionable implementation findings.

## Reviewed identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Exact candidate tip: 5c9f048c2b2cf246a5705d0c814059fcf7e90923
- Candidate tree: 06f7197a7ba52c35c63dfe403793942d987335f1
- Parent: dbfc01b27aa84d633157ff7d07a7dc2e4a4a2589
- Implementation commit: dbfc01b27aa84d633157ff7d07a7dc2e4a4a2589
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Review result

The exact projectionless local SellGoods payload is limited to the seller’s current City and selected item. Runtime reconstruction resolves the exact City projection and graph item asset, binds the compatibility projection to the existing Core payload, and installs it without notifying a P12 mutation boundary. Core remains authoritative for eligibility, deterministic resolution, and the account, inventory, and market writes.

The regression covers missing and duplicate City projections before the Core execution/RNG path; zero draws and unchanged action/account/inventory/market revisions and census epoch on rejection; exact action/payload/City/item identities; no action-slot revision or P12 epoch change during projection reconstruction; one deterministic draw; expected successful owner revision and epoch changes; and final census validation. Existing projected dispatch remains unchanged. The test restores its temporary shared action-asset settings in finally.

No assertions were weakened. The committed source hashes match the stable validation inputs. The verified public package contains the complete EditMode result including this test and has no workstation or private evidence paths.

## Validation evidence

- Focused: 1/1 passed; run 06f983466d764255a54a80bca7d2452c.
- Full EditMode: 3290/3290 passed; run b111d4aafc52402dbc3cd8bfac077eff.
- Official Smoke: 5/5 passed; run ddb6915908264aeebfc9f926ae436e60.
- Evidence package verification: VALID, one run and one normalized XML entry.
- Archive SHA-256: e07929f897c8cc1db827a8f2a9028ad5dc3456319b7bf7c2c68de99c47d68bf8.

The validation details and source hashes are recorded in VALIDATION.md.

## Non-blocking coverage note

There is no separate negative fixture for a missing item-definition-to-ItemData crosswalk. Production resolves that crosswalk through the exact selected-item resolver and fails before installing a projection when it is absent. The next bounded BuyGoods projection regression should exercise a missing item binding where its fixture permits.
