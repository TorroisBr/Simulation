# P12 solo-travel-start Core scope validation

This is a bounded CORE-F implementation checkpoint. Core now derives the
selected actor's exact P12 section cut, owns the admitted solo travel-start
scope, coalesces committed sections, and publishes those sections when the
scope closes. Unity uses that Core scope only for an admitted Core execution
with the exact selected actor owner. Legacy and mixed-roster actors without a
selected-owner binding retain the established host census operation. A
non-null mismatched selected-owner binding fails closed.

## Validation

The exact source change is based on candidate `5f2854c92cabb49e7b5909920049b4c0f67c0040`.
CoreValidation's stable working-tree input digest is
`f5ff5f01bbb6274e3ec5486711ed0d1f931dda54f1290cdbf7a14380ceacba80`.

- Official CoreValidation run `73ddbc209fdf42299673ade0a1570d8e`: cold and
  warm **978/978 PASS**; locked Lab gates passed. The five solo-travel Core
  operation tests are included in the explicit project compile list and the
  cold/warm case inventories. Protected originals remain in owner-only local
  retention. Core manifest SHA-256:
  `DE24315BDD332D69F5003A71EA2DAB24D4FC87FCCCE48AD51D72289B45672A17`.
- Official focused Unity EditMode run `4278826539924e1d8db5f1e2d7861e10`:
  solo-travel suite **11/11 PASS**.
- Official unfiltered Unity EditMode run `6459a73a85524837898eece3824e318f`:
  **3283/3283 PASS**, including all 11 solo-travel cases and all five Smoke
  cases; the source-input snapshot was stable. The verified normalized Unity
  package is `coref-solo-travel-editmode.zip`, SHA-256
  `37b87acd61e4ac3b235a6d30fba9afbf0e85d9ea97336d9ce989db77894a6ad7`.
- Official filtered Smoke run `cb881d0b380e45e48361d8cdb2477ff3`:
  **5/5 PASS**, zero failed, skipped, or inconclusive. XML SHA-256
  `10AE856B274749BA9CABC43B10F0EE5B72065B4D232B5845D19EF4EAA2D0CCE3`.
- `git diff --check`: PASS.

The CoreValidation runner emitted its private
`simulation-core-validation/v1` manifest, not the separate
`dotnet-trx-source-context-v1` adapter input. The approved public TRX packager
therefore rejected direct use of the runner manifest. No adapter context was
invented and no public .NET package is claimed. Original cold/warm TRX reports
remain unchanged in owner-only local retention; their SHA-256 values are
`CEDF9EE9DA09C4BCB4F347C34DA399B4C77498B5F0AE337E9F2F144D7957FB43` (cold)
and `DBCD8E5E16C3D167FB72000623BAB621D23D5E31266EAA78415CDAD3562906CA`
(warm). This run had a Unity `Library`; it does not claim a Library-independent
.NET environment.

The initial Unity compiler and Core test attempts are retained as failed
diagnostics and are not counted as passing evidence. The successful runs above
exercise the corrected source and registered Core tests. The empty 0-test
PlayMode filter result is excluded; the documented EditMode `Smoke` filter and
the full EditMode run provide the Smoke evidence.
