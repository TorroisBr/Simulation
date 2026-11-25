# Independent review finding — previous TravelParty-start candidate

Date: 2026-11-08
Disposition: NEEDS_CHANGES, resolved by the follow-up candidate

## Reviewed identity

- Base: `a644be627ff866b3f81564abcb62e7c625afa2ec`
- Candidate: `acd4b94f795ecec0d5824097ed4ec4a512f46310`
- Implementation: `fa0cedebdf46271cd3663f1694f51408aadd2e14`
- Implementation tree: `45f168ba888d0abf39e0e03aa9f7d1fd8e7673ad`

## Finding

The selected Core path rejected a valid compensation mutation. A TravelParty store addition advances cardinality by one and its revision by one. If a later start step fails, the existing rollback removes that party, advancing the revision again while returning cardinality to its original value. The Core receipt accepted only a cardinality increase of one, so it rejected the rollback removal and fault-closed the selected protocol.

The existing Unity event-store rejection test exercised the host fallback and did not cover the selected Core owner graph.

## Resolution

Candidate `aab04809c465fe110d9424c3f3500d70f86afc82` adds a selected-owner regression for the receipted add followed by compensating remove and limits this compensation to one removal that restores the initial store cardinality. See the follow-up validation record for source identity and evidence. This finding record remains historical; it does not itself attest that the follow-up candidate passed independent review.
