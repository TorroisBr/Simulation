# Current owner inventory provenance

Architecture base: a20978d2b61e07a4b131afbb3971aae346511acb.
Execution source: b2c2c0e94c7a8dd9175a8db3fab4fb9d78dcfb2a.
Graph reconciliation input: fe1c5912ca31ee6bed6e22bd2694a8436000bf2a (docs-only candidate, not Core canonical).
No production or validation execution was performed by this architecture package.

The current syntax/member inventory parsed 322 application inputs plus 44 shared Core source links (366 source files), using SDK 9.0.315 Roslyn, CSharp9 and the preprocessor symbols retained in PostOwnerGraph/preprocessor-symbols.txt. This run enumerated declarations, assignments and invocations without semantic symbol binding. Exact current source blobs accompany the retained member/provider/root rows. Historical graph semantic compilation and its tool/metadata inputs are recorded separately in PostOwnerGraph/analysis-manifest.txt; that historical result is not a new compilation by this audit.

Retained counts checked by CSV parsing: 89 families (76 Required, 13 ExplicitlyEmpty), 89 resolved owner type/census witness mappings; 4783 owner/provider declaration rows (3867 owner, 916 provider); 169 Runtime public declarations (101 methods, 67 properties, one constructor); 15 concrete facade candidates. The unique source-path/blob pairs in the retained member/provider/root inventories were checked against git ls-tree of b2c2c0e (89 distinct paths/pairs, all matched). These are identity and enumeration checks, not admitted-writer proof.

The local broad syntax-name diagnostic contained 65315 candidates; it is deliberately not retained or used as completeness evidence. Its count grants no coverage. Closure follows admitted roots and exact owner identities, as specified in CORE_F_CURRENT_OWNER_CLOSURE.md.

OWNER_WRITER_MATRIX.csv retains the original current Core historical matrix bytes. PostOwnerGraph retains the reconciliation graph inputs with original provenance; two parent documentation links are anchored to immutable Core b2c2c0e because the copied graph has a new documentation location. Current closure README explicitly separates historical claims from current owner-body mapping.

Artifact hashes in ARTIFACT_SHA256.csv cover retained Git blob bytes for inventory/graph documents other than that hash manifest. Checkout newline expansion is not the hashed representation; verify cat-file/archive bytes. They bind this static analysis package, never substitute for original NUnit/TRX result evidence, exact source review or host execution.