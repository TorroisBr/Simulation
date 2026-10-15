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

`DailyV1CapturedSelectedContentFixture.cs` is generated from the 718 independent
`member=` rows and the reviewed 164-row CSV, never from the expected P12 bytes.
The generator pins the baseline SHA, checks that its nested object/list walk
accounts for every captured field occurrence, and emits explicit C# constructor
inputs for the bounded P12 projection and typed P9 definition graph. The .NET
tests compare those values against all 184 retained P9 records, the P9 digest,
and the complete P12 catalog bytes. Regenerate with:
`py -c "import runpy; runpy.run_path('Tools/generate_daily_v1_captured_content_fixture.py', run_name='__main__')"`.

The test does not capture a P8 fingerprint; this fixture makes no P8 identity
claim. Runtime adapter inputs must continue to take the P8 fingerprint from the
existing Unity authority.
