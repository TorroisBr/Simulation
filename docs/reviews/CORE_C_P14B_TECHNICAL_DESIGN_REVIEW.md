# CORE-C P14-B finite-source technical design review

**Disposition:** PASS for the exact documentation candidate below.
**Review date:** 2026-10-13.
**Reviewer:** Codex independent reviewer `actor_bridge_tip_review`; read-only review, separate from the candidate author.

## Reviewed refs and diff

- Candidate branch: `codex/core/CORE-C-technical-design`.
- Candidate commit: `30e260d7be19f1daa288ec3a5672b0957dabc874`.
- Candidate tree: `6b10c44100dbf4d4a05c2dc05d62f5ad1751d9a1`.
- Actual base / current P14 canonical: `09fcbe46ffb5a7d80377185692d550310debc110`.
- Architecture baseline: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
- P12 baseline: `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.

The remote branch was verified at the exact candidate commit and tree. The merge base equals P14 canonical, and the candidate adds only `docs/design/CORE_C_P14B_FINITE_SOURCE_TECHNICAL_DESIGN.md` (533 lines); its two commits contain no source or test changes. The review also checked current P14 Market/source code and tests, P14-A/B/C contracts and State, the CORE extraction architecture, and the CORE-A handoff.

## Findings

**Unity Market authority and P14-C boundary:** The design keeps `MarketRuntime` as the single live Unity Market owner. The portable operation accepts a detached ordered row snapshot and emits a temporary prepared value; Unity continues to resolve content, enforce P12 admission, own Market revisions and serialized state, install the existing `PreparedMarketState`, and notify. It does not introduce another live Market store or migrate general Market behavior. P14-C remains on its current Unity path: its contributor calculation, ordering, replay/receipt behavior, one aggregate prepared replacement, and revision semantics are explicitly unchanged. The existing P14-C suite is required as an integration regression; the design does not claim P14-C calls the new operation. This preserves the promoted contract and pins the relevant regression boundary.

**Row mapping and cardinality:** The mapping retains row order and count, distinguishes a null row from a non-null row whose `Item` is null, and preserves unrelated rows and their serialized values. Target matching uses each row's own `ItemData` definition ID and requires exactly one matching row; duplicate IDs remain separate and therefore reject. The target row retains its exact `ItemData` reference and `desiredAmount`; only stock and the recalculated price change. These rules match the current implementation and explicitly call for tests covering null variants, duplicates, order, references, and serialized-field preservation.

**Float and IEEE behavior:** The design preserves the current binary32 operation order and Unity overloads, including the `Mathf.Max`/`Mathf.Clamp` conditional behavior for NaN and signed zero; it excludes integer division, double intermediates, and algebraic reordering. It specifies exact-bit parity vectors for normal rounding, clamp boundaries, signed zeros, infinities, and NaN, and requires Unity results to be captured before extraction and the portable implementation to match them bit-for-bit. This matches the current `MarketItemRuntime.CalculatePrice` behavior. The relevant Unity reference implementation is [UnityCsReference `Mathf.cs`](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Math/Mathf.cs).

**Tests and scope:** The design maps existing P14-B behavior and tests to the extracted operation, requires the P14-C mixed-source suite and affected Market/runtime admission regressions to remain green, and separates design from implementation validation. It stays within the single-City, one finite source, one item P14-B profile. It does not add P14-C migration, population consumption, trade, money, transport, multiple-City support, save/load, P12 profile admission, or gameplay scope.

## Applicability and implementation gate

This PASS applies only to the exact documentation candidate and its bounded technical contract. It is not source implementation review, test evidence for CORE-C, canonical promotion, or Phase closure. No tests were run for this documentation-only review. Implementation remains dependent on CORE-A becoming canonical and on the applicable refreshed extraction baseline and implementation validation. The architecture DAG shows CORE-A as the prerequisite for CORE-C; CORE-B is not a prerequisite for this bounded checkpoint.
