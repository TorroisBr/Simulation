# CORE-F selected local market action dispatch validation

**Status:** Candidate validation for the bounded selected local
BuyGoods/SellGoods dispatch in Core. The complete CORE-F package remains
incomplete and unpromoted.

## Exact source identity

- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`.
- Implementation commit: `93871f9ec472f666b6ccbac7f248fcf0f4aaf0c8`.
- Implementation tree: `df93c926cddc7e45fbc15c2aa7837293889f7ed3`.
- Parent: `a4a1060b2bbc304a9bb2e4fcf70e6d96e2e45000`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Unity input snapshot SHA-256: `39f9d37027c35f779f97a8307e3882aefd4e4a3edfc6645402e6b3b1617857f5`.

The Unity run records this exact repository HEAD/tree, the installed Editor,
and stable before/after input snapshots. The package contains the normalized
result XML; original XML and raw log remain retained in protected local
evidence storage.

## Results

- Focused .NET Core dispatcher and selected travel regressions: `14/14 PASS`
  on the implementation tree. `git diff --check`: `PASS`.
- Unity `6000.3.9f1`, unfiltered ALL EditMode: `3233/3233 PASS`, zero failed,
  skipped, or inconclusive cases.
- Within that exact full EditMode run, `SimulationRuntimeAdmissionTests`:
  `208/208 PASS`.
- The five official Smoke identities are present in the full result and all
  passed: `5/5`.
- Input snapshot stable and Unity executable stable: `PASS`.
- Evidence package verification: `PASS` (`runs=1`, `entries=1`).

## Evidence artifacts

- Archive: `coref-open-market-all-e77ca73568744f96a0575616fbf10e9a.zip`.
- Archive SHA-256:
  `a80d5152024fe326226484fe668ed526ab2ada5acb1235d31a6020450a62bd22`.
- Manifest: `coref-open-market-all-e77ca73568744f96a0575616fbf10e9a.manifest.json`.
- Manifest SHA-256:
  `47987031dd0223f043bda24d114852b33537b8b6c85add2ca63c7ef7d3ecbc60`.
- Private run ID: `e77ca73568744f96a0575616fbf10e9a`.

## Scope and remaining gates

The results cover the action-slot roll followed by the existing Core local
market transaction and P12 admission callbacks. They do not establish full
.NET cold/warm validation, detached restoration, the shared DailyV1Application
or host, T/T+1/T+2 process continuation, Unity/.NET parity, or complete CORE-F
acceptance. Independent exact-tip review of this market-dispatch slice remains
pending at the package integration review boundary. The prior independent
review of commit `6f36ae24` applies only to its selected Solo Travel slice.
