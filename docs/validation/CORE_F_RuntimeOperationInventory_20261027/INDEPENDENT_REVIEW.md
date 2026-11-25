# Independent review — CORE-F Runtime Operation Inventory

**Verdict: NEEDS_CHANGES**
**Reviewed tip:** 8770bbb43353cf9106b5598f6c4c09b4f1bd7343
**Base:** 61395a1438e0bf760e600ae09779a83f12c8d626
**Core canonical at review:** a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
**Independent reviewer:** operation_inventory_review
**Review date:** 2026-10-27

## Scope and code verdict

The exact committed diff centralizes selected runtime operation inventory in the Core-owned DailyV1ExecutionBoundaryState; the shared Unity census protocol delegates to that state while standalone protocol instances retain local inventory and accounting.

Static code and test review passed. The catalogue accepts only the four complete supported operation catalogues and rejects duplicate, incomplete, or unknown operations. Shared quiescence and capture use the Core operation count. Tests add catalogue, rejection, active-operation capture, and operation scope coverage without weakening prior assertions. No unrelated ProjectSettings or user-owned files were committed.

The candidate was the exact remote branch tip, directly based on the declared base, with Core canonical as an ancestor. The committed delta passed git diff --check. The complete EditMode result package was independently checked: archive SHA-256 87de2754cdf36d21a597e2bcc07641626ac58b16ea4a252da54a25280b1d2a25, 3,271/3,271 passed, and no workstation paths in the retained report or package manifest.

## Findings

1. The Smoke manifest digest in the then-current VALIDATION.md was malformed (62 hexadecimal characters). It has been corrected to the SHA-256 calculated from the retained original manifest: 6BB83A7E1EE130E3E6C42D679ACA7954BDC75CDA96D55D3C6D2E1760D72773CF. This documentation correction does not change the original run or result.
2. The CoreValidation TRX and the separate filtered Smoke result were retained only in protected local evidence, not as permanent complete result packages. The full EditMode report includes the same five passing Smoke identities, but does not independently evidence the separate filtered invocation. The retained Smoke manifest was unavailable in the reviewer checkout at review time; it has since been recovered from the protected E: evidence store and its digest verified. The permanence gap remains.
3. Operation scope handles do not carry operation IDs, so out-of-order disposal is not detected as a handle-to-ID pairing violation. Aggregate active-operation accounting still prevents false quiescence/capture. This is recorded as a limitation; the reviewed contract does not establish strict per-handle LIFO identity pairing as a required guarantee.

## Disposition

The code review is positive, but the checkpoint is not VALIDATED_CANDIDATE and is not promotion-ready until the required complete result evidence for CoreValidation and the filtered Smoke invocation is permanently and independently accessible under the approved evidence policy. Do not infer P12 census completeness, the 89-family owner coverage, daily execution authority, or CORE-F completion from this inventory change.
