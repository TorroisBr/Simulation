# P12-B Runtime Admission and Quiescence Adapter — Independent Design Review

**Result: CHANGES REQUIRED**

**Reviewed design tip:** `675b6bbcc95be79666405ebff42de1f204d6c40d`

**Review base:** P12 canonical `19d0373d6a71b63536248ecc9091e66c9b3a708b`
**Reviewer:** independent Luna design review

## Scope checked

Reviewed the corrected runtime-quiescence design against the repository's
`AGENTS.md`, P12 State and brief, accepted profile/exclusions, P12-B blocker
map, capability decomposition, and the canonical `SimulationRuntime`,
`SimulationTime`, `TesteSimulacao`, `SimulationGenesisPipeline`, and bootstrap
composition implementations.

The correction resolves the prior review findings about P18 intraday claims,
the runtime construction point, post-publication bootstrap exceptions, and
direct `SimulationTime` writes. It also retains the promoted TravelParty
provider's exact installed owner, party-instance cardinality and revision, and
the selected profile's exact-zero assertion without adding it to the partial
continuation protocol. The Start-thread capture, validation-tail scope,
permanent failed-start latch, publication revocation, daily multi-day scope,
and stated limits on synchronization/capture eligibility are otherwise
bounded and feasible.

## Required correction

The design's proposed callback on a runtime-owned `SimulationTime` conflicts
with canonical P18 clock ownership unless the adapter is explicitly restricted
to the selected legacy daily profile.

On canonical P18 runtimes, `SimulationRuntime.P18D` binds
`SimulationTime.AbsoluteDay` as a projection of `SimulationTimeline`. Today,
calling `SimulationTime.TryAdvanceDay` on that clock returns
`TimelineProjectionOwnsClock`, and `AdvanceDay` throws. The runtime's
`TryAdvanceDay`, however, advances the timeline to its next day boundary via
`TryAdvanceP18DIntradayToCore`. The proposed unconditional runtime callback
would therefore change a direct P18 clock call from a rejected independent
clock mutation into a full P18 timeline advance. The proposed outer scope on
every `SimulationRuntime.TryAdvanceDay` would also account for P18 intraday
execution and handoff, which the accepted `UnityBootstrap-Daily-v1` profile
explicitly excludes and this design says it does not cover.

Revise the contract to state how applicability is identified and ensure that
both clock callback installation and outer operation admission apply only to
the P12 legacy daily profile. For a runtime whose clock is timeline-projected,
preserve the existing `TimelineProjectionOwnsClock` behavior for direct
`SimulationTime` calls and leave P18 advancement/admission/handoff semantics
outside this P12 adapter. Add validation cases for both sides: the selected
P12 profile routes direct clock calls through one completed daily advance;
the P18 projected-clock profile retains its existing direct-clock rejection
and does not gain this P12 scope or change its temporal behavior.

## Disposition

Do not implement the current callback/scope contract until this applicability
boundary is made explicit and re-reviewed. This is a compatibility correction
to preserve the already accepted P12 exclusion and canonical P18 clock
ownership; it does not request new product scope or a P18 dependency. P12-B
remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

---

**Result: PASS**

**Reviewed design tip:** `e5a32b8ec226204e751e1da41dfcf2546a760718`

**Review base:** P12 canonical `f538a096bf4b2558566518483bc60f0129718a3b`
**Reviewer:** independent Luna design review

## Revalidation and disposition — 2026-10-01

The refreshed design explicitly selects only `UnityBootstrap-Daily-v1`,
captures both the Unity `Start` thread reference and managed ID, and rejects
combination with a P18 timeline profile. The direct `SimulationTime` callback
and the outer daily operation scope apply only to that selected daily profile;
P18 timeline projection, direct-clock rejection, intraday execution, and
handoff remain under P18 ownership.

The proposal preserves the promoted owner-composition registration order and
the existing partial census baseline. It adds its fixed operation IDs before
sealing the inventory, then opens the bootstrap tail scope after the baseline.
That scope lasts through complete pipeline return and publication callbacks;
failure revokes publication and faults the protocol. Daily and multi-day
advances use the existing runtime advance lease and one registered outer
operation scope, with the runtime-owned direct clock routed through the same
entry point. Zero-day and rejected requests do not open a scope.

The design is bounded to admission and active-operation accounting for those
named synchronous paths. It does not assert full live owner/cardinality
coverage, shared committed-write invalidation, capture eligibility, P12-A
readiness, or P12-B completion. ScheduledDirective and Expedition witnesses
remain outside the partial continuation census as documented limits. The
proposal is compatible with current canonical code and preserves the approved
P18 boundary.

This PASS supersedes the prior disposition for design tip `675b6bb` only for
the refreshed proposal at `e5a32b8`. The accepted P12 prerequisite scope
authorizes its bounded implementation. This record does not review or approve
an implementation candidate and does not authorize canonical promotion.
