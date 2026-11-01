# Independent Exact-Tip Review — Local BuyGoods Projection Recovery

## Verdict

PASS. No actionable implementation findings.

## Reviewed identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Exact candidate tip: 0dc961eda5d9f99db3b55b181379aec22fcf95b9
- Candidate tree: bd71c9fe37279b81bb7588a2ed144e6829238606
- Parent: 59931aafe7a9cfc10fbd6080259d472e5a26d5e3
- BuyGoods implementation commit: 59931aafe7a9cfc10fbd6080259d472e5a26d5e3
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Review result

The projectionless path is limited to the exact selected local open-market BuyGoods slot. It requires the payload City to equal the actor’s current City, a canonical item and positive amount, no target NPC, a non-traveling actor, and no active Merchant plan. The compatibility projection uses the exact selected action, City, and item-asset bindings and is installed without a P12 mutation notification. Core still owns the purchase transaction and mutations.

The regression verifies rejection for a missing item-asset binding and missing or duplicate City projections before RNG draws or owner writes. Rejection preserves action/account/inventory/market revisions and P12 census mutation epoch. Successful execution verifies exact payload/action/City/item identity, one deterministic draw, expected account/inventory/Market mutation directions, revisions, three mutation epochs, and final census. Existing projected, planned, and cross-City routes remain unchanged.

No assertions were weakened. Production and test file hashes match the stable validation inputs. The approved evidence package verifies, contains the complete EditMode result including the new test, and has no absolute Windows paths.

## Validation evidence

- Focused BuyGoods: 1/1 passed; run fcff142ea7e749efb6455933368c42b3.
- Full EditMode: 3291/3291 passed; run 60d6180af32e48c5baaa39d79d83fe6d.
- Official Smoke: 5/5 passed; run b2d165670b374175aa431e25e45f7aa4.
- Evidence package verification: VALID, one run and one normalized XML entry.
- Archive SHA-256: b94ba7b5d7ed938b3b76a8ac98687cb7303fa22c4f46115f41c2be60459490c5.

Validation details and source hashes are recorded in VALIDATION.md.
