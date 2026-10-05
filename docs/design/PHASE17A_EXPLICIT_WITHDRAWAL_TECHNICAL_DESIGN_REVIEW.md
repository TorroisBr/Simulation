# Independent technical review — P17-A explicit withdrawal

**Verdict:** PASS on reviewed design content tip
`4088d3485f360144cc60ee301cc4ecb2550ef5bc` against promoted
architecture base `ffd75652d89d862b83d634868c560f8540869b89`.
The reviewer did not author or edit the design. This is technical-design
approval only, not implementation validation or canonical code promotion.

The independent reviewer read the full design, AGENTS/architecture/Roadmap/
Phase 17 Brief, relevant P7/P16/P12 contracts and owning code/tests. Initial
review found a MAJOR precision gap in the exact-state boundary: the export
had to include the entire P7 War record as well as its P17 section. It also
found a MINOR need to state P17-only composition admission. Both were fixed
and re-reviewed. A final MINOR note required the stable trusted authority ID
to be retained in initial P17-A composition state before any input; this was
also fixed and re-reviewed at the content tip above. No findings remain.

The reviewed contract distinguishes Faction strategic participation from P7
force bindings; retains one War authority and P16 as movement/supply owner;
derives goal status from one retained P16 receipt; records the accepted
external crossing origin with that receipt and the explicit concession in
the War terminal state; rejects P7 raw-end and profile/admission bypasses;
specifies atomic/stale behavior, full-record clone/export/staged validation,
P12 fail-closed rejection and a later Lab observation seam. The reviewer
found no durable semantic or technical choice delegated to implementation.
Concession and crossing occur on separate logical days in the bounded profile;
the design does not claim a global cross-owner ordering policy.

Documentation-only validation: `git diff --check` passed against the
architecture base. No code or Unity tests were run for this design candidate.
The Master must still recheck current implementation refs/hotspots, validate
the eventual code candidate and obtain its normal promotion approval.
