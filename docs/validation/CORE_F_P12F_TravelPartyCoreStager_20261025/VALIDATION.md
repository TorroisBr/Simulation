# CORE-F P12-F TravelParty Core owner stager validation

This record covers the bounded P12-F cross-owner staging change. It is a candidate validation record, not canonical promotion, P12-F closure, or full CORE-F acceptance.

## Source identity

- Implementation commit: `abb2e3f53d4cd4dc4ae6d32b68a64b1684d177db`
- Implementation tree: `afd1785a4b0553edce145c43279982de3db91573`
- Parent/base: `d7aa82aca88a2beeecbfbabba165fe3916423772`
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`

The official CoreValidation input snapshot was stable (digest `04a1a9b525b4883f89375885573309fd6a8601990e3a19c85d92d6ce6ae4f51b`). Its recorded committed base was the parent above; direct SHA-256 comparisons confirmed every changed Core source/project/test file matched the candidate tree. The official Unity input snapshots were stable on the same parent. Every changed Unity source/test/metadata file present at test time matched the candidate tree; the deleted Unity stager and metadata were absent, matching the candidate.

## Validation

- Official CoreValidation run `975a398b31854324986c1bf9db8370ed`: cold build PASS; cold tests 798/798 PASS; warm tests 798/798 PASS; zero failures, errors, skipped, not-executed, aborted, or timed-out cases.
- Focused Unity P12-F EditMode run `8160e5cc96d24ac4a98a3628e911eaad`: 25/25 PASS.
- Full Unity EditMode run `236c4b7152184234a1af1530c398ae97`: 3264/3264 PASS.
- Official filtered Smoke run `b20fe5cfdb614969800f16bdcc0cedb0`: 5/5 PASS.
- `git diff --check`: PASS.

The first focused integration run on the earlier pre-correction source was 24/25: it exposed an incorrect assumption that all P12-D F rows must have a selected Core NPC owner. The corrected candidate permits additional compatibility F rows while still requiring unique paired rows for each selected Core owner and every TravelParty member. The final focused and full runs above passed on the corrected source.

## Unity result package

The official result-only package contains the normalized full EditMode result XML and no logs or source payload.

- Archive: [coref-p12f-travelparty-all-editmode.zip](coref-p12f-travelparty-all-editmode.zip)
- Archive SHA-256: `b0d1f41e4a92edd229479499475d3e7ee2ad86bd27597f5bf128dfdbb4a6fc44`
- Manifest: [coref-p12f-travelparty-all-editmode.manifest.json](coref-p12f-travelparty-all-editmode.manifest.json)
- Manifest SHA-256: `5197ba6fb62b37b884d6f4eab249889ecaf7f994dc5b29162ce636f86fee7f51`
- Normalized XML SHA-256: `8a283dffad7416ea1678e1033fbd8a0f62d1301b16f29333b0fbedcb8ca89aca`

The original Unity XML/logs and original CoreValidation TRX/logs remain unchanged in protected E: evidence storage. The CoreValidation manifest records the exact input digest and test counters. The current validation policy does not yet provide the approved public .NET TRX adapter/package; therefore these private originals and manifest are not presented as a permanent public .NET evidence package. This candidate is not ready for canonical promotion on that basis.

## Scope

The Core stager validates reciprocal TravelParty membership against selected Core NPC owners, detached P12-D F rows, and Core spatial topology before creating the staged store. Compatibility F rows without selected Core owners remain permitted. Unity projection validation remains an adapter concern.

This change does not complete P12-F or CORE-F, change P12-A/P12-B readiness, or claim detached-host acceptance. The subsequent docs-only record commit does not change the implementation tree or require rerunning the tests.
