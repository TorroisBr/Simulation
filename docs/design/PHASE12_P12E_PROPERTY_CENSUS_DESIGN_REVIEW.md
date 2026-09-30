# P12-E Property Census Design Review

**Result:** PASS — independent exact-tip design review.

**Reviewed proposal:** `codex/phase12/P12EPropertyCensus` at
`b0bc9778d23008445a1a30296407447e536e648f`.

**Base:** implementation-reviewed Estate census candidate
`da611028c4fd9066fef740e2eda84838c7362cd2`.

The review confirmed that the installed `Runtime.PropertyOwnershipStore`
owns distinct current-ownership and transfer-history record sets. Successful
registration increments its local revision once; a successful transfer
atomically replaces current ownership and appends history, incrementing the
same revision once. Runtime composition rebuilds both sets in a fresh owner
whose revision matches the source and does not mutate that source. Rejected
registration, stale or invalid transfer, same-owner transfer, runtime fault,
and overflow paths preserve both cardinalities and revision.

The selected genesis authors no property rows, and a missing source store
composes to an exact empty installed store. The two views may use separate
section IDs while reporting the same exact owner identity. The design now
classifies both changing sections as `Required`; their initial zero values
are observations, not permanent empty classifications. Implementation
evidence will include a populated runtime clone that preserves ownership and
history cardinalities and exact revision without changing the source.

The accepted P12-B/P12-E capability authorization covers this passive
witness. The proposal introduces no new property or transfer behavior and
defers export/staged hydration, complete owner coverage, shared invalidation,
owner-thread/quiescence, capture eligibility, and restore. P12-B remains
incomplete; P12-A remains `WAIT_DEPENDENCY`.
