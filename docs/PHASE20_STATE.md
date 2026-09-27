# Phase 20 State — Multi-participant Activities v1

**Status:** PHASE 20 IN PROGRESS — P20-A PROMOTED; broader Phase 20 work remains open.

**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`, including the intraday/extensibility and multi-participant alignment records.

**Canonical branch:** `codex/phase20/canonical` at `1dcf67a5a6d31d79fd7feb12d44bcb3da7a9b710`.

## Checkpoint status

| Checkpoint | Status | Evidence / boundary |
|---|---|---|
| P20-A — Synthetic Multi-participant Operation | PROMOTED | Scope accepted on 2026-09-27. Feature implementation `22df7b3` integrated at `ee8502f` against P18 code tip `b75c5b8`; exact integration review passed. The P18 canonical advance to `b390262` was documentation-only and independently classified `UPSTREAM_IRRELEVANT` to P20 execution code/API. User approved promotion; `codex/phase20/canonical` was created and pushed at `1dcf67a`. |

Validation on the integration tree at `ee8502f` passed: P20SyntheticOperation 11/11 (`EditMode-20260927-200939-811b47b1f816416d8d48e058c7c2bb99.xml`), ActivityLifecycle 17/17 (`EditMode-20260927-200956-09fac7deaee24d019b9c1413293d8586.xml`), LogicalTimeline 35/35 (`EditMode-20260927-201010-753b0cc0d8e044ba80d6cceb74ffbf57.xml`), ALL EditMode 1805/1805 (`EditMode-20260927-201027-0ed8dcbfea1a4333bc55e6403c5bce72.xml`), and official complete Smoke 5/5 (`EditMode-20260927-201122-5f04cf6872f74aa29f845903331ad121.xml`). `git diff --check b75c5b8..ee8502f` passed. The XML results report zero failures, inconclusive tests, or skips.

## Scope and dependencies

P20-A proves a bounded synthetic shared operation with independent participant decisions, coordinated commitments/start, and participant-specific results. Exactly two Persons is a fixture detail, not a cardinality rule. `ActivityInstanceId`, definition identity, and each participant `PersonId` remain distinct.

P20 execution consumes the relevant promoted P18-A/B/C timeline, lifecycle, and availability/decision capabilities. It does not depend on P18-D legacy integration, P19, or phase-number order. Future travel, production, save/fork, or mod-defined shared activities require their own scoped consumer contracts and the relevant promoted capabilities.

Phase 20 remains open. No broader gameplay consumer, universal role catalog, persistent Group/Organization model, mod loader, save/replay system, or War/robbery/gang behavior is authorized by P20-A.
