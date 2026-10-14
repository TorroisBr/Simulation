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
`96135eb6a87e5217d169a56414a20d21e6649af6`; current canonical is
`c95752846a0ce0e1ebf985c4d79e0e17b02bb7af`. Current-baseline design
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
review record `cb98d3e99c9491ce2c34278516272763333b271e` is pushed. CORE-C
remains design-reviewed; its bounded implementation follows the documented
CORE-A and P14-B dependencies.

## Readiness boundary

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
CORE-C checkpoint is therefore `READY_FOR_IMPLEMENTATION` within the exact
reviewed P14-B extraction boundary. The implementation must still satisfy the
design's Unity numeric-baseline, shared Core/Unity parity, owner-adapter,
callback-order, P14-C regression, Market/transaction, ALL EditMode, official
Smoke, and applicable long-run gates before exact-tip code review and promotion.
No claim is made about P12 readiness, equivalent .NET P12 continuation, P13,
full world hosting, or broader simulation hosting.

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
