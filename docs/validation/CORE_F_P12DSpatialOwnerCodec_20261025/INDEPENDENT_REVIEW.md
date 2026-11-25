# Independent review — P12-D Core spatial owner codec

Review date: 2026-10-25

- Candidate commit: `38c1608176cdaee1af4c0197a6560444c60c2fe9`
- Candidate tree: `283769f38edbf856df1c8cd5995c4e0292813cad`
- Verdict: **PASS**

The review compared the exact candidate diff with parent `cb5173cf`. The P12-A codec logic is unchanged apart from replacing dependencies on the Unity census provider with the same section-ID and schema constants now owned by Core. The Unity provider aliases those Core constants, so persisted identities remain unchanged. Ordinal location sorting, route order, revisions, cardinality bounds, endpoint and identity validation, and failure-before-output behavior are preserved.

The review checked that the new detached snapshot types are internal, that Unity assembly access remains covered by the existing friend assembly declaration, and that the moved codec retains its `.meta` GUID. The new Core tests cover round-trip values, unresolved endpoint rejection, and revision mismatch; existing Unity root-codec tests cover route ordering. No actionable findings remain.

This review applies only to the bounded spatial owner codec extraction. It is not canonical promotion or CORE-F acceptance.
