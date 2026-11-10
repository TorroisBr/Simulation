# CORE-F initial restore-start transaction validation

**Code candidate:** `0c4a4b9319a0bca63771516ea91923533ddb8320`
**Code tree:** `317e603627de244f038b5110401910efc87a2396`
**Base/candidate branch:** `49331cd7b131b8bc33a87d3f9a0a5f6077fae0dd` / `codex/core/CORE-F-restore-stage-transaction`
**Core canonical:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

This bounded candidate makes the initial start transaction release its private
restored-session cache and one-use proof after every `RestoreRequired` exit,
and clears Unity’s pending start adapter references when startup fails. It also
rejects a successful host projection callback that returns no projection.

## Validation

- **CoreValidation:** official run `1c96d57919a94dfea9c707b7565ff3dc`; cold and warm each **1088/1088 passed**, zero failed/skipped/inconclusive/not-executed. Complete source and generated dependency inputs were stable. Input digest: `c950fedb15bcae898c0e6c8307d4bda4d3729a6aec35cfa7f2df766d7ce9fe81`. Protected runner-manifest SHA-256: `ccd92f41906e5a4f102785979fd68d837a6ae0127d982124200251f7693c46ae`. Original cold/warm TRX SHA-256: `b2d3e15aa2300f3eb3edc94e38f2a9bab7c60bbfd69cd0421240d9c6a9b63fe7` / `8daf1197faad455e8425cc76848166cdec31e0d753bab3d75ce6379c377f5726`. This run included the default Lab gates. The checkout had a Unity `Library`, so this run makes no Library-independence claim; no detached .NET-host claim is made.
- **Focused admission:** `SimulationRuntimeAdmissionTests` reported 244/244 passed on the exact commit, but that first Unity import run marked its input snapshot unstable. It is not relied upon as a separate gate; the stable full EditMode run below covers its cases.
- **Full Unity EditMode:** run `f51576e104c045c8b18ea9173f6f2978`; **3303/3303 passed**, zero failed/skipped/inconclusive; complete input snapshot stable. Unity 6000.3.9f1 / Test Framework 1.6.0. The privacy-safe result package is `core-f-restore-start-0c4a4b9-editmode.zip` (SHA-256 `75dd107b6c38c48f41586c983b7a149b4d33bcb0388c94844a9c226111d7b79c`); package-manifest SHA-256 `d4d158430f6287e9a7c0c94a8331ef4a8329abeaa562cf3f35e3ec2744173111`. The official packager’s `Verify` compared the package to its protected original run and passed. The archived full report contains all five Smoke identities, each Passed.
- **Official Smoke:** EditMode name-filter run `2e731369c57f4308ad11345f15b3cb61`; **5/5 passed**, zero failed/skipped/inconclusive; input snapshot stable. Its original manifest SHA-256 is `c06f93da1f92f0bc4517db9a1e1342326b629921ee951b48835f40ca8bc87873`; original XML SHA-256 is `f8206e6b9ba75f90c08d07ad9ac382e77ef968e6f0c6b0fe0e6a0e0d37a3d24d`. The filtered original remains in protected local retention; the complete case results are also in the verified full EditMode archive.
- **`git diff --check`:** passed for the code candidate.

Original .NET and Unity reports/logs remain in protected local retention under
their run IDs. The public Unity package contains normalized result XML only;
no raw XML or logs are included. The .NET TRX originals are not published in
this candidate. They remain available for exact-tip review in protected local
retention; a future canonical promotion must use the approved package-bound
TRX workflow and complete evidence contract.

## Scope and limitations

The cleanup unit test verifies release of the matching image/session references
and proof invalidation, but seeds its private test state by reflection; it does
not construct a full P12-G restored session and fail the production
`RestoreRequired` callback end-to-end. Independent source review found no
production defect and noted this coverage limitation. This candidate does not
claim complete P12 restoration, detached host execution, `DailyV1Application.TryStart`
readiness, or CORE-F completion. It makes no daily-loop change, so LongRun was
not applicable to this startup transaction slice.

Exact-tip independent review of code and this evidence record is pending. No
Core canonical promotion is proposed.
