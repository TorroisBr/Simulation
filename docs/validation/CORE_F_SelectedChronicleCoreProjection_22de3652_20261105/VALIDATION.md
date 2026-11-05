# Selected chronicle Core projection validation

Candidate source: `22de365227a71e8327a2c1ff576ef4e7d4c3d993`
Committed tree: `687d9a91c294dbf4a4fa3f3d47a0877bc3e673ce`
Candidate parent: `efd1ae2b41acd54661eebf37a077e973e66a0bd3`

The Core formatter uses the selected Core owner graph and effective definitions for names and action labels. Restore composition and selected, site-free genesis use that formatter; the Unity formatter remains on legacy and site-bearing paths. The parity test compares the Core projection against Unity output for the same selected graph.

## Validation

- Official CoreValidation, run `8de984b15df546b79931c19888c0d818`: cold 1017/1017 and warm 1017/1017; source inputs stable; Unity Library absent from that .NET worktree.
- Focused Unity EditMode, run `d50bb1384d2a41d8a13a7e001d0c78ed`: 16/16 passed, input stable.
- Full Unity EditMode, run `74b5e756a39c438d8230465aab0c9069`: 3299/3299 passed, input stable. This report contains the focused and Smoke cases.
- Official Smoke filter, run `51a882c9fb294addba607bccce767fad`: 5/5 passed, input stable.
- `git diff --check`: passed.

All Unity run manifests identify the candidate commit and tree above. The packaged full EditMode report was verified by the official packager against its original private run manifest. The package contains normalized test XML; original Unity and CoreValidation reports/logs remain in their protected evidence stores and were not rewritten or included here.

## Published Unity evidence

- `Unity/coref-selected-chronicle-22de3652-all.manifest.json` — SHA-256 `B0F4A895F3F1C43BA9DD501931ED72EC2728765EA7F6D48DB952A9C1FAAD1627`.
- `Unity/coref-selected-chronicle-22de3652-all.zip` — SHA-256 `E87EAE5C70D1149EB7911BCC055AFC9357DBE6C8FDB4D2E6BB2E3B3EEFBD7716`.
- Official package verification: PASS, one source run and one normalized report entry.
