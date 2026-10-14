# CORE-F WP1-Values Unity baseline fixture

`CORE_F_WP1_VALUES_BASELINE.txt` is the raw deterministic output of
`Assets/_Project/Tests/EditMode/Editor/CoreFDailyV1ValueBaselineTests.cs`, run
against the unmodified production source at `8ea3b47e9ab1be9ab3fe6261a73dc33fece8cd8f`.
The approved selected-member inventory is
`docs/architecture/CORE_F_SELECTED_VALUE_MEMBERS.csv` (164 rows; source base
`8ea3b47e9ab1be9ab3fe6261a73dc33fece8cd8f`).

The fixture carries ordered P9 records, complete P12 catalog bytes, every
selected source-member occurrence and the resolved effective/calendar snapshots.
It is evidence captured from Unity, not generated from the new DTOs. The capture
test is retained as the reproduction source. SHA-256:
`422805cbe373cf1410ee6a928d363fa1ef413dbcf50dc062dce7867ed4fdd1b3`.

The test does not capture a P8 fingerprint; this fixture makes no P8 identity
claim. Runtime adapter inputs must continue to take the P8 fingerprint from the
existing Unity authority.
