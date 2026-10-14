# CORE-D technical design review — 2026-10-14

## Verdict

**PASS — implementation-ready within the reviewed bounded design.**

This is an independent review of the technical design only. It does not review
or validate an implementation, authorize a Phase promotion, or change numbered
Phase status.

## Exact references

- Design branch: `codex/core/CORE-D-shared-runtime-lab-design`
- Exact design candidate: `786a9765bcc99f9268962d8130519d6d7a2290ae`
- Candidate tree: `5efe8b2f0d4773ed5a19cc58b42177048fa04fee`
- Exact Core canonical base: `af084c2028528d8588268801daaee14a905f5f5b`
- Core base tree: `3312c44cbb2327a2ca423e39c5a4cce23acc1d79`
- Merge base: `af084c2028528d8588268801daaee14a905f5f5b`
- Current P14 canonical: `09fcbe46ffb5a7d80377185692d550310debc110`
- P14 tree inspected: `0644070d83269e1b052c212fbc0bc69be4145e8f`
- Architecture contract: `codex/architecture/simulation-core-extraction` at
  `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`
- CORE-A/B/C are promoted at the stated Core base. CORE-C's implementation is
  `114e3a7ec9f794b2996305eb1d640a8a58ab1083`; its exact-tip implementation
  review is `c04ec332eef29387463937c7153387d9b8728c75`. The CORE-C design PASS
  is recorded at `cb98d3e99c9491ce2c34278516272763333b271e`.

The exact candidate is a clean fast-forward from the stated base. The complete
base-to-candidate diff adds only
`docs/design/CORE_D_SHARED_RUNTIME_LAB_TECHNICAL_DESIGN.md`. The candidate
tree and all four canonical/architecture references above were checked
directly. `git diff --check af084c2 786a976` passed.

## Review scope and findings

Reviewed the complete design against the promoted Core extraction contract,
Core State, CORE-B and CORE-C boundaries, the P14-B implementation contract
and current P14 canonical State/code, repository `AGENTS.md`, and the
`checkpoint-technical-design` and `candidate-review` workflows.

The proposed scenario reuses CORE-C's finite-source kernel and Market price
calculator. It does not copy production or pricing rules into the Lab host.
The inspected P14-B path has one live `FiniteProductionSourceStore` and one
`MarketRuntime`; its prepared operation already checks source and Market
revisions, prepares the stock replacement, installs source and Market state
together, then invokes the existing Market/P12 notification path. The design
retains these Unity authorities and admission hooks. The Lab host has one
in-memory snapshot owner and no separate business-rule implementation.

The design preserves the P8 LocationId-to-City anchor and the P12
Daily-v1 pre-owner rejection. It keeps P14-C on its current Unity mixed-source
path and does not widen P14-B to population consumption or a full City daily
economy. The fixed .NET fixture is an in-memory Lab operation and makes no
P12 continuation claim. P13 history/fork, P19 extension APIs, compatibility,
intraday scheduling, persistence, and a general-purpose host remain excluded.

No unresolved product or architecture choice is needed to implement this
boundary. The single City/source/Item counts are explicitly fixture limits.
The proposed Core facade is a transition coordinator; it adds no second
mutable owner beside Unity's existing source and Market stores.

## Required implementation-start audit

Before implementing the adapter, enumerate every Market read and mutation
reachable in the selected P14-B proving profile. Confirm the same
`MarketRuntime` remains authoritative, all writes advance/check its existing
revision and pass its existing admission/notification hooks, and the prepared
source-plus-Market install remains serialized with respect to those writes.
If an exposed route bypasses those checks or cannot safely use the existing
owner, stop and record the exact call site before proceeding.

A prepared Core value is immutable; replaying it against the same old snapshot
can reproduce the same pure next value. Therefore “single-use” must be enforced
at installation by comparing against the current source and Market revisions.
The Unity adapter must reject a replay after the first successful install;
the Lab owner must likewise apply only against its current snapshot. This is an
implementation and regression-test obligation, not a second mutable Core
owner.

## Validation boundary

No Unity or .NET tests were run for this review. The candidate changes only a
technical-design document; no executable source, test source, project file,
or validation harness input changed. The implementation candidate must run
the design's required focused Core/Lab/Unity regressions, ALL EditMode,
official Smoke, applicable LongRun checks, CoreValidation cold/warm checks,
and `git diff --check`, all bound to its exact source tree. This review claims
none of those implementation results.

## Disposition

CORE-D is **READY_FOR_IMPLEMENTATION** within the exact reviewed design,
subject to the implementation-start Market operation audit above. This PASS
does not claim code delivery, test success, Core canonical promotion, P12
readiness, or Phase closure.
