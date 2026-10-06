# P12-B TravelParty Start Operation Design Review

**Result:** PASS — bounded design is ready for implementation.
**Independent reviewer:** p12_matrix_review agent; the reviewer did not edit candidate files and did not run tests.
**Design branch:** codex/phase12/P12BTravelPartyStartOperationDesign
**Design commit:** 6709f00c190813858b6e20246e92fbaaab0864dc
**Design tree:** 3675044588dba2feefd7a0bebf4f8ed01ef26184
**Exact parent / P12 canonical base:** 29162cd0cf63e31f9542e612023a7256ac36ca4c

## Findings

The proposed boundary matches the existing TravelParty source order: preparation completes before TravelParty ID allocation, followed by member travel, charges, party insertion and association, event recording, and Knowledge discovery.

The exact owner set and revision headroom cover the TravelParty allocator, party store, each exact member's travel state, account and Knowledge, affected City presence, Event counter, and record sequence. The two Knowledge sections share one owner revision and must be notified together. Zero-cost charges do not change account revisions; positive-cost charges and possible refunds are included.

The selected-profile runtime wrapper and preparation callback admit the normal group-start path before ID allocation or owner writes. An unwrapped P12-bound TravelPartySystem start rejects before mutation without faulting a healthy runtime. This remains a P12-B prerequisite capability and makes no P12-B completion or readiness claim.

## Retained limitation

The two direct Expedition callers remain outside this design. Expedition start or return may commit Expedition ID/store changes before calling TravelParty start; this design neither invalidates nor rolls back those P12-F writes. Do not treat this review as validating Expedition behavior or P12-B readiness.

## Authority references

Architecture: ffd75652d89d862b83d634868c560f8540869b89.
Intraday/extensibility alignment: 4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194.
Multi-participant activity alignment: c285466c355103d3637ac165246591b72eb7bda0.
