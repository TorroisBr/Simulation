# Simulation Core extraction state

This file tracks unnumbered Core infrastructure checkpoints. It does not
change numbered Phase State or imply Phase readiness.

Architecture approval: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
Production base for CORE-A: `codex/phase12/canonical` at
`4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.

## CORE-A — Calendar Shared Assembly

**Current status (2026-10-14):** the promoted TRX packager produced and privately compared the cold/warm .NET package; package integrity and privacy checks passed. The original candidate `f0bcb3e873fcf32df92c9dc3bc1a1ad20dae77d4` received exact-tip `NEEDS_CHANGES` because its validation record named a manifest hash matching neither the committed blob nor the Windows checkout. The current-base integration branch `codex/core/COREA-calendar-current-integration` was refreshed onto Core canonical `570b9f7c2718b79519b9a84b18e55748cc95ee35`, including the verifier correction. It corrects the representation-specific manifest hash while preserving CORE-A implementation, Unity test source, original run results and package bytes. The promoted CLI correction verifies both retained archives. The source-context comparison remains the recorded result from tool `c68ca7a9`; verification under the newer tool is archive-only because the package provenance identifies `c68ca7a9`. Exact-tip integration review passed at `3edc40af75eb4c442b55c398c9cf9309f474c1de`, and candidate `96135eb6a87e5217d169a56414a20d21e6649af6` (tree `51d0f26e90804fedf6e9dae7379342ac4e5c2405`) was promoted to `codex/core/canonical` on 2026-10-14. See the [CORE-A validation record](validation/COREA_CalendarSharedAssembly/VALIDATION.md) and [normalized TRX evidence record](validation/COREA_TRX_EVIDENCE_V1/VALIDATION.md).

- Preserved source candidate: `codex/core/CORE-A-calendar` at `f0bcb3e873fcf32df92c9dc3bc1a1ad20dae77d4`.
- Current-base integration branch: `codex/core/COREA-calendar-current-integration`, promoted from `codex/core/canonical` base `570b9f7c2718b79519b9a84b18e55748cc95ee35` as `96135eb6a87e5217d169a56414a20d21e6649af6` (tree `51d0f26e90804fedf6e9dae7379342ac4e5c2405`). Exact-tip integration review: `3edc40af75eb4c442b55c398c9cf9309f474c1de`.
- Calendar implementation commit: `e702cdf196703ead98d8d37ff818bd1c0d0219e4`.
- Parser remediation commit: `f725fa87c6d1259d822180b84856b747c9e02980`.
- Parser remediation tree: `bdb6b9b63532e780661477430099a835d2b4f086`.
- Scope: one engine-free `Simulation.Core` assembly for the existing calendar,
  date and age capability; Unity and .NET compile the same source.
- Promotion target: `codex/core/canonical`, promoted to `96135eb6a87e5217d169a56414a20d21e6649af6` after exact-tip review PASS.
- Numbered Phase canonicals were not changed.

The calendar implementation and test source remain unchanged by the parser
remediation. The first exact-tip review of `78011db2` found missing TRX run
identity/summary binding and an absent durable raw-report bundle. The parser
now requires and persists those fields; focused self-tests and a clean .NET
validation rerun pass. The first review's record is
`c4c73bae26ecbacdb9e761821f408534610622a2`. The fresh exact-tip review of
`2062e616` passed at `ad4d141d0ec00dee6b776d9373187173a330f0cc` and confirms
that the parser finding is resolved. The original reports remain protected locally because publication would disclose private run metadata. The normalized privacy-safe package recorded below provides complete public result evidence; its package-record hash correction remains under exact-tip review. The raw reports remain unmodified and private.

## CORE-B — Deterministic and Temporal primitives

Technical design candidate `d20c2352887184ccc964d7f9586407d00a727d32` received
independent PASS. Review record `96a7df8c2ebad6cf5ce3bfe2bde140f98c6e46dc`
is pushed. CORE-A is promoted in `codex/core/canonical` at
`96135eb6a87e5217d169a56414a20d21e6649af6`; the canonical used for CORE-B
current-baseline design was `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af`.
After CORE-B promotion, the current remote Core canonical advanced to
`bb82446c279ef47cf0d18a371f4728b2be4090e3`. Current-baseline design
revalidation passed at review record
[`CORE_B_CURRENT_BASELINE_REVALIDATION_C957528.md`](reviews/CORE_B_CURRENT_BASELINE_REVALIDATION_C957528.md)
(`3eb18a6a09ee430e15b2d479c02031b221443fd8`). It confirmed the CORE-A
calendar/date dependency and no semantic drift, and authorized bounded CORE-B
implementation from c957. Its evidence-wording clarification is explicitly
nonblocking: complete privacy-safe normalized public reports and immutable raw
reports retained in protected storage satisfy the existing evidence policy.

Implementation candidate `codex/core/CORE-B-deterministic-temporal-c957` is
based on c957 and includes the durable current-baseline review record. The
DeterministicRandom and LogicalTimeline source moves, shared stable-key encoder,
SDK compile ownership, conserved shared tests and narrow P12 snapshot bridge are
implemented. SDK source-closure build passed; shared .NET suite passed 97/97
with cold/warm CoreValidation; focused Unity RNG/snapshot passed 17/17, focused
LogicalTimeline passed 38/38, ALL EditMode 3,085/3,085 and official Smoke 5/5.
The normalized ALL EditMode evidence package is verified. Filtered Smoke
remains privately retained because the supported packager rejects its
incoherent aggregate suite counts; its raw report was not modified. See
[`validation/COREB_DeterministicTemporal/VALIDATION.md`](validation/COREB_DeterministicTemporal/VALIDATION.md).
Independent exact-tip review passed for implementation/tooling candidate `15833b01830d0a8789803af0e28e0e5529ddf916` (tree `163c296a530c477a968f2326166cf62a7a26366f`). The review record is `79460dbffef35eeaa85f814e11e2a7b6a6bc894f` on `codex/core/COREB-CoreValidation-15833b-review`. A later docs-only correction aligned the committed archive and manifest filenames with the manifest's declared archive basename; exact-tip package review passed at `cea14011ea630f008b80b3d4f01968afe5546ca6` on `codex/core/COREB-ArchiveName-4276bc9-review`. It verified unchanged archive and manifest bytes and kept archive-only verification distinct from the earlier private original comparison.

CORE-B was promoted to `codex/core/canonical` on 2026-10-14 by fast-forward from `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af` to `4276bc931c2119e22a0bebcec26b90c085aa9483` (tree `9c574eca02c85eba08096a94612e999ebfeb4aae`). Remote synchronization was verified at that exact SHA. The promoted checkpoint adds the reviewed CoreValidation evidence-directory/runtime-trace tooling to the reviewed CORE-B implementation; production and test source trees remain identical to candidate `3560f395`. The final cold/warm TRX package is `coreb-coldwarm-74920c53a6894ab1a1b7d942748a02b2.zip` (SHA-256 `b3c745f1c45acaf88397d325c6cc09ae45bfd37d11a2222b757b0b35ec7779a3`); its manifest is `coreb-coldwarm-74920c53a6894ab1a1b7d942748a02b2.manifest.json` (SHA-256 `16f6331cc36c0c01f953e2cda09d71139824bf4ef92ab710287e8e4d9f3e3044`).
## CORE-C — Market numerical capability

Technical design candidate `e80ae27cdf597e9f551fe94dd22827f59636712a` received
independent PASS after correcting the prepared-route callback count. Durable
review record `cb98d3e99c9491ce2c34278516272763333b271e` is pushed. CORE-C's
bounded implementation follows the documented CORE-A and P14-B dependencies.
Its candidate and exact validation are recorded in
[`validation/COREC_FiniteSource/VALIDATION.md`](validation/COREC_FiniteSource/VALIDATION.md).

## Historical readiness boundary before CORE-C implementation

**Historical snapshot:** the following readiness statements describe the
period after CORE-B promotion and before CORE-C implementation/promotion. They
are retained for traceability and are superseded by the later CORE-C and
CORE-D status sections below.

CORE-A and CORE-B are promoted. CORE-B passed design review,
current-baseline revalidation, cold/warm validation, package verification,
independent exact-tip implementation/evidence review, final docs-only package
review, and canonical fast-forward. It does not claim P12 readiness, capture
eligibility, or persistence compatibility.
CORE-C has an exact design PASS and current-baseline applicability PASS at
`8fa0408e481f457c153d7552eaed8ed5551cc612` (design PASS record
`cb98d3e99c9491ce2c34278516272763333b271e`). It can be scheduled after CORE-B
to serialize the shared Core package, test-project, Market and City integration
hotspots; CORE-B is sequencing, not a design or dependency blocker. The older
`WAIT_DEPENDENCY` and pending-review wording in the CORE-C design is superseded
by its current review records and this State. CORE-A and P14-B are promoted;
CORE-B is now promoted and the serialized integration window is clear. The
CORE-C checkpoint was `READY_FOR_IMPLEMENTATION` within the exact reviewed
P14-B extraction boundary. Its implementation candidate is
`codex/core/CORE-C-P14B-finite-source` at
`114e3a7ec9f794b2996305eb1d640a8a58ab1083` (full tree
`2359e06c25dc9f244fe48b653eb5ace9d9930583`), based on Core canonical
`bb82446c279ef47cf0d18a371f4728b2be4090e3` and P14 canonical
`09fcbe46ffb5a7d80377185692d550310debc110`. The tested source/input tip was
`b33a302f9b78e8f35ba0ea94bf7168e6ee94f86f` (tree
`170d95b3fa7fc73a069a472f029261e880680415`); later candidate commits add only
the validation record and evidence packages. The implementation commit is
`6355d37f416f88f90eb11ce6c8283c23d8acfb51` (tree
`b5ad86fd6114db106d7223e6e70da3bd5ff2b62c`).

Focused validation passed, .NET cold/warm passed 104/104 each, ALL EditMode
passed 3110/3110, official Smoke passed 5/5, and `git diff --check` passed.
The normalized .NET package was privately compared against the protected
originals; its archive and manifest hashes are recorded in the linked
validation record. The ALL EditMode package verified against its protected
source run. The separate Smoke package was rejected for an inconsistent XML
aggregate count; its original remains private and unchanged, and all five Smoke
cases are present in the verified ALL EditMode report.

Independent exact-tip implementation review passed with no actionable findings
for candidate `114e3a7ec9f794b2996305eb1d640a8a58ab1083` and tree
`2359e06c25dc9f244fe48b653eb5ace9d9930583`. Durable review record
`c04ec332eef29387463937c7153387d9b8728c75` is published on
`codex/core/CORE-C-implementation-review-114e3a7`; the record commit is a direct
child of the reviewed candidate and adds only the review document. The review
checked the complete base-to-candidate diff and exact validation references;
tests were not rerun during review. CORE-C was promoted to `codex/core/canonical`
on 2026-10-14 by fast-forward from `bb82446c279ef47cf0d18a371f4728b2be4090e3`
to `0e400bcf333ae7bfdb2d28287d2d40929b5774cf` (promotion-record tree
`cd0dbb289b21bcf6edb1782494f7e18536e93a0a`). The promoted code remains the
reviewed candidate `114e3a7ec9f794b2996305eb1d640a8a58ab1083`; the promoted
integration adds only the review record and factual State/validation updates.
Remote synchronization was verified. CORE-C extracts the bounded existing
Market price calculation and prepared finite-source production kernel; it does
not route P14-C mixed-source production through the Core kernel. No claim is
made about P12 readiness, equivalent .NET P12 continuation, P13, full world
hosting, or broader simulation hosting.

## TRX-EVIDENCE-v1 — shared .NET evidence packaging

**Status:** promoted as shared Core infrastructure. The latest reviewed
implementation commit is `b7535041a8aeb6e341fe35c6fcc465da48ce313e` (tree
`67b595da4c1337f4da29116cb4d3e791ada54d91`), promoted to
`codex/core/canonical` together with its review record at
`ba7c5c463a4754fe1f2b174b47fb6d79cdb30df5`. This is an additive correction to
the earlier promoted implementation `c555eb21c6f0f658dd851fbeda8d12ac59ddd18c`
(tree `274cb87538ca0038fd31c35457fd3f99d29ba48d`). The Core line remains based
on P12 production history. The bounded architecture and contract documentation
delta is from architecture canonical
`63d3b8352a450b27b9a5f593c4ea166c0343c59e`; the applicable validation policy
is the independently promoted `58157f9ac7bf573e9a4ed1b2d485a2db190e7c4f`.
The exact-tip independent review record is
[`INDEPENDENT_REVIEW_TRX_EVIDENCE_V1_B7535041.md`](../Tools/UnityValidation/INDEPENDENT_REVIEW_TRX_EVIDENCE_V1_B7535041.md)
on `codex/tools/trx-evidence-v1-review-b7535041`.

The latest correction requires cold then warm attempt order in both packaging
and archive verification, with a regression rejecting reversed equal-duration,
equal-exit attempts. Exact-tip validation passed: TRX synthetic tests 144
assertions, shared packager tests 64 assertions, Unity runner self-tests 33/33,
and `git diff --check`. Independent review reran those checks. Unity
EditMode/Smoke and .NET domain runs were not repeated because the change is
limited to evidence packaging and tests and does not change domain code, runner
behavior, fixtures, or execution inputs.

The reviewed pre-promotion package trial for CORE-A has archive SHA-256
`540026a1fe878a927bd2ba70c51dc5e03b87526f43f474467f60eb68cb5a43ed` (13,867
bytes). Exact-context comparison reported `originalComparisonVerified`; a
separate archive-only verification reported
`originalComparisonNotPerformed`, as required without private source context.
This trial was made before canonical promotion and is not the final CORE-A
evidence package.

**CORE-A evidence update:** the promoted TRX tool generated and privately compared the final cold/warm package. Its source identities, results and privacy boundary are recorded in `validation/COREA_TRX_EVIDENCE_V1/VALIDATION.md`. The old-base package candidate received `NEEDS_CHANGES` at `451ba62b1419a3936abcc28bc60f14eaa17eecfc` because its manifest-hash label matched neither the committed UTF-8/LF blob nor the Windows CRLF checkout. The current-base integration corrected the label to the committed blob hash and preserved the normalized report and archive bytes. Unity and TRX archive-only CLI verification passed under the promoted verifier correction. The prior private comparison under tool `c68ca7a9` remains the applicable source-comparison result; no comparison under the newer tool is claimed. Exact-tip review passed at `3edc40af75eb4c442b55c398c9cf9309f474c1de`, and the reviewed integration was promoted at `96135eb6a87e5217d169a56414a20d21e6649af6`. CORE-A makes no P12-A, P13, cross-build compatibility, or broader .NET hosting claim. CORE-B current-baseline revalidation passed; CORE-C current-baseline applicability passed at the cited review record and it is sequenced after CORE-B for shared integration hotspots.

### Unity CLI verifier import-order correction

**Status:** promoted to `codex/core/canonical` by commit `fb2189e41b4ebfe51146be5c4f85fc5c58270a75`.

The exact implementation candidate is `2bda6341f3c5e599064e10ccda569237b9aea445` (tree `f1d69e18dcf52e4f686116575fceb1729f53b59b`). It loads the TRX adapter before the Unity adapter so the shared CLI retains both command sets, and adds a subprocess regression that invokes the Unity `Verify` entry point. Independent exact-tip review passed at `7f0aeed1a036fe07dc6cd87f91b1c1652c099e8a` on `codex/core/TRXUnityVerifierImportFix-review-2bda6341`.

Exact-tip validation passed: TRX evidence suite 144 assertions, Unity evidence packager suite 65 assertions (including the real CLI regression), Unity runner self-tests 33/33, and `git diff --check`. Archive-only CLI verification of the retained Unity package passed with archive SHA-256 `ad99dea856aac26d94fec35c970ab8e9142dc77f9f7e95a3bcc0a89ffdd0c614`. The retained TRX package also passed archive-only CLI verification with `originalComparisonNotPerformed`. The earlier `originalComparisonVerified` result belongs to the promoted `c68ca7a9` tool and remains a separate preserved result. This correction does not alter simulation behavior or package contents.

## CORE-D — shared finite-source runtime and Lab host

**Status:** promoted to `codex/core/canonical` at candidate commit
`45f80689acceebdbe7e56565640a970142ec43a3` (tree
`1ddae66b757fb44466d0581afb5be3f9faf43f57`) on 2026-10-14. This is the bounded
shared finite-source runtime and Lab-host slice described below.

The candidate branch is `codex/core/CORE-D-implementation`. Its implementation
commit is `e83ea3ba7c9cbddf76280f20e12785ce82383e43`; the exact validated
source/tooling commit is `e53857e3f157def116136001e78af97e64ff50cb` (tree
`cf63c3db6054f749a82c631ea85c7e216001cf04`). The final candidate tip adds
validation records and corrected manifest digest labels only. The `Assets`, Core package,
`src`, `tests`, and `tools/Simulation.Lab` trees remain identical between those
commits. The implementation composes the promoted CORE-C finite-source kernel
through one immutable Core scenario runtime, reuses it from the Unity adapter
and fixed .NET Lab host, and keeps P12 Daily-v1 and P14-B admission boundaries
unchanged.

Exact-source validation passed .NET Core cold/warm 113/113, the combined Lab
cold/warm deterministic scenario, the standalone locked Lab runner, focused
Unity `FiniteSource` 41/41, ALL EditMode 3,120/3,120, official Smoke 5/5, and
the existing Core validation self-tests. The `.NET` cold/warm reports were
packaged and privately compared under the promoted TRX evidence tool;
context-bound and archive-only verification passed. ALL EditMode was packaged
and verified with the existing Unity evidence tool. The initial exact-tip
implementation review found only stale prose hashes for the two committed JSON
manifests; its record is `8ff0e76a45b482e28d79dafe177c51c17d529021`. The
corrected documentation-only successor `45f80689acceebdbe7e56565640a970142ec43a3`
received independent exact-tip applicability PASS at review record
`271bdfff6327e4568045531a411a421f8e46f463`. Run IDs, source identities,
hashes, and exact Lab output are recorded in
[`CORE-D validation evidence`](validation/CORE_D_ScenarioLab/VALIDATION.md).
The previous ALL EditMode package and manifest remain preserved unchanged.

These results do not claim an equivalent P12 continuation host, P12/P13
readiness, full P14-B City daily economy, persistence, cross-build compatibility,
or a public extension surface. The P12 Daily-v1 owner inventory and admission
contract are unchanged; this does not create a .NET continuation host or reopen
the closed P12 profile.

## Current Core extraction readiness — 2026-10-14

At the verified `codex/core/canonical` base
`158cc6f5663c5561a04ddafc623bac7bb41f6c38`, CORE-A through CORE-D were
promoted. CORE-D adds the bounded shared finite-source runtime and fixed Lab
host described above.

CORE-E remains the architecture-defined collection of individually reviewed
domain closures. The current canonical history contains no next CORE-E
checkpoint handoff or candidate design, so no additional domain extraction is
implementation-ready from this State alone. CORE-F remains blocked on the exact
selected Daily-v1 owner/definition/runtime closure and its own host design;
P12 closure and CORE-D do not satisfy those prerequisites. CORE-X remains
deferred pending an explicit compatibility scope and design. These extraction
readiness limits do not change numbered Phase status or authorize profile growth.

## CORE-F current-base owner and asset audit — 2026-10-14

The CORE-F Daily-v1 host design was authored against Core canonical
`6950f596cab9f96921290e24a9aeb091229dcc6d`. Its current-base review classifies
the change to `032259b4177a24ee4c0c536919f65e052cef9572` as
`BASE_DRIFT_ONLY`: CORE-E adds the bounded `PersonId` and demographic-policy
sources, but does not change the selected P12 owner-family inventory,
cardinality rules, codecs, staged restore, or admission contract. The design
requires docs-only baseline and dependency corrections followed by a fresh
independent review; the original old-base branch is not itself a current-base
candidate.

The current owner-family inventory contains 89 family rows: 61 fixed families
(48 Required and 13 ExplicitlyEmpty), plus 28 dynamic families (22 per-NPC,
four per-City, one non-Person-NPC residence, and one Person life/residence).
The 39 authored asset Git blobs in the existing
inventory match the inventory's recorded source identities. These counts and
blob matches establish contract-key and authored-source provenance only. They
do not prove post-resolution effective Unity values, a complete owner-to-live
instance map, or parity between Unity-resolved definitions and a .NET input.

The refreshed SDK project explicitly compiles 14 shared Core sources: the
previous 10 CORE-A through CORE-D sources, `PersonId`, and the three promoted
CORE-E demographic-policy sources. The selected Daily-v1 owner codecs,
SimulationRuntime, bootstrap graph, effective-definition resolver, and staged
restoration host remain Unity-owned; the current Core project is not a .NET
Daily-v1 host.

Implementation readiness remains `WAIT_DEPENDENCY`. The audit found existing
owner providers, revision sources, runtime operation boundaries, and Unity P12
capture/restore behavior, but their presence does not establish complete
supported-writer coverage, direct/nested mutation-to-revision mapping, or
runtime-wide epoch invalidation for every admitted owner. Effective-value
parity, complete live owner/cardinality binding, dynamic membership closure,
the complete writer/revision/epoch matrix, Core-owned runtime composition, and
same-build .NET restore/continuation validation remain unproven or absent.
The Unity P12 implementation and its evidence remain valid for Unity; they are
not relabeled as .NET host evidence. No P12 profile expansion or numbered
Phase status change is implied. CORE-X remains deferred.

## CORE-E — deterministic demographic policy (promoted 2026-10-14)

**Status:** the bounded deterministic demographic-policy closure is promoted to
`codex/core/canonical`. The canonical fast-forward was from
`6950f596cab9f96921290e24a9aeb091229dcc6d` to the exact-tip review record
`032259b4177a24ee4c0c536919f65e052cef9572`; the reviewed implementation and
validation candidate is `12d5b203eec15dd70909e0e16093b77d92650622` (tree
`ac50869d65b35228c7c8b927fc56e466f9a9a7e4`). Its tested implementation commit
is `2dcd4933f214d797d4e064b6cea26cdbe5bd5cba` (tree
`17543d1f9f318a77b5e6155f64c62d769e6b6557`). The independent exact-tip
implementation/evidence review is recorded in
[`CORE-E review`](reviews/CORE_E_DEMOGRAPHIC_POLICY_IMPLEMENTATION_REVIEW_12D5B20.md).
The implementation validation is recorded in
[`CORE-E validation`](architecture/SIMULATION_CORE_E_IMPLEMENTATION_VALIDATION.md).

The slice moves the existing deterministic demographic sampler and aggregate
birth/death policy, their required contracts, and `PersonId` into the shared
engine-free Core assembly; Unity calls the same moved implementation. The narrow
`AggregateDemographyContext` constructor accessibility change is the only
behavior-facing API change. Population mutation, receipts, daily coordination,
owner state, and P12 continuation authorities remain in Unity and unchanged.
CORE-E remains a collection of separately bounded closures, not a claim that
all demographic or population systems are portable.

Validation passed the required shared characterization and Unity regressions:
ALL EditMode 3,132/3,132, official Smoke 5/5, and the unchanged LongRun suite
7/7 (including the original 100-, 1,000-, and 5,000-day cases). The normalized
.NET cold/warm package passed 125/125 in each attempt and was privately
source-compared with `originalComparisonVerified`; the locked Lab cold/warm
regression passed. The result-only archive SHA-256 is
`4b74838a29be7fbe63eb83aaff2dc8e015e54931c6011a4cd09dae89d827628f`; its
manifest SHA-256 is
`72bdcb177ca05af1caa5f51a7e9dfe2e928e7fc51842ee668dd3a17f051f91e1`.
Protected Unity originals and private .NET source context remain outside the
public package.

This promotion does not deliver a population owner, natural-mortality query,
headless world advance, full Daily-v1 .NET host, new save profile, or cross-host
save compatibility. It does not change any numbered Phase or P12/P13 readiness.

## Current Core extraction readiness — refreshed after CORE-E

This entry supersedes the older readiness snapshot above. At the promotion record
`032259b4177a24ee4c0c536919f65e052cef9572`, CORE-A through CORE-E have their
recorded bounded promotions; CORE-E is only the demographic-policy slice
described above. The promoted architecture baseline is
`7166ed088483c69d37300b4f12528ebb4b27be84`. CORE-F remains
`WAIT_DEPENDENCY` on the exact selected Daily-v1 owner, effective-definition,
and runtime closure, despite its authorized technical-design work; live owner
bindings and complete supported-writer/revision/epoch evidence remain
prerequisites to implementation readiness. CORE-E does not close that
dependency. CORE-X remains deferred until
its separate compatibility scope and design are approved. This refresh changes
no numbered Phase status and authorizes no profile growth.


## CORE-F WP2-Persistence bounded delivery — 2026-10-15

**Status:** VALIDATED_CANDIDATE, eligible for immediate autonomous bounded
promotion after refreshed remote/fast-forward preflight. This entry is the
factual preflight record; canonical delivery is established by the canonical
ref containing the reviewed candidate and this documentation-only successor.

- Production base: `8ea3b47e9ab1be9ab3fe6261a73dc33fece8cd8f`.
- Approved prerequisite architecture: `4da844dee0c69fdf66562590fa96cc728fc86bf7`.
- Independently validated integration candidate:
  `fb48bea45ee2425fa706e356641d768b9d29f278`.
- Exact tested production/test source:
  `50976a358e3563be3dcd37404ccd9d511d6855ff`.
- Independent review: `reviews/CORE_F_WP2_PERSISTENCE_FINAL_REVIEW.md`.
- Validation: `validation/CORE_F_Persistence/VALIDATION.md`, complete final
  taxonomy ALL and cold/warm result packages, protected unchanged originals.
- Gates: .NET 153/153 cold/warm, locked Lab PASS, Unity focused269/269,
  ALL3163/3163 including seven original LongRun cases, official Smoke5/5,
  base diff-check PASS. No nonpassing cases; source/input identities verified.

The package supplies shared descriptor-v1/wire, the approved bounded v2 value
and admission taxonomy, numeric probe identity and one atomic byte-slot kernel.
Unity retains v1 same-build/current-host wrapper authority and its existing
slot/signatures/results. Canonical v2 rejection is exercised before owner staging.
No owner/runtime host or cross-host compatibility is claimed.

Dependency refresh: WP2 delivery satisfies Values' persistence integration edge;
Values still needs its complete independently constructed164-field value parity,
final two-host gates and independent review. WP1-Owners remains an independently
executable test/evidence package with no new owner implementations. Concrete
selected-owner/runtime/restore source closure and real .NET continuation remain
CORE-F dependencies. Full CORE-F remains WAIT_DEPENDENCY; CORE-X DEFERRED;
closed P12 contracts and all numbered canonical branches remain unchanged.


## CORE-F prerequisite promotion refresh — 2026-10-15

WP2-Persistence is **PROMOTED** at Core canonical
`84b66da69be896f0db347b940d9e464cfaa1c4f2`; local/remote synchronization was
verified. This factual marker supersedes the conditional WP2 preflight status
above. Its exact source, review and complete final packages are unchanged.

WP1-Owners is **VALIDATED_CANDIDATE**, eligible for autonomous bounded promotion
following immediate refreshed-ref/fast-forward preflight and docs-only applicability.

- Independently reviewed candidate:
  `026ebf82f5bb8164036238bc2878bb30f5c2e379`.
- Tested source: `6441c18374eb2b6bacb7238be442ac52d73e0b8f`.
- Review: `reviews/CORE_F_WP1_OWNERS_FINAL_REVIEW.md`.
- Evidence: `validation/CORE_F_OwnerClosure/VALIDATION.md`, complete normalized
  ALL archive and unchanged protected originals.
- Gates: focused owner1/1, ALL3164/3164 including original seven LongRun cases,
  official Smoke5/5, diff-check PASS. Unchanged91 SDK inputs preserve WP2
  .NET153/153 cold/warm and locked Lab applicability; no .NET live graph claim.

The package closes the selected89-family owner-binding/writer/capture/target
migration evidence baseline and24 currently composed operation-route mapping.
It changes no production owner, scope, P12 guarantee or profile. The source-bound
P8 witness was captured before Values production changes from the existing runtime.

Current dependency DAG: A-E promoted -> Values; closed P12 -> Owners and
Persistence; Persistence promoted -> Values canonical integration edge satisfied;
Values plus Owners plus Persistence -> concrete selected-owner/runtime/restore
source closure -> real .NET host/continuation/parity -> full CORE-F. Values still
requires independent fixed164-field fixture parity, final two-host gates and final
review. The source-closure design can be prepared from actual APIs but cannot be
marked executable from a guessed transitive source list. Full CORE-F remains
WAIT_DEPENDENCY; CORE-X DEFERRED; numbered canonical branches remain unchanged.


## CORE-F WP1-Values validated completion — 2026-10-15

WP1-Owners is **PROMOTED** at `8fc975631db0d4e53fa58c490c752fb79d209b0e`;
WP2-Persistence remains **PROMOTED** at
`84b66da69be896f0db347b940d9e464cfaa1c4f2`. Local/remote sync was verified.
These actual deliveries supersede the earlier conditional promotion entries.

WP1-Values is **VALIDATED_CANDIDATE**, eligible for autonomous bounded promotion
once the immediate refreshed-ref/fast-forward and docs-only applicability gates pass.

- Reviewed candidate: `925c58d9cb529db5a7a62f3746a38c9d8ce32481`.
- Executed source: `e2184090f0803a2b0ede16d00da0742189fab9be`.
- Review: `reviews/CORE_F_WP1_VALUES_FINAL_REVIEW.md`.
- Full final evidence: `validation/CORE_F_Values/VALIDATION.md`, aligned cold/warm
  and typed ALL packages,132-row source ledger and protected unchanged originals.
- .NET169/169 cold/warm, fresh locked Lab PASS, actual typed2/2, focused340/340,
  ALL3169/3169 including7 original LongRun cases, official Smoke5/5, diff-check PASS.
- Independent exact source verification:132 SDK inputs,107/107 shared raw hashes,
  all1,635 Unity inputs and both exact typed case results verified.

The package supplies the five-file engine-free configuration closure, complete
copied164-field selected definitions, independent constructor/value parity,
shared P9 identity and shared closed P12 catalog encoding with Unity adapters.
The real WP2 dependency is satisfied; no P12/profile/host compatibility expansion.

DAG refresh: A-E and Persistence -> Values (delivery ready); closed P12 -> Owners
(delivered); Values + Owners + Persistence -> exact shared owner/runtime/restore
source migration -> actual net8 host -> complete same-host .NET continuation and
matched authoritative cross-host execution proof. Full CORE-F remains
WAIT_DEPENDENCY on those actual capabilities, not another abstract owner inventory.
A bound compile graph identifies303 runtime+restore source nodes including a
191-file runtime/domain/census SCC; it is source design evidence, not permission
to move303 files unchanged or admit unselected state. CORE-X stays DEFERRED.
No numbered canonical branch or unrelated worktree was changed.


## CORE-F prerequisites promoted — final factual marker (2026-10-15)

All three approved prerequisites are **PROMOTED** on `codex/core/canonical`:

| Package | Exact bounded promotion SHA |
|---|---|
| WP2-Persistence | `84b66da69be896f0db347b940d9e464cfaa1c4f2` |
| WP1-Owners | `8fc975631db0d4e53fa58c490c752fb79d209b0e` |
| WP1-Values | `c69fcf471be7c73c3b63b539d0ae5e659cb9e029` |

Each was independently reviewed, passed its required gates, and was promoted by
an additive fast-forward after refreshed preflight. Local/remote equality was
verified at each SHA. This actual delivery marker supersedes earlier conditional
eligibility entries; historical records, diagnostics and source identities remain.
The Values promotion includes only its approved implementation and reviewed
validation/review/State successors. Final run sources are unchanged by this marker.

Effective-value parity and the selected Unity owner evidence baseline are now
delivered, as are shared descriptor/wire/atomic storage. Full CORE-F remains
**WAIT_DEPENDENCY** on actual shared selected-owner/runtime/restore source closure
and the real net8 host continuation/comparison proof. The bound303-file source
closure and precise Unity/numeric/serialization inversions inform the next bounded
technical design; static compilation alone is not a .NET execution result.
No new product/architecture choice has been identified. No arbitrary profile,
P12 guarantee, cross-host save or CORE-X scope is admitted by these promotions.
P12 canonical remains `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`;
architecture policy remains `4da844dee0c69fdf66562590fa96cc728fc86bf7`.


## CORE-F source-preparation architecture adopted — 2026-10-15

Production base: `fff5cca0297ce5af346a264b7fa2e2e37be587b0`.
Promoted architecture: `758d4eaf7022e09f4b106ef25b390d14d6b55f7a`.
This successor imports only the new reviewed documentation/evidence delta and
refreshes planning; it changes no executable source or validation input. The three
prerequisite promotions and their final exact-source reviews/packages above remain
current: Persistence84b66da6, Owners8fc97563, Valuesc69fcf47, all PROMOTED.

[Exact Master design handoff](architecture/CORE_F_SHARED_RUNTIME_SOURCE_MASTER_HANDOFF.md)
and [independent assessment](architecture/CORE_F_SHARED_RUNTIME_SOURCE_REVIEW.md)
are canonical architecture and imported here. The source/readiness assessment
passed independent review; only the finite next compatibility design/evidence wave
is READY_FOR_TECHNICAL_DESIGN. Full CORE-F and production runtime/restore source
migration remain WAIT_DEPENDENCY, not READY_FOR_IMPLEMENTATION.

The next authorized work captures original Unity frozen-payload and numeric
compatibility evidence, fixes exact engine-free APIs/access/source cuts and receives
independent technical review before port dispatch. The329-node application graph
and separate Core API/source-reference inventories identify real dependencies;
they are not303 unchanged source moves, owner/profile admission or .NET runtime
proof. No original implementation source or evidence is invalidated by this
compatible documentation-only refresh.

DAG: A-E + promoted Values/Owners/Persistence -> bounded compatibility/source-cut
design -> reviewed actual selected owner/runtime/restore migration -> fixed net8
host using the same runtime -> complete same-host T/T+1/T+2 continuation and
authoritative execution comparison -> CORE-F bounded promotion. The required
89-family/24-route matrix, completed token/epoch/lease and failure atomicity
remain intact. CORE-X stays DEFERRED; no cross-host/build save promise, new P12
scope, P13 readiness or numbered-phase reopening/lock follows.

The architecture promotion was docs/evidence-only and fast-forward, independently
reviewed at758d4eaf and verified local/remote. This Core adoption requires the normal
independent exact-tip docs-only applicability, diff/link/privacy check and fresh
fast-forward preflight. Existing final .NET169/169 cold/warm, locked Lab PASS,
Unity typed2/2, focused340/340, ALL3169/3169 including7 original LongRun cases,
and Smoke5/5 remain their original run results; they are not new host evidence.
No routine human decision remains for the next bounded technical-design wave.


### Core source-preparation adoption — independent applicability

Independent Luna reviewer `/root/wp2_final_review` passed exact Core adoption
`e8b2343e525a1965cc1064c09a92d5b646606041`:13 imported artifact blobs match
promoted architecture758d4eaf exactly, and all16 changed paths are documentation.
The reviewer confirmed no executable-input differences between executed source
e2184090, production basefff5cca0 and this adoption in the relevant source/test/
Tools/profile roots. All original final validation remains applicable; no new run
or .NET full-world proof is claimed. Diff-check and handoff/review links passed.
This State-only successor records that independent PASS and requires unchanged
metadata applicability plus immediate safe fast-forward preflight, without another
routine user gate. Exact canonical delivery is established by the containing
commit on `codex/core/canonical`; architecture and numbered-phase refs are separate.


## CORE-F Shared Runtime Source design adoption — 2026-10-15

This State successor adopts the independently reviewed compatibility design into
Core canonical. The architecture design promotion is current at
[`codex/architecture/world-identity-projection` `bec3998f697f362944aa5b551d45998145f4e67e`](https://github.com/TorroisBr/Simulation/tree/bec3998f697f362944aa5b551d45998145f4e67e).
The Core design/evidence candidate is `codex/core/CORE-F-Shared-Runtime-Design-652ac7b`
at `781cfb71f43bd80d781aaf7a678495ad4e7cdc46`, based on the current Core canonical
`652ac7bf02943cf6160fd27f271a781db70657c5`. The reviewed technical-design SHA-256
is `36137D97DDBB633ECDE6C82E5EE1F7B60ED98F5457781B506531E254DECDE2FE`; its
independent design review is `PASS` in
[`TECHNICAL_DESIGN_REVIEW.md`](validation/CORE_F_SharedRuntimeSource/TECHNICAL_DESIGN_REVIEW.md).
The exact-tip candidate review and its docs-only successor review are recorded in
[`CANDIDATE_REVIEW.md`](validation/CORE_F_SharedRuntimeSource/CANDIDATE_REVIEW.md).

The accepted Unity oracle is run `d8b4c40e04ab466a9c56fdf6e5f5361d`: Unity
6000.3.9f1, Test Framework 1.6.0, unfiltered EditMode 3,173/3,173 with zero
failures/skips/inconclusive. The integrated Editor fixture SHA-256 is
`B0713750A006BBF1A5B75AD70EE183CF3CD0A2581D940DF49335B2037B70D5B5`. Current
public evidence rehashes to the reviewed identities: manifest
`75652F63593191509DFC9E5E3B2F2C277657BAD4BF33B33D2E3D581CDC2AC80B`, archive
`D930CA6E929ABBBDF14508C245363FE700F519CE4A6032CEB895233596520B9B`, capture
records `95E3912A43712693CF6EF68CF22E306A770B53A51818B0D6123842F6FA1D7A44`,
and the 172 numeric vectors `2A5D2C653CEC82421936939A0FFBABABBB2F12B8834AFA645B890D884DEAF2F1`.
The prior independent design review verified the original XML/log comparison
against the protected run manifest and the 247 captured outputs. This adoption
relies on that existing attestation and the unchanged, hash-verified public
package; the protected raw XML/log were not reopened in this orchestration run,
so this record does not claim a new original-report comparison.

The reviewed design's readiness matrix records its status at design review time
(`CORE_CANONICAL_ADOPTION_PENDING`) and is superseded by this current State. The
active Master instruction explicitly authorizes the next bounded compatibility
extraction after this Core adoption; implementation authority comes from that
instruction, not from architecture promotion alone.

- **READY_FOR_IMPLEMENTATION:** the closed typed P18-D `Plan`/`Edge` Core codec
  values/API and golden tests, using the exact captured JSON bytes and reader
  outcomes; and the selected numeric compatibility helper overloads with the
  complete reviewed 172-vector/domain set. Keep each implementation within the
  shared `Simulation.Core` package and existing explicit .NET source/test links.
- **WAIT_DEPENDENCY:** switching `P18DCommercialSharingBoundaryStepProvider`
  to the shared codec until the Core codec candidate passes its .NET and Unity
  parity/rejection gates and independent exact-tip review.
- **WAIT_DEPENDENCY:** selected owner migration, shared runtime/restore, and a
  real .NET Daily-v1 host until the full per-owner source/writer/epoch/export/
  hydration/relationship closure and serialized composition gates are satisfied.

The first implementation does not change P12's same-build/current-host
`UnityBootstrap-Daily-v1` contract or add a save format. It does not move owners,
rewrite causal bytes, close full CORE-F, establish CORE-X or P13 readiness, or
change numbered-phase branches. Full CORE-F remains **WAIT_DEPENDENCY** on the
actual selected owner/runtime/restore source closure, host composition, detached
restoration, and exact .NET continuation/parity proof. CORE-X remains **DEFERRED**.

## CORE-F — Shared Runtime Source compatibility extraction (2026-10-15)

The Core-canonical adopted design has implementation candidate `6431fad3bc94263d2db5699706bdf185683b0f76` (source tree `4cdae296cc2fa84e26d48b38636ed9a9647cab06`), based on Core canonical `b8962cbc8a390c735858ac77b6fdf8e50cfaea8b`. It extracts the approved typed payload codec and selected numeric compatibility helpers into the shared engine-free Core source path with Unity/.NET tests. Scope excludes provider switching, owner migration, runtime, P12 persistence changes, restoration, and host composition. Full evidence is in [`CORE-F compatibility validation`](validation/CORE_F_SharedRuntimeSource/IMPLEMENTATION_VALIDATION.md).

.NET cold/warm validation passed 171/171 each. Unity 6000.3.9f1 / Test Framework 1.6.0 unfiltered ALL EditMode passed 3,175/3,175 with stable complete inputs. The final input-bound Unity package is `core-f-compat-6431fad3-unity-all-inputbound.zip` (SHA-256 `ee56efbee92816320dd681911764e1418ba02815d95313bd2d8d070c7421aa13`); the .NET TRX package is `core-f-compat-6431fad3-cold-warm.zip` (SHA-256 `6917029217d029e4bf7c8f752aced893bdf0a4a476cd242eec5ab539d05ec878`). The complete Unity report includes the two focused parity cases and all five Smoke cases. Filtered executions also passed, but their standalone XMLs are not packaged because the fail-closed verifier detects stale parent-suite aggregate counts; no XML was changed or accepted as normalized evidence.

The implementation candidate received independent exact-tip review PASS at candidate tip `1057f52eb2165421f05d8c9f0b25403719de6c83`, against Core canonical base `b8962cbc8a390c735858ac77b6fdf8e50cfaea8b`. The reviewed code remains `6431fad3bc94263d2db5699706bdf185683b0f76` / tree `4cdae296cc2fa84e26d48b38636ed9a9647cab06`; subsequent candidate changes are documentation/evidence only. The first review finding about omitted CSV fixture inputs was resolved by the extra-input sidecar and final full run, then independently rechecked. The durable review record is [`IMPLEMENTATION_REVIEW.md`](validation/CORE_F_SharedRuntimeSource/IMPLEMENTATION_REVIEW.md). The candidate is a validated bounded checkpoint awaiting canonical promotion preflight. CORE-F remains WAIT_DEPENDENCY for provider switch, selected-owner extraction, shared runtime/restore, and .NET host. This candidate does not close CORE-F, change P12, or establish CORE-X/P13 readiness.

The reviewed design contains one prose discrepancy about null object rows in the runtime roster versus the hash-bound Unity capture. This candidate follows the captured output and leaves provider behavior unchanged; reconcile the design prose and verify the capture before a provider switch.

### Unity fixture-input identity remediation

An initial independent review found the Unity snapshot omitted the three CSV oracle fixtures consumed by the new parity tests. The finding was addressed without changing code/tests: final run `17bfb6a506ee456f8aeb7d8ed77f5b45` passed ALL EditMode 3,175/3,175 with stable inputs, and the committed extra-input sidecar binds each fixture's pre/post hashes and Git identity to the run. The final verified result archive is `core-f-compat-6431fad3-unity-all-inputbound.zip` (SHA-256 `ee56efbee92816320dd681911764e1418ba02815d95313bd2d8d070c7421aa13`). Independent exact-tip review then passed as recorded above. No provider switch or broader CORE-F readiness is claimed.

## CORE-F compatibility slice promotion — 2026-10-15

The bounded codec and numeric-helper implementation/evidence candidate was
promoted to `codex/core/canonical` by fast-forward from
`b8962cbc8a390c735858ac77b6fdf8e50cfaea8b` through
`6b2364cc500a32a68cef7edc39ff508a0d68dff0`. Remote refs were refreshed before
promotion; candidate ancestry, exact source tree, package hashes, and
`git diff --check` passed. The exact implementation is `6431fad3bc94263d2db5699706bdf185683b0f76`, tree
`4cdae296cc2fa84e26d48b38636ed9a9647cab06`. Independent implementation review
passed for exact candidate `1057f52eb2165421f05d8c9f0b25403719de6c83`; the
independent review of docs-only successor `6b2364cc500a32a68cef7edc39ff508a0d68dff0`
also passed and verified unchanged code/test blobs, evidence identities, scope,
and protected-file boundaries. See
[`implementation review`](validation/CORE_F_SharedRuntimeSource/IMPLEMENTATION_REVIEW.md).

The final evidence remains .NET CoreValidation cold/warm 171/171 each and Unity
6000.3.9f1 / Test Framework 1.6.0 ALL EditMode 3,175/3,175. The Unity full
report contains the two codec/numeric parity cases and all five Smoke case
identities. Final package hashes are recorded in the linked validation and
review records; the input-bound Unity archive is
`core-f-compat-6431fad3-unity-all-inputbound.zip` and the .NET archive is
`core-f-compat-6431fad3-cold-warm.zip`. No new test run or original-XML
comparison is claimed by this docs-only promotion record.

This promotion delivers only the shared typed frozen P18-D `Plan`/`Edge` codec,
selected Unity-compatible numeric helpers, and their shared .NET/Unity tests.
The Unity provider, owner authorities, `SimulationRuntime`, P12 persistence,
restore, and host composition are unchanged. P12 remains closed only for its
accepted same-build/current-host `UnityBootstrap-Daily-v1` profile; this slice
adds no P12 profile/save guarantee. CORE-F remains open and
`WAIT_DEPENDENCY`.

## CORE-F readiness refresh after compatibility promotion — 2026-10-15

- **PROMOTED:** the reviewed codec and numeric-helper compatibility foundation
  above.
- **WAIT_ARCHITECTURE_RECONCILIATION:** the P18-D provider call-site switch.
  The canonical design says a JSON null element in `runtimeRoster` remains a
  null row (`docs/design/CORE_F_SHARED_RUNTIME_SOURCE_TECHNICAL_DESIGN.md`,
  “Frozen P18-D payload codec”). The hash-bound capture CSV for run
  `d8b4c40e04ab466a9c56fdf6e5f5361d` instead records sequence 18 as
  `roster=len=1[runtimeId=~,personId=~]`; the current Core codec intentionally
  matches that captured default-object behavior, and the existing provider
  rejects the incomplete row before owner mutation. The capture row and source
  were inspected here; this is not a new claim of raw-report comparison. The
  design owner must reconcile the prose before provider switching. No production
  behavior or design meaning was changed by this promotion.
- **WAIT_DEPENDENCY:** owner-group extraction, shared runtime/restore
  composition, and the .NET Daily-v1 host still require complete selected-owner
  source/writer/epoch/export/hydration/relation closure and serialized
  composition evidence. Full CORE-F remains `WAIT_DEPENDENCY`; CORE-X remains
  `DEFERRED`.

The dependency edges are unchanged: promoted Persistence/Owners/Values plus the
closed P12 contract support the compatibility foundation; the provider switch
depends on design reconciliation; selected owner/runtime/restore extraction
depends on complete owner-group closure; and the .NET host, detached restoration,
and exact T/T+1/T+2 continuation parity follow that source closure. No numbered
Phase, P12, or P13 readiness changed. Protected ProjectSettings edits and the
untracked superseded oracle package remain untouched.

## CORE-F null-roster reconciliation adopted; provider switch ready — 2026-10-15

The current architecture canonical is
`4b69e43516b3fc548611ce704ef45af9e54e3d0e`. Its independently reviewed
reconciliation is imported in
[`CORE_F_NULL_ROSTER_PROVIDER_SWITCH_MASTER_HANDOFF.md`](architecture/CORE_F_NULL_ROSTER_PROVIDER_SWITCH_MASTER_HANDOFF.md)
and [`CORE_F_NULL_ROSTER_PROVIDER_SWITCH_REVIEW.md`](architecture/CORE_F_NULL_ROSTER_PROVIDER_SWITCH_REVIEW.md).
The Core technical design now records the hash-bound Unity observation for
`runtimeRoster:[null]`: decode succeeds with one non-null roster member whose
two IDs are null, and the provider separately rejects that incomplete member
before preparation or authoritative mutation. This supersedes the earlier
`WAIT_ARCHITECTURE_RECONCILIATION` status above; it does not change the codec,
admission rule, or original oracle evidence. The amended Core technical design
has SHA-256 `91bbeabf249a5ec77d47fa7c975c6ad9ed9eff01c97ecefff1c60fa59fd21b6f`;
the original adopted design/review and protected Unity oracle identities remain
unchanged historical evidence.

**READY_FOR_IMPLEMENTATION** on Core canonical
`64610d6c4a5cfaec2c9e14fdb70b5e7ed5d5aeaa`: switch only the P18-D Unity
provider's frozen Plan/Edge encoding and decoding call sites to the promoted
`P18DCommercialSharingCodec`, using lossless private DTO conversions. Preserve
raw activation-payload comparisons, provider validation order, owners, commit
boundaries, replay, identities, and failure behavior. The bounded implementation
and validation gates are specified in the imported handoff. No provider switch
has yet been promoted by this readiness record.

Selected-owner migration, shared runtime/restore composition, and the real
Daily-v1 .NET host/continuation remain separately dependency-gated; full CORE-F
remains `WAIT_DEPENDENCY`, and CORE-X remains `DEFERRED`.

## CORE-F null-roster provider switch candidate — 2026-10-15

Implementation candidate `6a1e3a5f49284152159086e2bb22aa50cda510ad` (tree
`4aae462cfee091705bf3e84a5611ee39202507ea`) switches only the frozen P18-D
Plan/Edge codec call sites and decouples the oracle fixture from provider-private
types. It preserves the `runtimeRoster:[null]` decode result and rejects the
incomplete member before preparation or authoritative mutation. The exact
implementation and evidence are recorded in
[`CORE-F null-roster provider-switch validation`](validation/CORE_F_NullRosterProviderSwitch/VALIDATION.md).

Unity focused provider/parity/consumer/daily-boundary suites and official Smoke
passed; unfiltered ALL EditMode passed 3,176/3,176 with stable candidate/input
identities. The normalized report archive and additional-input sidecar verify
the full run. Fresh CoreValidation passed cold/warm 171/171 each; its 142-file
effective .NET input inventory is byte-identical to the already reviewed and
packaged compatibility run. `git diff --check` passed. Independent exact-tip
implementation review and Core-canonical promotion were pending when this
candidate record was written; the following promotion record supersedes that
status. The provider switch does not close CORE-F; selected-owner migration,
shared runtime/restore, detached restoration, and .NET host continuation remain
dependency-gated.

## CORE-F null-roster provider switch promoted — 2026-10-15

The bounded P18-D provider call-site switch was fast-forwarded to
`codex/core/canonical` at `3e817bf14c2d4e9e0f5fc30e48d14197d4450875`, from
`64610d6c4a5cfaec2c9e14fdb70b5e7ed5d5aeaa`. The promoted implementation is
`6a1e3a5f49284152159086e2bb22aa50cda510ad`, tree
`4aae462cfee091705bf3e84a5611ee39202507ea`. The independent exact-tip review
returned `VALIDATED_CANDIDATE` at evidence tip
`cc7c579c64097ec90eac8356e8f6ea61483b62f7`; its durable record is
[`IMPLEMENTATION_REVIEW.md`](validation/CORE_F_NullRosterProviderSwitch/IMPLEMENTATION_REVIEW.md).

Final promotion preflight confirmed the architecture canonical remained
`4b69e43516b3fc548611ce704ef45af9e54e3d0e`, the candidate was a clean
fast-forward, production/test source was unchanged after review, the Unity
package verified against its protected original, the .NET effective input
inventory still matched the reviewed package, and `git diff --check` passed.
The focused provider/parity/consumer/daily-boundary suites, Smoke 5/5, ALL
EditMode 3,176/3,176, and fresh CoreValidation cold/warm 171/171 each remain
bound to the reviewed implementation and input identities in the validation
record.

The refreshed CORE-F graph has no new implementation-ready owner/runtime/host
slice from this promotion. Selected-owner source/writer/epoch closure, shared
runtime and restore composition, detached restoration, and the .NET host with
deterministic continuation remain separately dependency-gated. Full CORE-F
remains `WAIT_DEPENDENCY`; this promotion changes no P12/P13 or CORE-X status.

## CORE-F institution and claim owners promoted — 2026-10-15

The bounded institution and political-claim owner extraction was promoted to
`codex/core/canonical` by fast-forward from
`b2c2c0e94c7a8dd9175a8db3fab4fb9d78dcfb2a` to
`06b0e4f047923bbaeee7080a8657981ab4e10ee0` (tree
`dd38559ee53a694ff82f4866f724f009366f2260`). The reviewed implementation and
test source is `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d` (tree
`e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`); the candidate validation and
evidence are at `c5d6babf15480b9ac44174ce43d11e1a2eabc397`. Independent
exact-tip review returned `VALIDATED_CANDIDATE`; its durable record is
[`CORE_F_INSTITUTION_CLAIM_OWNERS_REVIEW_C5D6BAB.md`](reviews/CORE_F_INSTITUTION_CLAIM_OWNERS_REVIEW_C5D6BAB.md).
The review-record successor changes no production or test source. Local and
remote `codex/core/canonical` synchronization was verified at the promotion
tip.

Final evidence: Unity focused `P12GPoliticalBindingRestoreTests` 11/11, ALL
EditMode 3179/3179, official Smoke 5/5, .NET CoreValidation 188/188 cold and
188/188 warm, and `git diff --check` PASS. Normalized Unity and .NET package
hashes and exact runner identities are recorded in
[`CORE-F Institution and Claim Owners validation`](validation/CORE_F_InstitutionClaimOwners/VALIDATION.md).
The standalone focused and Smoke filtered XML reports remain privately
retained because the strict packager rejects their full-discovery parent-suite
counts; no XML was modified. All five Smoke identities and pass outcomes are
present in the verified full EditMode report.

This promotion extracts only the approved institution and political-claim
owner sources, the shared property contracts required by that slice, adapters,
and required runtime call-site changes, including the reviewed restored-clone
revision correction. It does not complete CORE-F,
close the full selected Daily-v1 owner/runtime graph, authorize detached .NET
hosting or continuation, or change P12/P13, CORE-X, or any numbered Phase
status. Recompute the member/source/access ledger against this promoted graph
before treating the dependent shared runtime/restore design as implementation
ready; it remains `WAIT_DEPENDENCY` until that complete closure is proven.

## CORE-F source closure refreshed after institution/claim promotion — 2026-10-15

The post-promotion source graph is recorded in
[`CORE_F_SourceClosure_PostInstitution`](validation/CORE_F_SourceClosure_PostInstitution/POST_INSTITUTION_CURRENT_BASE_REVALIDATION.md)
and its row-level inventories. It uses the unchanged production source
`ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d` (tree
`e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`) now present in Core canonical
`2764b50b49c0773328ebcc56b2c668a2c609b7ff`, under architecture canonical
`797eb02f06fc5ca759a7e47283589d97a5efe73d`. The graph was regenerated from the
matching Unity 6000.3.9f1 compiler response and current Core project source list;
all 315 Unity source and 53 Core compile-input blob rows match the production
source commit. It reports 2,837 Unity source edges, 116 SCCs, a largest SCC of
190 files, 220 runtime-reachable and 289 restore-reachable Unity source files,
plus separate Core/Unity symbol-edge inventories. This is compile/declaration
evidence, not an owner or behavioral proof.

The source crosswalk accounts for 15 prior Unity application files moved to the
Core compile set and the one new Unity genealogy guard-binding source. The
promoted owner packages remain bounded. The 89-family owner/capture/target and
24-operation matrices still need exact current-source joins for each live
owner, constructor, supported writer/epoch, capture, stage, and test identity.
The remaining definition-bound owner graph, internal member/access cut, shared
application/session gate, v1 persistence integration for host-kind-v2,
detached .NET restore, fixed host fixture, and .NET T/T+1/T+2 proof remain
unimplemented or unproven as specified by the dependent design.

The source-closure artifact set and current-base revalidation passed
independent exact-tip review at `9a7117037ab418e041bd41296a700a1dc2c3b34d`;
the durable review is
[`CORE_F_SOURCE_CLOSURE_POST_INSTITUTION_REVIEW_9A711703.md`](reviews/CORE_F_SOURCE_CLOSURE_POST_INSTITUTION_REVIEW_9A711703.md).
The review confirms the inventory/blob joins and graph derivations, while
retaining the evidence boundary below.

The first post-promotion owner/member/access join is recorded in
[`POST_INSTITUTION_OWNER_MEMBER_ACCESS_JOIN.md`](validation/CORE_F_SourceClosure_PostInstitution/POST_INSTITUTION_OWNER_MEMBER_ACCESS_JOIN.md).
It resolves all 1,660 cited source references across the existing 89-family and
24-operation matrices to the exact promoted code tree and projects the current
selected Core/Unity symbol edges. Its generator verifies the complete Unity
source/symbol multiset, and a same-count substitution is rejected. The existing
matrices still do not directly bind each family to its post-migration owner
declaration, member, constructor, live instance, and writer/hydrator path. This is partial
source identity evidence, not semantic closure.

Accordingly, `CORE-F-Shared-DailyV1-Application-and-Host` remains
`WAIT_DEPENDENCY`; this refresh does not make it `READY_FOR_IMPLEMENTATION`.
The next authorized technical task is to finish the family-to-declaration,
member, constructor, live-instance, writer/epoch, and restore-target mapping,
then obtain independent review of that completed join before implementation
readiness. No P12 profile, numbered Phase, or CORE-X status changes. At that
earlier docs-only evidence refresh, no production or test source changed.

## CORE-F current selected-owner source/access closure audit — 2026-10-16

Core canonical remains `1998974515b6357c308d861fb608f2db5071a63a`; the
production/test source identity is unchanged at `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`
(tree `e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`). The current closure
addendum and corrections are in
[`CORE_F_SourceClosure_PostInstitution`](validation/CORE_F_SourceClosure_PostInstitution/POST_INSTITUTION_OWNER_MEMBER_ACCESS_JOIN.md).
The historical owner matrices remain intact; the current 89-family section
anchors are in `owner-current-source-anchors.csv`, and stale/wrong joins are
corrected additively in `OWNER_CURRENT_JOIN_CORRECTIONS.md`.

The refreshed executable evidence check reports 89 matching owner inventory,
writer, and capture/target keys; 24 unique operation IDs; 1,660 source
references across 142 paths with no unresolved blob; and a passing selected
Daily-v1 live-owner identity assertion. That assertion compares every
registered section ID with an independently assembled owner map and checks the
exact live owner instance by reference identity. The earlier validation applied
to the unchanged `ca55c3f` source/test tree at the time; the selected-profile
test was subsequently strengthened and rerun as recorded below.

The 89-family inventory/capture key joins, current source/blob anchors, and
selected-profile section-to-live-owner identity are evidenced. This does not
close every runtime-member/constructor/access join. Shared runtime/restore
remains `WAIT_DEPENDENCY` because the member-level
compiler/access cut is still open and 18 Required owner families have no
normal selected Daily-v1 writer or approved exact-empty disposition: eight
P16/P17-gated armed-force/manpower/conflict/war/battle rows; the two political
knowledge/decision rows whose only populated evidence is synthetic P12-G
restore; seven per-NPC site/local-topology/adventure knowledge views; and the
Expedition row, whose contract explicitly preserves payload without a hard-zero
or invented operation. No producer or false epoch claim is added.

The specific typed-definition/source gap is current and executable: the
selected Unity startup constructs `SimulationRuntime` from Unity-side
`CityRuntime`/`NpcRuntime` and authoring data, while the existing
`DailyV1DefinitionAdapter` is called from `SimulationGenesisPipeline` to create
`DailyV1EffectiveDefinitions` for profile fingerprinting, but the selected
startup does not pass that graph to `SimulationRuntime` or owner constructors.
Runtime owner constructors continue to use
Unity-dependent model/serialization members. The refreshed compile inventory
contains 315 Unity sources, 53 Core compile inputs, 220 runtime and 289 restore
source files; its selected projections contain 2,582 Core symbol references
(830 distinct symbols) and 288 Unity symbol references in 67 files. A new
Roslyn call-site map binds all 42,625 selected Core/Unity symbol references to
source line, caller member/accessibility and declaration where available. Its
projection exactly reconciles 2,582 Core and 288 Unity source/symbol pairs.
The remaining gap is to assign each callsite in split sources to its approved
same-assembly Core member, Unity adapter, or compatibility-helper destination.

The final call-site integrity/privacy check reproduced 42,625 rows and the
recorded raw SHA-256 with no workstation/home path or credential-like marker.
Tool source SHA-256 is
`2931dab3ea6e16fa00d95fae589c67f7f4f7a8873aab746b41b2978b94fc57f7`, project
SHA-256 is
`ba5fb505ce9124c1cbc443051a2d7f06fcf940d13cee4b1639f85c8def67f41c`, and
manifest SHA-256 is
`c9eaf9b89144d58618b22f2d6d8651547d9d34f63be5de8f991240dc075effad`. The
compressed map SHA-256 is
`2898b762de97b6134ece33f8cdad035a1aa4f30f3026852850b1913e38919e25`; the
decompressed CSV SHA-256 is
`2702d6d14e33fb7da6a56beda6cdecdbf71f1559fc951e48b2ae975536305d47`.

No shared runtime/restore `READY_FOR_IMPLEMENTATION` claim is made. The
approved entry rule requires a writer/epoch path for every admitted owner; 18
selected Required families currently lack a normal Daily-v1 writer, so their
exact-zero baseline evidence does not satisfy that dependency. Do not invent
producers or reclassify owners. Remaining bounded work is to complete the 89
family-to-member/constructor/capture/restore joins, assign the 67 split files'
member-access destinations, and bind typed definitions to selected runtime
constructors. No P12 scope or numbered Phase status changes are introduced.

### CORE-F selected-profile evidence update — candidate `cb31d4d`

Core canonical remains `1998974515b6357c308d861fb608f2db5071a63a`; this is a
local source-closure candidate, not a Core canonical promotion. Production
source remains `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`. The only
production/test-source delta is an EditMode assertion in
`SimulationBootstrapCompositionTests.cs`, committed at
`cb31d4d6f777a78af25670e3e6d43f01578b8809` (candidate tree
`07a5a406985f83a672007a82fe126048dd30b26d`). No production behavior changed.

The selected `UnityBootstrap-Daily-v1` admission test now asserts exact-zero
cardinality and revision at the initial profile boundary for all seven Required
per-NPC site/local-topology/Adventure Intel views, alongside the existing exact
live-owner identity checks. Those families remain Required and may contain
nonzero restored/later payloads; the new baseline check is not a hard-zero
restore rule or a writer/epoch claim. The source-join checker enforces these
seven assertions. Current generator result: 89/89/89 owner keys, 24 operations,
1,660 source references across 142 paths with zero unresolved blobs, live-owner
identity PASS, and selected Required Knowledge zero-baseline PASS.

Unity validation against the exact candidate tree passed: focused admission
test 1/1; full EditMode 3,179/3,179; filtered Smoke 5/5. The full EditMode
archive verified against its private run manifest and is stored in
[`CORE_F_SourceClosure_PostInstitution/Evidence`](validation/CORE_F_SourceClosure_PostInstitution/Evidence).
The full archive includes the selected-profile case and all five supported
Smoke identities once each, all Passed. `git diff --check` passed, and the
validation input snapshot was stable. Focused and filtered reports/logs remain
in private local retention; no filtered result was edited or published.

The initial exact-zero baseline is now executable for 17 Required families
without a normal Daily-v1 writer (eight P16/P17 armed-force rows, two political
rows, seven per-NPC Knowledge views). The Required Expedition row remains
preserve-only, with no hard-zero claim. This closes an evidence gap but does not
decide whether the existing profile baseline is an approved no-writer
disposition under the runtime/restore entry criterion. The member-level
constructor/access cut and typed-definition consumption gap also remain open;
Shared Daily-v1 Runtime/Restore stays `WAIT_DEPENDENCY` and is not
`READY_FOR_IMPLEMENTATION`.

### CORE-F selected call-site access graph — current evidence

The exact `Assembly-CSharp.rsp` hash matches the preserved Unity compiler
response; all 315 production-source blobs match the pinned source inventory.
The Roslyn output
[`selected-member-access-sites.csv.gz`](validation/CORE_F_SourceClosure_PostInstitution/selected-member-access-sites.csv.gz)
contains 42,625 selected call-site rows (39,305 Core; 3,320 Unity) and
reconciles exactly to the 2,582 Core and 288 Unity file/symbol pairs. It names
the source line, caller member/accessibility, referenced member/accessibility,
and declaration path/line when available. Runtime/restore source inventory
classifies 222 files for `CoreMove` and 67 for `SplitHostDomain`.

This closes the member-level citation gap, but boundary assignment is still
open for the 67 split files. The approved §14 design remains the rule: shared
semantic members move together within Core; Unity calls remain in a thin
adapter or use an already-approved host-neutral helper; public host access is
only through `DailyV1Application`. The complete current join must also bind all
89 owner rows to concrete owner members/constructors, mutation revision/epoch
callbacks, capture/stage/hydrator targets, and the approved no-writer profile
dispositions. Shared Daily-v1 Runtime/Restore therefore remains
`WAIT_DEPENDENCY`; this evidence does not authorize source extraction or widen
the public API.

## CORE-F owner-entry qualification — selected Daily-v1 evidence (current review candidate)

The refreshed test/source candidate is `350d0aaa44f657379da9e1009838896a3da9ffef`
(tree `ff290a1b6daa6b68a5e4f4b312a32fca467d3aea`), based on Core canonical
`1998974515b6357c308d861fb608f2db5071a63a` and the owner-entry contract at
architecture `512cf5721927a5b3d6766bc4d19cfd62a6729a41`. The reviewable
qualification and validation record is
[`CORE-F Owner Entry Qualification`](validation/CORE_F_OwnerEntryQualification/VALIDATION.md).
The previous candidate and validation packages remain historical evidence;
they are not the current validation baseline. The refreshed test-only candidate
addresses the exact-tip review findings by exercising split-alias rejection at
the integrated G restore boundary and accurately naming the paired P12-D
place/connection exclusion fixture. No production source changed.

The current evidence joins 89/89 owner-entry chains, preserves all 18
dispositions as `Required`, and maps 18,270 row-linked references from 67 split
host files into 3,365 fully assigned destination groups. The member-access
projection contains 42,625 callsites, including 2,582 resolved Core declaration
pairs and 288 Unity-engine pairs. The post-institution source join passes all
89 owner/capture anchors and resolves 1,660 references across 142 paths. The
generated chain ledger now anchors current declarations to the actual Core
canonical source blob/line and retains the prior production-base citation in a
separate historical field. The anchor input contains 89 unique families and is
verified against the canonical baseline, pinned source reference, and candidate
working tree.

Unity `6000.3.9f1` / Test Framework `1.6.0` validation against the exact
candidate tree passed focused restore 15/15, full EditMode 3,184/3,184, and
official filtered Smoke 5/5, with zero failed, skipped, or inconclusive cases.
All three runs share the same stable 1,668-file input digest. The full
normalized result package verifies against its protected original and contains
all focused and Smoke case results. Previously approved CoreValidation
cold/warm evidence is reused because its full 142-input digest exactly matches
the current CoreValidation input set. The refreshed exact-tip independent
review is pending; the prior review of the superseded evidence tip requested
the two test-evidence corrections now recorded in this candidate.

The evidence qualifies the approved owner/source/access and grouped-restore
claims; it does not alter production source, create a writer, widen Daily-v1,
or prove that every selected runtime source is already movable. The promoted
owner-entry contract supersedes historical State wording that treated a normal
writer or unconditional exact-empty disposition as a prerequisite for every
Required family. The 18 families remain Required and are qualified using their
accepted preserve/restrict and supported-operation rules.

Shared Runtime/Application/Host remains `WAIT_DEPENDENCY` pending the explicit
post-qualification source, definition-binding, and integration-readiness
reassessment under the existing CORE-F runtime/restore design. This evidence
alone does not authorize a source move or establish detached .NET host,
persistence, or deterministic continuation readiness.
