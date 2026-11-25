# CORE-F selected planned local market sale validation

**Status:** Candidate validation for the bounded selected active-plan local
SellGoods dispatch. The full CORE-F application/host package remains
incomplete and unpromoted.

## Exact source identity

- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`.
- Implementation commit: `2b0234347476e011fae873f600a8023a353c138a`.
- Implementation tree: `55e079809ce4c8d7773ff35335a56668662c9c00`.
- Parent: `1830af243db94834ba00f3e47eec69eb63c157d9`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Unity input snapshot SHA-256:
  `801795bee8c644d905aab6339a15e603a1b99cffe13d37611b23335e591d31d9`.

## Results

- Focused .NET selected-market and Solo Travel dispatcher tests: `15/15 PASS`
  on the implementation commit.
- `git diff --check`: `PASS`.
- Unity `6000.3.9f1`, unfiltered ALL EditMode: `3233/3233 PASS`, with zero
  failed, skipped, or inconclusive cases.
- `SimulationRuntimeAdmissionTests` within that exact EditMode result:
  `208/208 PASS`.
- All five official Smoke identities in the full result passed: `5/5`.
- Unity input and executable stability: `PASS`.
- Evidence package verification: `PASS` (`runs=1`, `entries=1`).

## Evidence artifacts

- Archive: `coref-planned-market-all-bcb27c99d8e048108756551594fe84f2.zip`.
- Archive SHA-256:
  `6c950c32b971ad6c8533d69a49ee1d804e70fa7451286c7d5f966a746538928c`.
- Manifest: `coref-planned-market-all-bcb27c99d8e048108756551594fe84f2.manifest.json`.
- Manifest SHA-256:
  `ab3544e03326a575319b14f10883886cb3b18b721da48ff7a6f9933e68053a10`.
- Private run ID: `bcb27c99d8e048108756551594fe84f2`.

Original XML and raw logs remain in protected local evidence storage. This
record publishes the normalized XML package and its integrity identities only.

## Scope and remaining gates

The tests cover local action dispatch, P12 plan admission, market commit,
post-commit plan transition, and refusal/order behavior. This evidence does not
claim full .NET cold/warm CoreValidation, detached restoration, a shared
DailyV1Application/host, process-separated T/T+1/T+2 continuation, Unity/.NET
parity, or complete CORE-F acceptance. Independent exact-tip review of the
market dispatch additions remains pending at the integrated package review
boundary. The earlier travel review is scoped to its separate `6f36ae24`
candidate only.
