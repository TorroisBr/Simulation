# P12-G Pending ActorChoice restore rejection validation

## Candidate and scope

- Canonical base: `104e437a9407fe61374abbd7d0ed88f5fd1c7bdd`.
- Implementation commit: `93dd4ddbe9434455c428039179fe7674b9a07ba5`.
- Git tree: `e5553bad988dbc43077c9d3065e5839e1ddc2a09`.
- Assets tree: `9f45fd146da5af7135774ef94dae6b2d7685a41d`.
- Changed implementation paths: P12-F detached snapshot staging seam, P12-G restore coordinator/test-only override forwarding, `TesteSimulacao`, and `SimulationRuntimeAdmissionTests`.

The test starts from a successful terminal SellGoods `AttemptReturned/Succeeded` on the authored Daily-v1 Pedro fixture. After the valid source F snapshot has been captured, a null-default test-only override changes only that detached ActorChoice value to a well-formed `Pending` input with the same input/command/Person/action/sequence/day identities, preserved snapshot schema/revision/next-sequence, and empty disposition/temporal history. The ordinary `ActorChoiceStore.TryCreateP12FStaged` path rejects it before `FActorChoicesStaged` and before publication.

The test verifies that the active source session, exact completed-boundary token, runtime health, current day, selected facts, continuation roots, and complete included owner projection remain unchanged. The source then advances in parity with an uninterrupted control, an unmodified valid retry succeeds, and a further completed boundary remains in authoritative owner/root parity. No source owner is mutated by the override. The default production path passes `null` and uses the captured snapshot unchanged; the only behavior change is test coverage of the existing P12-F rejection rule.

This case closes only the Pending ActorChoice owner-hydrator failure boundary. It does not complete P12-G, general no-replay, exhaustive owner/epoch coverage, capture eligibility, P12-A export/hydration, P13 readiness, or Phase 12.

## Validation

Unity Editor: `6000.3.9f1` (`7a9955a4f2fa`). Unity validation used `Tools/UnityValidation/Invoke-UnityValidation.ps1` on implementation commit `93dd4ddb` / Assets tree `9f45fd14`. Each XML reports zero failed, skipped, or inconclusive tests. The compressed log was decompressed and its raw SHA-256 recomputed.

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused Pending ActorChoice case | 1/1 PASS | [`XML`](results/focused/EditMode-20261011-012720-e451d0f56aae4968bd63ee7cce0d1029.xml) | `9C936E8E7B43EC4CC5B33BC9892B20CE4BB9428D2054257CE9E995DF2BBB2563` | [`log.gz`](results/focused/EditMode-20261011-012720-e451d0f56aae4968bd63ee7cce0d1029.log.gz) | `AB9092B00F627492221DAE157A2468E1F6E3F63066CB97A6A89A1C613F3FDF92` | `2629320BFB4874B8317494BE2BBF0B51458DD53FE277019CC1BB1F740BAC0052` |
| `SimulationRuntimeAdmissionTests` | 187/187 PASS | [`XML`](results/admission/EditMode-20261011-012746-3c758791796a44c6bdcf896a2c072ec6.xml) | `3658A5DAB9F4DA80F3C4CA8099BED5039329687A31E0ABCEE1FBB56FFAB99605` | [`log.gz`](results/admission/EditMode-20261011-012746-3c758791796a44c6bdcf896a2c072ec6.log.gz) | `AC6305CB66F1458B67C31983E6A75A560E94256F10505C3B429BB377E087F8FA` | `2A3630E1F1B8F41F42E8614B8708A857F067BD23809669623FF450AB432519DE` |
| `P12FActorChoiceSnapshotTests` | 7/7 PASS | [`XML`](results/p12f-actor-choice/EditMode-20261011-012827-06e75c5322b74449829b86ec85520367.xml) | `D8A5F1AE6E05BF0A89661B1473FC0FCB27C15C5EC4194441D8D6F2D2AC858AF0` | [`log.gz`](results/p12f-actor-choice/EditMode-20261011-012827-06e75c5322b74449829b86ec85520367.log.gz) | `49D9425A2B8FAD818965084C3D80FB8DF9843AA438B836F4B59E899E44655A62` | `66245BFDA77A927A142D162A23AC25127364C903BF66158642E4FCF63B2AD699` |
| `ActorChoiceRuntimeTests` | 11/11 PASS | [`XML`](results/actor-choice-runtime/EditMode-20261011-012840-cc4c4d0f46bc4c98a0caf19fc998b20c.xml) | `F8A44F4DA1E7D82B1B73E7F74C3C9E839FBAB8A6828F97669C577785F55CE7E6` | [`log.gz`](results/actor-choice-runtime/EditMode-20261011-012840-cc4c4d0f46bc4c98a0caf19fc998b20c.log.gz) | `D2A30ADEE66D4E577352A718CECA7EE2E1F25FF9701A686B982D7D6475ACA757` | `EFEC345068D9E4D1A81CF240D83681AE442224BCDA39191A7163FC00CB0F6313` |
| ALL EditMode | 2861/2861 PASS | [`XML`](results/all-editmode/EditMode-20261011-012855-df0a342684604bdd9c31ee95cd345ce5.xml) | `4D175091E397205B5C690A982BE6568D6A624850F0744C148ECC27BBAC91718F` | [`log.gz`](results/all-editmode/EditMode-20261011-012855-df0a342684604bdd9c31ee95cd345ce5.log.gz) | `103C67C83DF04928C968E807E314A3A971D6AA00C48BB65F07A13D8CD181CD90` | `544ADE9DF6B4D1C68CF088380DB230EB4DEBD77D6513F3415E2858F853788579` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | [`XML`](results/smoke/EditMode-20261011-013000-01b3790a619440e094b25854aa420ce7.xml) | `DE536840F0264BBC3586A4229E4DC0E9886624501B6BBBCCE790E8DAC156FA54` | [`log.gz`](results/smoke/EditMode-20261011-013000-01b3790a619440e094b25854aa420ce7.log.gz) | `68A822C93D624B4213474E05F0A39BE38069C01D6BA32BFF83084DE96A0D7C1E` | `E5027774F06E696D780C2293E4FBE8368C39D05F06FCB780AA64BF0C768B049E` |

`git diff --check 104e437a9407fe61374abbd7d0ed88f5fd1c7bdd..93dd4ddb` passed. No long-run validation was required because the change adds no daily-loop/domain behavior. Protected ProjectSettings changes, unrelated untracked `.meta` files, and previously retained diagnostics were not staged.
