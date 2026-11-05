# Independent exact-tip review — selected NPC runtime-state census remainder fix

Review date: 2026-11-05

## Reviewed identity

- Code commit: `958e7cc58faaf3dc7d0f371f938dfc4471ed6c20`
- Code tree: `18d61b8dd19ec636c8279c664b6589b377fc0ee1`
- Parent: `9359ed39056995e3d071312496759982d2bbb161`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Verdict

**PASS — VALIDATED_CANDIDATE.** No actionable implementation findings.

The independent reviewer confirmed that the remainder-count check now subtracts the exact runtime-state rows included in the Core-owned ID set. The helper validates each source row against the bound owner's identity, cardinality, and revision before filtering. Mixed rosters continue to return no Core runtime-state omissions, preserving the full Unity compatibility rows. The added initial-boundary capture assertion covers the failed startup census path without weakening existing post-advance assertions.

The reviewer verified the full EditMode and Smoke outcomes and the committed hashes of both changed files against the stable validation input snapshot. CoreValidation remains applicable because this candidate changes neither Core source nor Core tests.

This review accepts only the bounded selected NPC runtime-state census remainder correction. It does not declare CORE-F complete or authorize canonical promotion.