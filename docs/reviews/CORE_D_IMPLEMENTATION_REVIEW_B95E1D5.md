# CORE-D implementation candidate review — b95e1d5

## Verdict

NEEDS_CHANGES — one documentation-only validation-evidence digest mismatch. The implementation, bounded contract, owner checks, validation result packages, and exact source binding reviewed successfully. Do not promote this exact content tip until the two manifest digests in the validation record are corrected and a docs-only applicability review confirms the correction.

## Exact review identity

- Repository origin: https://github.com/TorroisBr/Simulation
- Current Core canonical at review: af084c2028528d8588268801daaee14a905f5f5b
- Candidate branch and exact tip: codex/core/CORE-D-implementation at b95e1d5979d86fa8408b0c8f824251e5c445b7a3
- Candidate root tree: 76422ac326fe57fdbd43e72bd0667d5da9d3f06c
- Candidate is a clean fast-forward from Core canonical; merge base is af084c2028528d8588268801daaee14a905f5f5b. At review, origin refs matched the local remote-tracking refs.
- Implementation commit: e83ea3ba7c9cbddf76280f20e12785ce82383e43; tree 33a610623a9443e9755874ae88bcc27ebdfedad3.
- Exact validation source/code commit: e53857e3f157def116136001e78af97e64ff50cb; tree cf63c3db6054f749a82c631ea85c7e216001cf04.
- Git confirms no changes to Assets, Packages, src, tests, or tools/Simulation.Lab between e83ea3b and e53857e. The b95e1d5 successor changes State and validation documentation/evidence only; it does not change the validated executable or test inputs.
- CORE-D design: docs/design/CORE_D_SHARED_RUNTIME_LAB_TECHNICAL_DESIGN.md, candidate design tip 786a9765bcc99f9268962d8130519d6d7a2290ae, based on the same Core canonical. Its implementation-readiness review is durably recorded at 60fd0c00acd940d98e68262dc4445a8340cdb69c. The design document in this candidate carries that exact PASS record.
- Architecture contract referenced by the design: 32358d2a0faf511e098c4f295a5132a4bfb8cfb2.
- P14 canonical at review: 09fcbe46ffb5a7d80377185692d550310debc110.

## Contract and source findings

The candidate implements the bounded P14-B finite-reserve operation fixture: one City/settlement identity, one finite source, one Item, and admitted absolute days. These are proving-fixture cardinalities only. The Lab path does not claim the complete P14-B daily economy and does not enter P12 Daily-v1.

The new Core coordinator calls the promoted FiniteSourceProductionKernel and MarketPriceCalculator rather than duplicating production or pricing rules. Shared fixed parity vectors drive the Lab and Unity tests. The implementation preserves source, settlement, Market, Item, content revision, LocationId, row ordering, revision and price-bit checks.

Unity keeps FiniteProductionSourceStore as source owner and MarketRuntime as stock owner. The adapter captures detached rows, verifies current Market revision and rows against the prepared basis, preserves the existing P12 mutation admission boundary, and maps the next Core snapshot back to existing Market rows. No second mutable production or Market owner was introduced.

The reachable Market surface was inspected: GetItem, GetAmount, GetPrice and GetPriceForSale read current state; AddStock, RemoveStockUpTo, BuyItem, SellItem, and UpdatePrices are the public mutation paths. Successful mutations advance the Market revision and pass through the existing P12 owner admission/commit callbacks. The finite-source preparation checks both owner revisions and the processed day before install; Install repeats those checks before mutating either owner. Consequently a second install of the same prepared transaction is rejected after the first install advances owner revisions. The Core facade itself is pure: applying against the same still-unchanged immutable input can calculate the same next value again, but that does not install or mutate it.

The design and candidate keep P14-C behavior separate. No full P14-B daily economy, population, P12 continuation/save-load, P13, P19, compatibility, intraday, or general-host capability is claimed.

## Validation and package inspection

No tests were run by this reviewer. The existing evidence is bound to exact validated source/code e53857e and tree cf63c3d, and the final candidate contains no executable or test-input changes. Candidate diff-check passed.

The validation record reports:
- .NET Core tests: 113/113 cold and warm.
- Standalone Lab locked restore/build and cold/warm exact-output parity.
- Unity finite-source focused suite: 41/41.
- Official Smoke: 5/5.
- ALL EditMode: 3,120/3,120.
- Unity input snapshot: 1,584 files, digest ed083356bd3d9aa5048ecff8a7d92eba97a8f46dff61fa78242b95456546a75b, bound to e53857e/cf63c3d.

The Unity result archive is 163,320 bytes and its SHA-256 matches the manifest value 85470f220d081cd2b8654f5ba7559855590e85f3018e00812a990d50f8182a5d. It contains one normalized XML result entry whose length and SHA-256 match the manifest. The .NET TRX archive is 48,045 bytes and its SHA-256 matches the manifest value 6a5d194649f719118c4b2289454bc94450f1aeff1beb8d6a2c32cfbd769739f2; both normalized TRX entries match their manifest lengths and hashes. Entry names are safe; archive content scans found no absolute local paths or credential-like values. The validation record states context-bound original comparison passed; archive-only verification is correctly described as not performing original comparison.

The candidate worktree also had unrelated user ProjectSettings edits and untracked .meta files. They were not staged or changed. The validation record says their input bytes were captured in the validation inventory; preserve those files and exclude them from any promotion staging.

## Required correction

In docs/validation/CORE_D_ScenarioLab/VALIDATION.md:

- Line 34 claims the tracked .NET manifest SHA-256 is b544bab96e5f7c8d9d1e8d0ade9342468172754e116275777acf8feb071e450d. The exact candidate Git blob and its checkout bytes hash to 4bba39e0d1d5d55709b29385cad657f595b89b8fe300f8a0c1dbf324852f33a4.
- Line 48 claims the tracked Unity manifest SHA-256 is f89f4ee11f06ddec8c8a1792883f284b08128270ef2f7309532b5a656e2bff39. The exact candidate Git blob and its checkout bytes hash to d4688415d2182fd4c92a1f1df666cf39bf784b9b821acd72c0a6b9aee7c0c426.

The embedded archive digests and archive-entry digests are correct; this finding is limited to the two prose manifest-digest values. Correct those references without changing implementation, tests, validation results, package files or unrelated user files. No tests need rerunning if the successor proves the executable/test/input trees and package bytes are unchanged; obtain an independent docs-only exact-tip applicability review before promotion.