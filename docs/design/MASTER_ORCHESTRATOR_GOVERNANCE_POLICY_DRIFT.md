# Bounded-promotion policy wording drift

**Status:** Documentation/governance handoff for Architecture General.
**Observed:** Current repository-level `AGENTS.md` and `docs/EXECUTION_MODEL.md` authorize autonomous promotion of a bounded numbered-phase checkpoint when all named review, validation, ancestry, scope, and safety conditions pass. Some older Phase/technical-handoff text still says routine canonical promotion requires separate human approval.
**Applied in this run:** The current repository policy and the user's standing authority were followed for P20-B and P10-B after exact preflight. Both promotions were additive fast-forwards with unchanged reviewed code trees and recorded review/validation evidence.
**Requested owner action:** Architecture General should reconcile stale approval wording across Phase briefs, handoffs, and reusable workflow documents with the current canonical promotion policy. This note records the drift; it does not edit the architecture baseline, reassign architecture ownership, or change checkpoint semantics.

## Evidence

- Current architecture baseline: `f6924e63d8e5731da1d33021d0361e7defe6dad7`.
- P10-B handoff: `cfaccfa4b0dbd051d66aa29543e18b3661f109fb` contained prior routine-promotion gate wording.
- P20-B handoff: `d80ec06f48500a0ee80d6e05136ad50a6978a670` was also prepared under earlier promotion wording.
- Current `AGENTS.md` and `docs/EXECUTION_MODEL.md` contain the autonomous bounded-promotion conditions.

No gameplay/product or architecture decision is requested by this note.
