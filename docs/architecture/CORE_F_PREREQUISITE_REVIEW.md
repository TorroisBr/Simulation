# CORE-F prerequisite designs — independent review and adoption record

**Date:** 2026-10-14. **Disposition:** PASS.
**Reviewed design content:** `edb04c1beb1447b90c365c5ff400d286aa0f1d2d`.
**Architecture base:** `7166ed088483c69d37300b4f12528ebb4b27be84`.
**Production base:** `8ea3b47e9ab1be9ab3fe6261a73dc33fece8cd8f`.
**Candidate:** `codex/architecture/core-f-executable-prerequisites`.
**Target:** `codex/architecture/world-identity-projection`.
**Author:** root General Architect. **Independent read-only reviewers:** Luna agents
`/root/f_effective_values_design`, `/root/f_owner_coverage_design`,
`/root/f_portability_design`. They provided prior source audits but did not author
these documents; cross-reviews covered the complete eight-file candidate as well
as each agent's original audit area. The root did not self-approve the design.

## Verdicts and exact reviewed scope

| Package | All three reviewers |
|---|---|
| WP1-Values | PASS / READY_FOR_IMPLEMENTATION after architecture promotion. |
| WP1-Owners | PASS / READY_FOR_IMPLEMENTATION as a cumulative test/evidence package. |
| WP2-Persistence | PASS / READY_FOR_IMPLEMENTATION after architecture promotion. |
| Full CORE-F host | WAIT_DEPENDENCY, intentionally unchanged; no .NET continuation result exists. |

No BLOCKER, MAJOR or MINOR findings were reported. The only nonblocking note was
to record these verdicts instead of leaving the review placeholder pending.
No routine human approval or unresolved product/architecture choice was identified.
The original user-confirmed parent host design is imported from the production
base with an adoption note; its old submission status is not treated as a new gate.

### Values checks

Reviewers checked the five-source BCL configuration closure, exact existing
constructor/caller surface, 164-field source map, typed-ID/value graph, copied
immutability, reciprocal City links, list ordering and existing error/default
behavior. The owner reviewer independently resolved all 164 source blob IDs
against 8ea3b47e with zero mismatches. P9 curated/effective identity and P12 raw
serialized-catalog identity remain distinct. Legacy type/container tokens, null/
empty/getter shape and scalar bits are explicit compatibility inputs; the .NET
fixture cannot substitute its own CLR type names. Shared canonical encoders plus
independent actual Unity vectors and field-by-field .NET comparison are required.
No asset, owner or general content loader is moved by implication.

### Owner evidence checks

Reviewers used current Git-object source rather than the architecture checkout's
older executable tree. They verified the quiescent vector/epoch seam, exact
P12ADailyV1Census key/role/schema/cardinality/alias checks, 89 families and
`61+22*N+U+P+4*C`, conditional 24-operation registration, dynamic membership,
existing Editor-only test access, and closed Unity P12 baseline. Notifications
increment per successful batch/call; nesting does not universally coalesce epochs.
Reservations/compensation/fault behavior is preserved per actual callsite.
The finite protocol fixture/source matrix covers supported roots, required-zero,
explicit-empty and existing synthetic-restore-only cases without new producers,
registry, epoch, token construction or arbitrary public-API support claims.

### Persistence checks

Reviewers verified the complete v1 descriptor dependency closure (strict wire,
limits, byte equality), narrow public data bridge, internal retained image/result/
owner/store surfaces, and constant/forwarding adapters. V2 uses the exact accepted
parent schema and mismatch taxonomy; current Unity negative-marker integration
is mandatory before staging. Real net8 runtime/assembly discovery remains later
host work. The shared atomic kernel preserves original predecode, stream options,
bounded reread, digest/validator order, replace/move, owned-temp cleanup and
absent/access/error categories. The three named existing storage tests, new fault
matrix and real default-filesystem SDK test constitute the bounded validation.
No new save format, profile admission, owner/image extraction or cross-host save
compatibility is implied.

## Documentation validation and final successor

Full architecture-base-to-candidate diff contains eight documentation/CSV paths
only. Reviewer diff checks passed; no runtime/test/config/assembly/asset/tool input
was changed and no new Unity/.NET execution result is claimed. Root independently
verified 164 unique source/line/blob rows, all 89 existing family/source rows and
39 authored asset blob identities against the current production ref. Existing
125-case Core/E delivery and Unity P12 results are retained historical facts,
not evidence of a .NET world host.

The successor records review/readiness and adds implementation-closure precision:
leave the optional BCL composition validator in place; pass stage-order/coordinate/
provider contract tokens as values instead of importing P10/spatial/census owners;
keep the existing P9 big-endian record-hash framing separate from P12 little-endian
wire; and preserve lazy getter call order. These clarify already required exact
source/parity behavior and do not alter product/profile scope. The final integration-order clarification explicitly records Persistence
before Values promotion for the shared framing API; Values preparation remains
parallel and no Core-to-Unity wire dependency or temporary codec is allowed.
Obtain independent exact-tip successor applicability before publication. Its containing commit is the
adoption marker, avoiding a self-referential SHA.

Final promotion must refresh remote refs, classify any canonical drift, preserve
reviewed content and safe fast-forward ancestry, pass diff/link/inventory checks,
verify documentation-only scope and leave Core/numbered-phase refs and active
implementation worktrees untouched. Push the intended architecture branch and
verify local/remote equality. Master imports only reviewed docs deltas into current
production, updates owning Core State there and uses normal implementation/evidence
review for each package. A design PASS is not production delivery or CORE-F closure.
