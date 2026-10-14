# Simulation Core extraction state

This file tracks unnumbered Core infrastructure checkpoints. It does not
change numbered Phase State or imply Phase readiness.

Architecture approval: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
Production base for CORE-A: `codex/phase12/canonical` at
`4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.

## CORE-A — Calendar Shared Assembly

**Current status (2026-10-14):** the promoted TRX packager produced and privately compared the cold/warm .NET package; package integrity and privacy checks passed. The original candidate `f0bcb3e873fcf32df92c9dc3bc1a1ad20dae77d4` received exact-tip `NEEDS_CHANGES` because its validation record named a manifest hash matching neither the committed blob nor the Windows checkout. The current-base integration is being prepared on `codex/core/COREA-calendar-current-integration` from `codex/core/canonical` at `c68ca7a9a51df178ba0b889096c762c3fd7f4034`; it corrects the representation-specific hash while preserving the implementation, source identity, tests and results. A fresh exact-tip integration review is pending. The promoted packager currently fails to expose the Unity/NUnit verifier after importing its TRX adapter; this separate CLI defect must be corrected and the retained Unity package reverified before CORE-A promotion. CORE-A is not yet promoted. See the [CORE-A validation record](validation/COREA_CalendarSharedAssembly/VALIDATION.md) and [normalized TRX evidence record](validation/COREA_TRX_EVIDENCE_V1/VALIDATION.md).

- Preserved source candidate: `codex/core/CORE-A-calendar` at `f0bcb3e873fcf32df92c9dc3bc1a1ad20dae77d4`.
- Current-base integration branch: `codex/core/COREA-calendar-current-integration`, based on `codex/core/canonical` at `c68ca7a9a51df178ba0b889096c762c3fd7f4034`.
- Calendar implementation commit: `e702cdf196703ead98d8d37ff818bd1c0d0219e4`.
- Parser remediation commit: `f725fa87c6d1259d822180b84856b747c9e02980`.
- Parser remediation tree: `bdb6b9b63532e780661477430099a835d2b4f086`.
- Scope: one engine-free `Simulation.Core` assembly for the existing calendar,
  date and age capability; Unity and .NET compile the same source.
- Promotion target: `codex/core/canonical`, currently at `c68ca7a9a51df178ba0b889096c762c3fd7f4034`; this integration candidate has not yet been promoted.
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

**CORE-A evidence update:** the promoted TRX tool generated and privately compared the final cold/warm package. Its source identities, results and privacy boundary are recorded in `validation/COREA_TRX_EVIDENCE_V1/VALIDATION.md`. The old-base package candidate received `NEEDS_CHANGES` at `451ba62b1419a3936abcc28bc60f14eaa17eecfc` because its manifest-hash label matched neither the committed UTF-8/LF blob nor the Windows CRLF checkout. The current-base integration corrects that label; fresh exact-tip review is pending. A separate promoted-tool module-load-order defect blocks current CLI verification of the retained Unity package; the stored results remain unchanged. No CORE-A code is promoted. CORE-A makes no P12-A, P13, cross-build compatibility, or broader .NET hosting claim. CORE-B still requires CORE-A promotion and current-baseline revalidation; CORE-C still requires CORE-A and its documented P14-B dependency.
