# Independent review — CORE-F P12-C Core runtime identity capture

## Verdict

**VALIDATED_CANDIDATE — PASS; no actionable implementation findings.** This is a bounded Core-side P12-C identity-capture integration review. It does not promote canonical or claim complete P12-C, detached-host acceptance, or CORE-F acceptance.

## Exact identities

- Repository: `https://github.com/TorroisBr/Simulation`
- Base: `ef6cae682638416c4bf7da7e3a52eac9ea075923`
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch/tip: `codex/core/CORE-F-core-owned-runtime-application` / `a23ed4ad03e332bcfd147bc42a8ef459155d3ad4`
- Implementation commit: `4e8307e6f4ad742969fad0edee0a885a30f6fc52`
- Implementation tree: `fcf5f07a5a0b54b760e305b45a43cc28857ee623`

The implementation delta is exactly five source/test files. The candidate tip adds only its validation record after the implementation commit. All five source-file SHA-256 values in that record match the implementation and the stable Unity input inventory; the two changed Core files also match the CoreValidation input inventory.

## Review findings

No actionable defects were found.

- The save operation selects `Session.OwnerGraph.RuntimeIdentityIndex` as the capture authority. For each admitted NPC, City, Location, and Route section, it requires exactly one census entry and checks the expected section ID/schema, `Required` role, exact owner-instance identity, cardinality, and shared revision. The snapshot uses that same index instance's registered values; the existing codec preserves section IDs/schema, alias groups, row identities, ordering, and revisions, and rejects unsupported identity families.
- Capture is restricted to the active continuation-save operation and its exact source witness. The operation caches the owner and snapshot, checks revision/census consistency, and revalidates before and after encoding. The application includes those Core-produced sections in the sealed image and rejects host-supplied `p12c.runtime-identities.*` sections. The restore coordinator no longer re-samples those identities from the Unity registry on the Core-operation path.
- The legacy no-Core-operation path remains: it still captures the identity snapshot through the Unity adapter and uses the original section encoder. Wire IDs, schema, and codec behavior are unchanged.
- The changed EditMode test adds direct assertions for the four section identities, exact census-owner binding, schema/role/cardinality/revision, encoded row IDs, and row revisions. The existing persisted-image continuation regression exercises save, detached load, and subsequent continuation. There is no isolated negative assertion for the new host-supplied-C-section diagnostic; the production boundary has an explicit fail-closed guard, and the full image composer continues to validate section uniqueness/manifest coverage. This is a non-blocking test-coverage limitation for this bounded code review.

## Validation evidence independently verified

- CoreValidation run `fc7a0e7fbd8b4ab2959ffcc0b4e1e5b9`: cold and warm **936/936**, zero failures/aborts/skips/inconclusive/not-executed. Private manifest SHA-256 `a1206678db462cbecc4b78bdb13503c368b933ef1ebbf69730db6bd3d7c06ed2`; stable working-input digest `12d38d26ba6489028990bc77717714cceeddc7036fb4d0a78728101a168d276d`. Both cold/warm TRX hashes match the record: `5df08e0ce2e7f8b46dd1ba60465c81a4290092698ec4ac32ca2bd6cb1553f7ad` / `a5750c138816f18cc367f17c225b005d30a7e1d80e6eae4fd921d10fed62752e`; each report totals 936/936.
- Unity EditMode `SimulationRuntimeAdmissionTests`, run `af0bed3d486540ea9c7cc66ae3ec9db4`: **228/228 PASS**, stable inputs at base `ef6cae682638416c4bf7da7e3a52eac9ea075923` / tree `7a8760441dc0efd509f59230fd39dcb52a66a148`. Private manifest SHA-256 `5e2e9691fa1340e2b82ddbaaabcd314bf4efde769ba8cba035eea816f2383f4d`; original XML/log SHA-256 `f5c583cf1cdf26f89646af4ae8a8ce83df0ce42324da59f1374cb65d994c8f00` / `9f0e030bc1951590782c840f35c3b2635615727ebe85aa26595941acbe1a508b`. The XML's 228 test cases all passed, including `PersistedDailyV1ImageRestoresWithoutUsingSourceSessionAndContinuesIdentically`.
- Unity EditMode `P12EDailyV1OwnerPackageTests`, run `bb1ed8fcb4864e4f9a3d9e940abfce13`: **6/6 PASS**, same stable input identity. Private manifest SHA-256 `fa245fd96f0fa4186bf26a50086f43eb325604a88265845f1291fe780d3cdbd3`; original XML/log SHA-256 `4b863f30a070dda0e0546d762374ca93067b57c6afb06c1d792712920248aae2` / `fb197b2863e92feebeba655774732de633790045ea125392d39e738f0c5d0aab`.
- The recorded private manifest hashes and original report hashes were recomputed; Unity XML counts and outcomes match manifests. `git diff --check` passes.

No tests were rerun for this review. The available evidence is focused/CoreValidation, not full Unity EditMode, Smoke, LongRun, or a full host acceptance gate.

## Scope and integration limits

Only the four selected baseline runtime-identity families are captured by this slice. The existing codec still rejects unsupported site/local-place/local-connection/notable-item identity values for this selected profile. P12-C's wider scope and other host/runtime requirements remain separate. No canonical promotion is claimed. Unrelated ProjectSettings edits, private manifests, and untracked evidence were not staged or modified.
