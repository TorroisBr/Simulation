# P12-B RuntimeIdentityRegistry census implementation candidate

**Status:** Implementation complete; awaiting exact-tip independent review.

**Canonical base:** `codex/phase12/canonical` at `1ada62b031e738e2bdd5d3d623e028a114961d6e`.

**Reviewed design:** `docs/design/PHASE12_RUNTIME_IDENTITY_CENSUS_DESIGN.md`, design tip `abf433f6767be39d0ddac5cf2b4c194ca0fa10a4`; independent design review passed and is recorded on `codex/phase12/P12BIdentityRegistryWitnessDesignReview` at `7f911ab`.

## Delivered boundary

The registry now issues eight fixed passive census providers for its existing typed identity indexes. Each provider reports its index's live dictionary count, schema 1, the exact registry object as owner identity, and the registry's shared monotone identity-membership revision. The already-published bootstrap composition exposes only the ordered provider collection; it does not expose the raw registry or a general provider-registration hook.

Each successful single-item registration advances the revision once after insertion. The existing validated LocalPlace/LocalConnection publication batch advances once after a non-empty commit. A valid empty batch leaves the revision unchanged. Null, duplicate, cross-type collision, rejected batch, and revision exhaustion preserve all indexes and the prior revision. Exhaustion is checked before single or batch mutation. Blank runtime IDs are not representable through the supported runtime constructors: all eight constructors reject blank IDs before a registration call can be made; the focused suite verifies these constructor boundaries.

The selected `UnityBootstrap-Daily-v1` profile's live census is `10/2/2/2/0/0/0/0`, total 16, with revision 16. All eight providers report the same installed `RuntimeIdentityRegistry` identity. Repeated reads leave identity, cardinalities, and revision stable.

## Validation

Validation completed on 2026-09-29 against the implementation checkout:

| Gate | Result | Evidence |
|---|---:|---|
| `RuntimeIdentityCensusTests` | 6/6 | `Temp/ValidationResults/EditMode-20260929-221909-86f9ce2e666d4dd899a65ad81d98cff5.xml` |
| Selected authored bootstrap profile | 1/1 | `Temp/ValidationResults/EditMode-20260929-221245-0abbc0fd1b144ce4abdd498b6e14f112.xml` |
| ALL EditMode | 1963/1963 | `Temp/ValidationResults/EditMode-20260929-221928-ce9cfdc06525421889beacde0a7c23c4.xml` |
| Official complete `Smoke` | 5/5 | `Temp/ValidationResults/EditMode-20260929-222003-64db68fb6c6b42dab8630f9d541fc261.xml` |
| `git diff --check` | passed | implementation checkout |

## Limits retained

This adds live P12-B cardinality/revision evidence for one owner only. It does not add the shared P12-B mutation epoch, runtime registration, capture eligibility, owner-thread or quiescence enforcement, serialization, staged hydration, or any P12-C export implementation. It does not claim that LocalTopology is composed or complete. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY` pending complete included-owner coverage, staged hydration, and the separate profile implementation gate.
