# CORE-F Core-Owned Selected Mutation Epoch Authority

Status: **validated candidate checkpoint; not promoted**.

## Identity and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Candidate implementation commit: `535e5a3158b08a63b68c4bf00d65212e9f082330` (tree: `43632273125d4329047b1c64d7c329726899baf5`).
- Candidate base: `b566a7f78f6e3495b3c4f8d582263aaa773dbc7b`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The selected Core execution boundary now publishes one shared mutation epoch after its host census protocol validates and refreshes all changed owner witnesses. This applies to selected Merchant, TravelParty Start, solo travel start, and TravelParty advance scopes.
- The Unity census protocol retains witness registration and baseline validation. Its existing `NotifyCommittedMutations` epoch-incrementing behavior remains for legacy/mixed-roster writers. No 89-family census migration or full Core-owned capture/restore claim is made.
- No `DailyV1Application.TryStart`, detached host, or full CORE-F completion is claimed.

## .NET CoreValidation

Official CoreValidation run `cc21099d71a843c1ac163f587d3a56e2` passed **1,119/1,119 cold and 1,119/1,119 warm**, with zero failed, skipped, inconclusive, or unexecuted cases. The stable input inventory contained 541 files and digest `af900847f1e495e4f6c0e918c77e3e657bfd79ccd0c06e333c8635efd436e819`; the recorded base was the candidate base above. All five changed Core source/test files are hash-identical to their validation input records.

The cold and warm TRX reports each contain 1,119 passed cases and have no failures, skips, or inconclusive cases. Their SHA-256 values are `d0d9fe2c1035c8da9429a37c300bf19840b678d4b708db68e4e501ab56a53aea` and `e81cb5a2d3c610dcf2ef4b1d8de58dfa741185775a94b9d346a4f13e2df63a2e` respectively (1,599,336 bytes each). Original TRX and logs remain in protected local retention; this report does not claim a public normalized .NET evidence package.

## Unity

Official Unity `6000.3.9f1`, Test Framework 1.6.0, PowerShell 7.6.5 / .NET 10.0.11:

- Full EditMode run `30542d3d1d584edab2961b5eda6fb14c`: **3,319/3,319 passed**, zero failed, skipped, or inconclusive; input stable. Its 1,982-file input manifest matches all eight changed Unity/Packages source/test files byte-for-byte. The full report's original SHA-256 is `d995dfd8711930225923de252e28385e26b9a8111b0393ea4451f9f128b3a18c` (3,303,954 bytes).
- The seven existing LongRun regression cases were present in this full run and passed.
- Filtered official Smoke run `5cfea1b1437244bb9d32e4ffa00396ae`: **5/5 passed**, zero failed, skipped, or inconclusive; input stable. The five exact Smoke identities also appear once each and passed in the full EditMode report.
- The full normalized package was produced and verified against its protected original using the approved packager: [manifest](Unity/coref-core-owned-epoch-authority-20261112.manifest.json), [result archive](Unity/coref-core-owned-epoch-authority-20261112.zip). Archive SHA-256: `edbe247ce94ac24f263aa34c90e5f296f01f0964d13917c087297baf696968b6` (175,970 bytes); manifest SHA-256: `e9c71995e2311bf7da632326337bb757b47ee4260179d195e02bf46ce4af7c36`.
- The filtered Smoke XML remains private and unchanged. The packager refused that filtered XML because Unity retains the unfiltered ancestor suite's `testcasecount`, which does not match the filtered descendants. The full report package contains all five Smoke cases and passes the archive verifier; no result XML was edited to bypass the check.
- An earlier PlayMode-filter attempt executed zero tests and is excluded from validation evidence. The supported EditMode `Smoke` filter above is the passing Smoke result.

## Review and limits

`git diff --check` passed. Public Unity package files were scanned for workstation paths and common credential-token prefixes; no matches were found. Protected ProjectSettings and pre-existing unrelated validation changes were not staged. Private originals remain unchanged.

This checkpoint transfers only selected operation epoch publication to the Core execution boundary. The owner census registry and compatibility writer paths remain host-side. Independent exact-tip review is the next gate; the candidate is not canonical and does not complete CORE-F.