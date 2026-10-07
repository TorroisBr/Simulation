# Phase 20 State — Multi-participant Activities v1

**Status:** PHASE 20 IN PROGRESS — P20-A and P20-B promoted; P20-C scope accepted with current-base design revalidation pending; broader Phase 20 work remains open.

**P20-A architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`. **P20-B/current architecture baseline:** `f6924e63d8e5731da1d33021d0361e7defe6dad7`, including the current alignment records.

**Canonical branch:** `codex/phase20/canonical`; P20-B was promoted at `142672b9eddd23013ff83b7b176979dd4cc9e3b6` from prior canonical `7a81cc0ecbc511dd36c248ec62c7b20f7e477f53`. A documentation-only State/evidence follow-up is recorded below.

## Checkpoint status

| Checkpoint | Status | Evidence / boundary |
|---|---|---|
| P20-A — Synthetic Multi-participant Operation | PROMOTED | Scope accepted on 2026-09-27. Feature implementation `22df7b3` integrated at `ee8502f` against P18 code tip `b75c5b8`; exact integration review passed. The P18 canonical advance to `b390262` was documentation-only and independently classified `UPSTREAM_IRRELEVANT` to P20 execution code/API. User approved promotion; `codex/phase20/canonical` was created and pushed at `1dcf67a`. |
| P20-B — Daily-profile census admission | PROMOTED | Selected `UnityBootstrap-Daily-v1` explicitly-empty census admission; implementation `de24dff` / tree `62f8f3f`; independent exact-tip review PASS; focused 8/8, ALL EditMode 2273/2273, Smoke 5/5, diff-check PASS. See the detailed P20-B section below. |
| P20-C — Two-Person Joint Civil Travel consumer | SCOPE ACCEPTED; REVALIDATION PENDING | User decision 2026-10-07; see the current-canonical revalidation and architecture handoff records. No separate P20-C promotion is claimed. |

Validation on the integration tree at `ee8502f` passed: P20SyntheticOperation 11/11 (`EditMode-20260927-200939-811b47b1f816416d8d48e058c7c2bb99.xml`), ActivityLifecycle 17/17 (`EditMode-20260927-200956-09fac7deaee24d019b9c1413293d8586.xml`), LogicalTimeline 35/35 (`EditMode-20260927-201010-753b0cc0d8e044ba80d6cceb74ffbf57.xml`), ALL EditMode 1805/1805 (`EditMode-20260927-201027-0ed8dcbfea1a4333bc55e6403c5bce72.xml`), and official complete Smoke 5/5 (`EditMode-20260927-201122-5f04cf6872f74aa29f845903331ad121.xml`). `git diff --check b75c5b8..ee8502f` passed. The XML results report zero failures, inconclusive tests, or skips.

## Scope and dependencies

P20-A proves a bounded synthetic shared operation with independent participant decisions, coordinated commitments/start, and participant-specific results. Exactly two Persons is a fixture detail, not a cardinality rule. `ActivityInstanceId`, definition identity, and each participant `PersonId` remain distinct.

P20 execution consumes the relevant promoted P18-A/B/C timeline, lifecycle, and availability/decision capabilities. It does not depend on P18-D legacy integration, P19, or phase-number order. Future travel, production, save/fork, or mod-defined shared activities require their own scoped consumer contracts and the relevant promoted capabilities.

Phase 20 remains open. No broader gameplay consumer, universal role catalog, persistent Group/Organization model, mod loader, save/replay system, or War/robbery/gang behavior is authorized by P20-A.

## P20-B — Daily-profile census admission — PROMOTED

Promoted to `codex/phase20/canonical` at `142672b9eddd23013ff83b7b176979dd4cc9e3b6` by clean fast-forward from `7a81cc0ecbc511dd36c248ec62c7b20f7e477f53`. The promoted implementation commit is `de24dff356a54a0a4037e16c0ca5dc9ca379bc18`, tree `62f8f3f3ad803e3f8eca832f7e39cff8196d5b85`; the candidate tip contains a docs/evidence-only child.

The selected `UnityBootstrap-Daily-v1` census registers P20 joint civil travel as an explicitly empty owner section. An absent or empty P20 owner is admitted; nonempty P20 travel state fails closed during daily census/runtime initialization. Focused tests cover both paths.

Independent exact-tip implementation review PASS is recorded in `docs/design/PHASE20_P20B_IMPLEMENTATION_REVIEW.md`. Validation on the unchanged implementation tree: focused integration 8/8, ALL EditMode 2273/2273, official Smoke 5/5, and `git diff --check` PASS. The six XML/log artifacts are in `docs/validation/P20B/P20B-validation-20261004.zip`, SHA-256 `8316130BF87ACDA926822C6E4060B1CC9E83B37AADE5854EF3A960EA6C7BA56A`.

This checkpoint adds only the bounded Daily-profile admission/census seam. It does not add save/load, Party/Group semantics, or broader P12 census/capture/export/hydration readiness. P20-B is a promoted checkpoint; Phase 20 remains open.

## P20-C identity and current-canonical revalidation — 2026-10-07

The user resolved the checkpoint identity conflict: retain P20-A as the synthetic multi-participant operation; retain P20-B as the promoted Daily-v1 empty-owner census/admission checkpoint; assign the bounded Two-Person Joint Civil Travel consumer to P20-C. A search of current P20 canonical content and retained checkpoint history found no earlier P20-C reservation.

The canonical P20-B promotion and its State/review records remain unchanged. The existing P20-B section above remains the complete P20-B scope statement. The joint-travel source commits are already ancestors of the P20-B promotion history, and current P20 canonical Assets tree 8b579d9f61ad3145b535b27cdd71a37d512a32b1 matches the reviewed de24dff Assets tree. Preserve those commits and code; do not rewrite history, remove the consumer, or infer that the P20-B checkpoint has been broadened. The separate P20-C checkpoint requires its own current-base design/code review and status record.

The historical P20-B joint-travel technical design at 8afc463fb71112a0c7b8902e7e5673aee9e31bd9 and independent design review remain available for reuse. Master revalidation against Architecture e167960, P8 canonical 470667d, P18 canonical 8ac2d78, P20 canonical fe4909a, and P12 canonical 94551b0 found its semantic boundaries compatible: two distinct Persons in the proving fixture only; P18 remains the timeline/lifecycle/commitment authority; P8 remains owner of each Person's travel truth; P20-B/P12 Daily-v1 remains explicitly empty and rejects populated P20 travel state. This is a preliminary compatibility assessment, not an independent current-base review or implementation-readiness verdict.

P20-C consumes promoted P18-A/B/C, P20-A, and P8-E; it must preserve P20-B's Daily profile admission boundary. It does not require P18-D, P12-B completion, P19 loader work, save/load, or P13 fork.

**Status:** P20-C scope accepted; current-base independent design review and canonical Architecture Roadmap reconciliation pending. No P20-C promotion, code-tree change, Unity validation, P12 readiness, or Phase 20 closure is claimed. The General Architect handoff is durably published at `codex/architecture/P20B-P20CIdentityReconciliationHandoff` (commit `64c08ba`; draft PR #1). Canonical Roadmap reconciliation and independent current-base design review remain pending.
