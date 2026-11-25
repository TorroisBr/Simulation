# CORE-F TravelParty Event Identity P12 Receipts

Candidate `acad0d957442c6d1c1274e0b1f9e2d7bdfc05153` (tree `a7ae8e89cfe70b2d1fe31206612866aa47be1615`), based on candidate `211ba17a4b76a1e2910acbfacd883f52d1181034`. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

This bounded checkpoint extends the Core-owned TravelParty P12 receipt baseline to the selected RuntimeIdAllocator event counter and SimulationRecordSequence. The census providers bind to the exact allocator/sequence objects selected by the admitted runtime. An individual committed receipt must advance its one-row counter by exactly one. Unreceipted drift and receipts hiding multiple increments fault closed. The whole host owner-vector preflight and notification protocol remain in place for compatibility and non-Core rows.

## Validation

- Official CoreValidation run `5c6e26177a374257974384c5fad38116`: **1,045/1,045 cold and 1,045/1,045 warm passed**, zero failures. The run occurred immediately before commit; its compile-input SHA-256 values for both changed files were compared with the committed files and matched exactly. Cold TRX SHA-256: `c402b7c9ddaefc909403e3ccaefb884de41747f4655ce2d50699ed115fea7543`; warm TRX SHA-256: `dbbc070d8ab66aad47fe80e2786a0531a6c5629edd550bd2e04f0d2b45e06814`.
- Official Unity EditMode run `c42e43af7d16487e9f7bce4c484a7c3e`: **3,300/3,300 passed**, zero failed, skipped, or inconclusive. It ran against the exact candidate commit/tree on Unity `6000.3.9f1` and PowerShell Core `7.6.5` / .NET `10.0.11`. The stable input snapshot SHA-256 is `e28253a9135dcf1e99fd9280033eab66407e5780bc5a044d4499ec1fff7564c4`. Original XML SHA-256: `6e750d3584c1a7bb425624cd584e6514a73fe31c36cf7345b7dc345505dab565`.
- Official filtered Smoke run `ad9a93ded2834a40a79c9dfc5a74ca5a`: **5/5 passed**, zero failed, skipped, or inconclusive, against the same exact commit/tree and stable input snapshot. All five Smoke identities were found as passed in the complete EditMode XML. Original filtered XML SHA-256: `0a9ae68e9914d3ee51a025f3892194534f87bee135f547924d1c1a787d907580`.
- The complete EditMode XML was normalized and packaged with the repository evidence tool. Package verification passed (`runs=1`, `entries=1`). Archive: `coref-travelparty-identity-receipts-editmode-acad0d95.zip`, SHA-256 `6e3c974c70b03cb82ff409947bd22bcb651798df8fc07159d40c085d7438958d`. Companion manifest SHA-256: `82a612efd0d336f903b3b4dd750ea18cdda335e5e673d7aaf6af47d1447f30ed`.
- `git diff --check`: PASS. The unrelated pre-existing `ProjectSettings/ProjectSettings.asset` edit was not staged; input snapshots were stable. No unrelated or private evidence files were included in the candidate commit.

Original XML, raw logs, and private run manifests remain in the protected local evidence store. The filtered Smoke report is private and is not published as a second aggregate package.

## Evidence limits

This validates only the bounded TravelParty Core receipt checks for the event counter and record sequence. It does not claim completion of P12, the shared Daily-v1 runtime, detached hosting, or CORE-F.
