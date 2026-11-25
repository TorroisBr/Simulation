# P12-D Core-Bound Owner Section Encoding Validation

Status: **validated candidate checkpoint; independent exact-tip review pending**.

This record covers code candidate `7207f91e080763566496e02d4dde3572b0a8c8ef`, tree `bc2b181879bcb1b1b14a9064c00a4796feb34ec9`, based directly on candidate `c037cff3ad001a136127708703a9f15080c203ea`. The candidate branch is a clean fast-forward from that base. Core canonical is unchanged.

## Change

The active Core continuation-save operation now encodes P12-D sections from its operation-bound Core snapshots for City, SpatialNetwork, PersonStore, Genealogy, Person life/residence, and—when the selected Core NPC graph covers the full admitted NPC roster—NPC owners.

The existing Unity whole-roster path remains responsible for NPC rows when the Core NPC graph is incomplete for a mixed roster. It still captures every NPC row; the Core operation does not emit a partial NPC section set. P12-C runtime-identity and P12-E composition remain on their existing paths. P12-F continues using the prior Core-bound encoder.

The EditMode coverage compares each Core-produced section and row against the composed continuation image, including key, revision, and payload bytes. The mixed-roster test verifies that Core omits NPC sections when it cannot cover the full admitted roster.

This checkpoint does not close CORE-F or claim the detached host, full-suite validation, or overall application/restore acceptance.

## Candidate files

Five implementation/test files are included in the code commit:

- `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs`
- `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs`
- `Assets/_Project/Scripts/P12ADailyV1OwnerSectionEncoder.cs`
- `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

SHA-256 of the validated source inputs:

| File | SHA-256 |
|---|---|
| `DailyV1P12GCoreBoundOwnerSources.cs` | `c7d0ba838b899db8c5dd2752796ecc450de10dc05643899270028fb75f02c842` |
| `DailyV1P12GContinuationSourceWitness.cs` | `54c8502afbd0ce4f706620971379cf0579a56144ae76d83dd276a4ff2975b4bb` |
| `P12ADailyV1OwnerSectionEncoder.cs` | `8566e0ae4af2ba6cad8b2c951f3104244a8642993bd28243b2249a55cc49ff72` |
| `P12GDailyV1RestoreCoordinator.cs` | `40267c1814b7b601899dc08979ee9354c573dceef41c1416b4e3363ead5e1aed` |
| `SimulationRuntimeAdmissionTests.cs` | `7afebe136a26b6ae43cce50610023bbb2ec3c97414f00cc7d2e5afa5f11a09b5` |

## Validation

All results are from the official project runners. Original reports and logs remain in their protected local evidence store; this repository record contains identities and hashes, not copied reports.

- CoreValidation, .NET SDK 9.0.315: **936/936 cold and 936/936 warm**, all failures/skips/inconclusive counts zero. Input stability: PASS. Run ID: `033531cb7d054d40ac6806853ea6c257`. Protected manifest SHA-256: `73523c99f28240935aec8a072d9225d62fd0e2cfb93e638a27f0c776ede9eb7b`. Cold TRX SHA-256: `fa71dfde62dfc4524715ecc5bccd175269015eaa7985c3a71afdee2d8bb75142`. Warm TRX SHA-256: `5322c625c0008e40ecc30d9ba8156ac0bd751c0430118576dca7977583ceed48`. The runner reported that this checkout contains a Unity Library; no Library-independence claim is made.
- Unity EditMode, filter `SimulationRuntimeAdmissionTests`: **228/228**, zero failures, skips, or inconclusive results. Unity Editor 6000.3.9f1. Run ID: `3dd66955d63348fd89c0e5de75425368`. Input snapshot stable: PASS. Protected run manifest SHA-256: `4b0dd531313df0e3e9a18f1bb7f55c0e3a8de18fb7889e9ca9232de2e1db5d38`. XML SHA-256: `036773ff26178ee5131e958a05a00602cc782d35701cc98b479f33ee2082b8c5`. Log SHA-256: `dda28d5c45be4eb8c57625287875dcc125fe6aa77b6974492e8f1dcf899ba68d`.
- `git diff --check`: PASS.

The Unity input manifest was stable across the run. The three pre-existing local ProjectSettings edits were unchanged by validation and are excluded from the candidate commit.

## Remaining gates

This is not full Unity EditMode, Smoke, LongRun, or full CORE-F acceptance. Independent exact-tip review is pending. No Core canonical promotion has occurred.
