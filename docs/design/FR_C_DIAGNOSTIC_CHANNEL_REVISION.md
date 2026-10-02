# FR-C Diagnostic Channel Revision

## Status and bases

- Design state: proposed for independent exact-tip review.
- FR-C design authority: `codex/architecture/world-identity-projection` at `451340c56e9b676bf6ea43412bcb856b9ccde3de`; original FR-C review `96062630aed6e1fbb616a48d97290c331aed50c6` found no semantic blocker apart from the FR-B capability dependency.
- FR-B provisional implementation base: reviewed code commit `5c43733088bfbe860183f849d16295b542c1f465`, tree `d7d463960756e313bdc3800a20ecc976b659f2c5`.
- Phase 12 canonical at this revision's start: `1ac675cc558aa919a749167647c10506c11303fc`.
- This FR-C line is provisional and stacked on the FR-B candidate. It does not claim FR-B is canonical or FR-C integrated.

## Problem

The promoted FR-C design at `docs/design/FACTION_FACTUAL_READER_DESIGN.md` requires an invalid active affiliation endpoint to return `Unavailable` with a diagnostic. The promoted FR-B `FactReadResult<T>` models status and value, but has no diagnostic channel. `FactualReadCoordinator` also turns thrown reader failures into a generic unavailable capture and discards the exception context. A reader cannot currently satisfy the FR-C diagnostic requirement without adding an explicit, immutable channel.

## Decision: capture-scoped immutable diagnostics

Add a read-only `FactualReadCapture.Diagnostics` collection. Each immutable `FactualReadDiagnostic` identifies the capability and carries a stable diagnostic code plus a deterministic, non-authoritative message. Diagnostics explain an unavailable result; they are not facts, do not change `FactReadStatus`, and are not used as World Exchange data.

Keep `FactReadResult<T>` status semantics unchanged. An FR-C validation failure returns `Unavailable`; its typed reader outcome carries a diagnostic for the coordinator to attach to the failed capture. The coordinator still discards every copied factual value when any requested reader fails. It preserves the diagnostic alongside the all-unavailable result set.

Use an internal immutable reader-outcome wrapper containing the factual result and an optional diagnostic. This changes only the internal `IFactualReader` seam; it does not add callbacks or expose Store references. When an unexpected reader exception occurs, the coordinator returns `Unavailable` for the capture and records a stable generic code/message. It never publishes `Exception.Message`, stack data, or exception type as a factual result. If a reader returns `Unavailable` without a diagnostic, the coordinator attaches a generic `reader-unavailable` diagnostic for that capability.

Copy diagnostics into a read-only collection and sort ordinally by capability ID, code, and message before publishing the capture. Codes and messages for known FR-C validation failures are deterministic and do not include source IDs. A capture that fails before any reader runs may have an empty diagnostics collection; FR-C validation failures and unexpected reader exceptions always have an attached diagnostic.

This is an additive `FactualReadCapture` API extension with an internal reader-interface change. Existing status and `TryGet<T>` behavior stays intact. The design does not add generic logging, a mutable error sink, exception-as-domain-result behavior, or any World Exchange dependency.

## FR-C behavior under this revision

The faction reader returns the approved `simulation.faction-truth/v1` capability and two immutable copied collections: current Faction facts and active affiliation facts. If an active affiliation references a missing Person endpoint, it returns the entire capability as `Unavailable` with a deterministic diagnostic such as `faction.active-affiliation.person-endpoint-missing`. It does not omit the row, claim known-empty membership, or include the missing ID in the diagnostic message.

The reader remains inside `TryCaptureCoherent`; the coordinator's before/after logical boundary and FactionStore/PersonStore revisions remain the only coherence evidence. It retains no Store references in returned facts. Ended affiliations stay out of current membership, and a fully read empty Faction collection remains `Present([])`.

## Validation obligations

Add focused factual-read tests proving that diagnostics are immutable, sorted deterministically, tied to the right capability, and preserved when the coordinator discards all factual output. Verify that a reader exception produces only the stable generic diagnostic and does not publish exception text. Existing result-status and no-partial-capture tests must continue to pass.

The FR-C reader tests must also cover source-field fidelity; blank display names; known-empty Factions; active versus ended affiliations; same-cut Person endpoint verification; fail-closed invalid endpoints with diagnostics; duplicate/invalid affiliation rejection; deterministic ordering across insertion orders and locale; immutable copied records and collections; boundary/revision changes; and exclusion of actor Knowledge/support facts.

## Provisional implementation plan and boundaries

After independent design review, implement on a branch descended from this provisional FR-B base. Add the diagnostic value/outcome support to `Assets/_Project/Scripts/FactualRead/` and `FactualReadCapture`, add the Faction facts and reader in new files, and register the reader in `SimulationRuntime` only after FR-B's live runtime facade is canonical. The narrow runtime registration is the shared hotspot; keep it isolated and serialize it against any active P12/WI-A runtime integration.

Do not modify `FactionStore.cs` or `Person/PersonStore.cs`; their immutable record/read APIs, endpoint verification, revisions, and FR-B admission guards supply the needed boundary. Do not modify `SimulationBootstrapComposition.cs` or `TesteSimulacao.cs` for FR-C unless a concrete composition gap is found. Do not change any numbered Phase canonical ref or the reviewed FR-B candidate ref.
