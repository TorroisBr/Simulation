# CORE-B current-baseline revalidation

**Verdict: PASS — current-baseline design revalidation.** This is a read-only
technical revalidation, not an implementation review or a claim that required
builds/tests have passed.

## Exact refs

- Current `codex/core/canonical`: `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af`.
- CORE-B design: `d20c2352887184ccc964d7f9586407d00a727d32`.
- Original independent design review: `96a7df8c2ebad6cf5ce3bfe2bde140f98c6e46dc`.
- Promoted CORE-A integration in current canonical: `96135eb6a87e5217d169a56414a20d21e6649af6`.
- CORE-A exact-tip integration review: `3edc40af75eb4c442b55c398c9cf9309f474c1de`.
- Audited source baseline: P12 canonical `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
- Architecture baseline reviewed with the design: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.

## Revalidation

CORE-A is promoted, and the current Core calendar/date API matches the
CORE-B design's dependency assumptions: `SimulationCalendar` exposes the
absolute-day projection used by `LogicalTimeline`, and `SimulationDate` retains
the value semantics required by that projection. The current shared Core
assembly remains engine-free, and the SDK project retains its explicit
`netstandard2.1` / C# 9 compile list.

The audited RNG, timeline, spatial-key and P12 staging sources are unchanged
from the stated P12 source baseline. The existing P12 stager still calls the
internal random staged-restore API; the design's narrow visibility bridge for
that API and the raw snapshot constructor remains necessary. The timeline's
stable-key call sites and calendar/date dependencies remain compatible with the
design's proposed single shared encoder and complete source move.

No semantic drift was found. The design's exclusions still align with the
current architecture: no `SimulationRuntime` or owner migration, P12 schema
change, P18 consumer migration, P19 extension API, second scheduler/RNG, or
cross-build/host compatibility claim.

CORE-A promotion and the original design PASS satisfy the documented
architecture/design prerequisites. CORE-B may proceed to bounded
implementation, with compilation of the exact proposed Core source closure
against current canonical as the first implementation gate, before consumer
integration. This review ran no tests or builds and does not claim implementation
validation.

## Nonblocking clarification

Design §7's phrase “original per-case report bundle” could be read as requiring
public raw TRX. The current evidence contract separates complete privacy-safe
normalized public reports from immutable originals retained in protected
storage. Clarifying the phrase to name both evidence classes would remove that
ambiguity; the controlling policy already resolves it, so this is not a design
blocker.

The current `docs/CORE_STATE.md` still labels CORE-B current-baseline
revalidation as outstanding; its status should be reconciled with this record.