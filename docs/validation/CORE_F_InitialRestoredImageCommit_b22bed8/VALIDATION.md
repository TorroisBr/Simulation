# CORE-F initial restored-image application commit

## Scope and identity

This is an intermediate CORE-F candidate checkpoint. It moves initial
P12 image boundary finalization into the application's exact session-publication
gate. The private Core session retains a one-use proof bound to the admitted
image and validated owner vector. At commit, Core binds the mutation guard,
recaptures and matches the complete target vector and epoch, finalizes the
restored boundary, then publishes the Core session and host projection under
the application gate. Missing projections and invalid admissions fail before
finalization. Replacement restore behavior is unchanged.

The test changes cover the delayed commit boundary, no capture token before
commit, late target-roster mutation rejection at commit, and valid initial
restore/continuation after commit. The update preserves the rejection and
parity assertions; it does not claim full CORE-F acceptance.

- Branch: `codex/core/CORE-F-try-start-transaction`
- Base: `4622e71841e74f6e2f995927cf4cc80bdc9d3f2e`
- Core canonical during validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Implementation commit: `b22bed8521bf97b0b21f7354c91dd90ec5624e5c`
- Implementation tree: `5e09ff2a0e003581c1ce5d639ba94b066db691a5`

The validated four-file source/test identity is:

| File | Bytes | SHA-256 |
|---|---:|---|
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | 40,490 | `8c2592e5701d2dd8ec4bfa7dacaa1472f0f3dabaa5ceff03389bf1250006487a` |
| `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` | 55,112 | `c3b9ed8c24d33ad1174852a053f198a767a20763405750c6be84d2d63f92730a` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | 185,756 | `8b7e3cb306d7006cdf170b24e74e6f589dceec465c8e72db6e1528f7f0012bce` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ApplicationSessionGate.cs` | 34,611 | `140a6da479c0816e0483f2b216c963d4edce1f70fef284ce970ad0ce18cb382e` |

## Validation

- Official CoreValidation, run `87900d26e6cc470d816e17c3bc906c43`: cold and
  warm **1,010/1,010 PASS**, with cold build, locked restores, demos and Lab
  gates passing. SDK `9.0.315`; input digest
  `30f8cc7ac5fde614780401d3460b4533ea0895e851267fcaf4e292101e0426b1`,
  inputs stable. Private runner-manifest SHA-256:
  `04b9e96777d4dd6dd5bf5bcb364f651a885db2575e9875d7ef14ed4171985ce2`.
  Original cold TRX: 1,409,911 bytes,
  `153c5c775c3487d352da38e3990f3a2745250bbdda4f829ddaebaac0900848a8`.
  Original warm TRX: 1,409,911 bytes,
  `0156f2c54c58ec5246799a414ea1750a70e8a3e281fe5a213567d68c6adeb6be`.
- Official Unity EditMode P12G restore fixture, run
  `dd11aa19399c4606b9c97b1d309ada93`, filter
  `P12GPoliticalBindingRestoreTests`: **16/16 PASS**, zero failures or skips,
  exit code 0. Unity `6000.3.9f1`, Test Framework `1.4.6`; input digest
  `6fef56b0783797785f048310ab935be3bf24627b207e183de5716bd43ec71f7c`,
  `inputStable=true`. Private run-manifest SHA-256:
  `96217c99c416be26b969d6332f518c0ecb1d345a890c647a3c02ae7843b932c9`.
  Original result XML: 40,017 bytes,
  `ae8de03b124faaec44d4d9784216a7a5b25a855e69c38d3340a3264d7af30d3a`.
  Original runner log: 8,026,459 bytes,
  `8302e208c8f4414bd50cc68e68e44e6ed118589a72436884d2031131a4ba0c99`.
- `git diff --check` and the staged-diff check passed.

The CoreValidation and Unity originals/manifests/logs remain unchanged in the
protected local evidence store for independent review. Raw reports and logs are
not included in this commit. The three pre-existing user-owned
`ProjectSettings` edits were present in the input snapshots, were not changed,
and were not committed. The Unity runner reported a stable input snapshot.

## Superseded attempts and dispositions

- CoreValidation run `bc0cadf14f9f4c099d545061a29ff5c1` resolved its relative
  project path from the wrong current directory; it stopped before restore.
- CoreValidation run `0852f8d8bb8d4f2eb3436dbc164753fb` found missing collection
  imports; run `2b9ffc66a25e4a55bda94d4e0bd660c3` found incorrect named
  parameter labels. Both source defects were corrected before the passing run.
- Unity run `8eb153ff902e469bb789681b2bc1d691` selected zero tests because the
  filter named a file rather than the declared fixture class; it is not counted.
- Correctly filtered Unity run `4a4bd39ae9d54e9983d8c9a2277a157d` executed one
  test and failed: after the old fixture tried to advance the private candidate
  before commit, the commit assertion found it no longer at the required exact
  unadmitted boundary. The test now checks token absence by read-only inspection
  instead.
- Unity run `dc03488c8ef242b1bd2500b3db7b0b4b` executed 16 cases with 14 passes
  and 2 stale expectation failures. One expected a late owner mutation to be
  rejected while creating the private candidate; the transaction now detects it
  at the application commit gate. The other encoded a valid initial restore
  before committing it. Both assertions were moved to the correct boundary; the
  mutation rejection and byte-parity checks remain. These intermediate reports
  remain protected; the final 16-case run above is the only passing evidence for
  the final test source.

This checkpoint does not claim full EditMode, official Smoke, LongRun, complete
CORE-F validation, or Core-canonical promotion. Core canonical remains
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
