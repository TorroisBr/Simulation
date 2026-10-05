# P17-A implementation review

**Result:** `VALIDATED_CANDIDATE`

**Reviewed implementation candidate:** `172ff15fcc7fbe28577b403bce2cf33eee5ac8a9`

**Exact code tree:** `d070c1dfa5c8c6b1429f04a2a4e19067212f1aed`

**Base and current required capability:** `codex/phase16/canonical` at `75a27d7ac97e66c2762835ccea7950a945c2f20d`

**Architecture:** `codex/architecture/world-identity-projection` at `ffd75652d89d862b83d634868c560f8540869b89`

**Design:** `4088d3485f360144cc60ee301cc4ecb2550ef5bc`, independently reviewed PASS.

An independent exact-tip code review passed. The reviewer inspected the full
candidate diff against P16 canonical and confirmed the bounded P17-A contract,
P7 War ownership, P16 movement/receipt provenance, deterministic state and
clone behavior, fail-closed runtime composition, P12 Daily-profile exclusion,
and the required validation evidence.

The review specifically verified:

- duplicate participant IDs, duplicate Factions/sides, and missing force
  bindings reject without advancing War state;
- a mutation fault blocks crossing and concession;
- stale concession revision/day rejects unchanged;
- accepted concession provenance survives the runtime WarStore clone;
- a Battle victory alone neither satisfies the withdrawal goal nor ends War;
- standalone P16 composition rejects P17-provenance movement state;
- P17 state rejects Standard and P12 `UnityBootstrap-Daily-v1` composition,
  both with and without its matching P16 owner.

No blocking defect was found. An additional combined unavailable-passage then
later-concession test was noted as optional; existing pending-goal concession
and P16 failure tests cover the behaviors independently.

## Exact-tree validation

All retained validation belongs to code tree
`d070c1dfa5c8c6b1429f04a2a4e19067212f1aed`:

| Suite | Result |
|---|---:|
| P17-A runtime | 10/10 |
| P16 movement regression | 21/21 |
| Runtime admission | 37/37 |
| ALL EditMode | 2335/2335 |
| Official Smoke | 5/5 |
| `git diff --check` | PASS |

Validation archives:

- `docs/validation/P17A/P17A-validation-review-fix-20261005.zip` — SHA-256
  `FB3871A7B1E16B41A76B5E850D937F5D59FB7E723C0C2B3E749E259B5C7EA80D`.
- `docs/validation/P17A/P17A-validation-20261005.zip` retains the earlier
  validation attempt; the review-fix archive is the final exact-tree evidence.

The independent reviewer confirmed the focused XML/log evidence and archive
hashes match the reviewed tree. No code or tests were changed during review.

## Scope and limits

This candidate implements only P17-A's explicit withdrawal demand and
explicit two-participant War concession. Goal achievement does not end War;
movement remains owned by P16; Battle result does not become War result.
It adds no territorial control, occupation, treaty, universal War winner,
general strategic AI, Campaign, or generalized diplomacy.

P17 state is excluded from Standard and P12 `UnityBootstrap-Daily-v1`
composition. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and
P13 remains blocked. This review does not claim full P12 continuation,
P17-A or Phase 17 closure, or later save/fork capability. Phase 17 remains
open after this bounded checkpoint.
