# Simulation Core extraction state

This file tracks unnumbered Core infrastructure checkpoints. It does not
change numbered Phase State or imply Phase readiness.

Architecture approval: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
Production base for CORE-A: `codex/phase12/canonical` at
`4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.

## CORE-A — Calendar Shared Assembly

**Status:** implementation and review remediation are committed. Independent
exact-tip review passed candidate `2062e616d5a6bc53a2f154f33e9f4887a44972fe`
and is recorded at `ad4d141d0ec00dee6b776d9373187173a330f0cc` on
`codex/core/CORE-A-calendar-review-2062e616`. Canonical promotion remains
blocked by the durable .NET result-evidence/privacy gate below. The
original cold/warm .NET TRX files are preserved byte-for-byte in protected
local evidence, but are not published because their metadata contains a local
account identifier and checkout paths. The required durable result-evidence
gate is unresolved. See
[`CORE-A validation record`](validation/COREA_CalendarSharedAssembly/VALIDATION.md).

- Candidate branch: `codex/core/CORE-A-calendar`.
- Calendar implementation commit: `e702cdf196703ead98d8d37ff818bd1c0d0219e4`.
- Parser remediation commit: `f725fa87c6d1259d822180b84856b747c9e02980`.
- Parser remediation tree: `bdb6b9b63532e780661477430099a835d2b4f086`.
- Scope: one engine-free `Simulation.Core` assembly for the existing calendar,
  date and age capability; Unity and .NET compile the same source.
- Promotion target: `codex/core/canonical`, to be initialized from current
  reviewed production state under the approved CORE-A handoff. It has not been
  created or advanced by this candidate.
- Numbered Phase canonicals were not changed.

The calendar implementation and test source remain unchanged by the parser
remediation. The first exact-tip review of `78011db2` found missing TRX run
identity/summary binding and an absent durable raw-report bundle. The parser
now requires and persists those fields; focused self-tests and a clean .NET
validation rerun pass. The first review's record is
`c4c73bae26ecbacdb9e761821f408534610622a2`. The fresh exact-tip review of
`2062e616` passed at `ad4d141d0ec00dee6b776d9373187173a330f0cc` and confirms
that the parser finding is resolved. The original reports remain protected
locally because publication would disclose private run metadata; the review
does not resolve this separate durable-evidence gate.

## CORE-B — Deterministic and Temporal primitives

Technical design candidate `d20c2352887184ccc964d7f9586407d00a727d32` received
independent PASS. Review record `96a7df8c2ebad6cf5ce3bfe2bde140f98c6e46dc`
is pushed. CORE-B remains design-reviewed, not implementation-ready, until
CORE-A is promoted and its production-baseline refresh is reviewed.

## CORE-C — Market numerical capability

Technical design candidate `e80ae27cdf597e9f551fe94dd22827f59636712a` received
independent PASS after correcting the prepared-route callback count. Durable
review record `cb98d3e99c9491ce2c34278516272763333b271e` is pushed. CORE-C
remains design-reviewed; its bounded implementation follows the documented
CORE-A and P14-B dependencies.

## Readiness boundary

CORE-A remains the first implementation checkpoint. CORE-B implementation waits
for CORE-A promotion and revalidation. CORE-C waits for its design corrections,
independent PASS, and then its documented dependencies. Full world hosting,
equivalent .NET P12 continuation, P13 and broader simulation hosting are not
claimed by CORE-A.

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

**CORE-A remains blocked at its separate durable evidence gate.** The final
cold/warm CORE-A package must be generated and privately compared using the
promoted tool, then recorded with its source/test identity and independent
applicability review. The CORE-A candidate, its existing execution evidence,
and original reports remain preserved; no CORE-A code is promoted by this Tools
delivery. No P12, P13, CORE-B, or CORE-C readiness is implied by this
promotion. CORE-B still requires CORE-A promotion and current-baseline
revalidation; CORE-C still requires CORE-A and its documented P14-B
dependency.

### Unity CLI verifier import-order correction

**Status:** promoted to `codex/core/canonical` by commit `fb2189e41b4ebfe51146be5c4f85fc5c58270a75`.

The exact implementation candidate is
`2bda6341f3c5e599064e10ccda569237b9aea445` (tree
`f1d69e18dcf52e4f686116575fceb1729f53b59b`). It loads the TRX adapter before
the Unity adapter so the shared CLI retains both command sets, and adds a
subprocess regression that invokes the Unity `Verify` entry point. Independent
exact-tip review passed at `7f0aeed1a036fe07dc6cd87f91b1c1652c099e8a` on
`codex/core/TRXUnityVerifierImportFix-review-2bda6341`.

Exact-tip validation passed: TRX evidence suite 144 assertions, Unity evidence
packager suite 65 assertions (including the real CLI regression), Unity runner
self-tests 33/33, and `git diff --check`. Archive-only CLI verification of the
existing TRX package passed with one run and two entries; it correctly reports
`originalComparisonNotPerformed`. The earlier `originalComparisonVerified`
result belongs to the promoted `c68ca7a9` tool and remains a separate preserved
result. The candidate makes no new private-context comparison claim and does
not alter simulation behavior or package contents.
