# CORE-F Restored Owner Vector Contract Review

## Verdict

PASS — VALIDATED_CANDIDATE

Independent exact-tip review was completed for candidate
177a308a37f51ceeddf285a4e02c7e536952b966, against candidate base
e54a3eaf50761ed343970cdb20b6863de49440e0. The reviewed implementation code
commit is acabfc1efaa8693355502392bfa6c82926be6e55, with Git tree
f1fbe020b6e341a125df1f594788cbb41be0b3e6. Core canonical at review time is
a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.

The review was performed by a separate reviewer who did not implement the
change. No actionable findings remain.

## Finding and correction

The earlier owner-vector implementation rejected source/target identity reuse
only within corresponding sections. A target owner in one section could reuse
a live source owner from another section, contrary to the detached-graph
requirement in docs/architecture/CORE_F_RUNTIME_RESTORE_INTEGRATION_DESIGN.md,
section 4.

The candidate now compares every source owner reference with every staged
target owner reference using ReferenceEquals and rejects cross-section reuse
with the existing ReusedSourceOwnerIdentity result. The added regression
RestoredCandidateVectorRejectsSourceOwnerReusedAcrossDifferentSections tests
source owners A/B against target owners C/A.

The review confirmed that this check preserves section/schema/role/cardinality/
revision checks, exact-empty and excluded-owner rules, duplicate/missing
identity rejection, per-graph alias equivalence, recapture identity/vector
checks, and epoch equality. The Unity-facing wrapper delegates to Core, and
the explicit Core project compile entry is present.

## Validation evidence

Validation is tied to code commit
acabfc1efaa8693355502392bfa6c82926be6e55 and tree
f1fbe020b6e341a125df1f594788cbb41be0b3e6.

- Focused owner-vector tests: 16/16 PASS. This run preceded the code commit,
  but the source and test files were byte-identical to the committed tree.
  Run: 5039aaa2cb8344f99f9cd8bb396a8c53.
- P12-G restore regression tests: 32/32 PASS.
  Run: 414c7a1b37574eeebd876181c4e77991.
- Full Unity EditMode: 3260/3260 PASS.
  Run: 6b051a6994d24349a6760e5c4e607cd5.
- Official Smoke: 5/5 PASS.
  Run: 3c50d505c09c413899f1042d3c28ce1d.
- CoreValidation: 704/704 cold and warm PASS.
  Run: 6cbd3d6bb7224e249a04b06a062a2b75.
- git diff --check: PASS.

The Unity input digest for P12-G, full EditMode, and Smoke is
bfb5472a7bf1da04f57fadbb98a4cc997c7cda0fbceff3859a44a28bcaabcc3e.
The CoreValidation input digest is
a36eb3f4793efbaef7d2adaf0e6e611641f7a1ad86b59a7384e1bef813a3ae46;
its protected manifest SHA-256 is
5b97460323bf334f2f1feadfee546cef3897948e0e31cdcc5870d0599141447e.
The original CoreValidation reports and manifest remain in protected private
evidence storage. No public TRX package is claimed.

The verified official Unity EditMode archive is
docs/validation/CORE_F_CoreOwnedRuntimeApplication/Evidence_owner-vector-
disjointness-acabfc1/coref-owner-vector-disjointness-acabfc1-editmode.zip.
Its SHA-256 is
010720e5bf33c571e8d8bcd4fc51088e9659c2e3cb7c7717be37f0dfff31a871.
The package manifest SHA-256 is
e2aeb32c2448b4aa6ab92287dc09068859ac824e8b77fce334124a41bda4de65.
The official verifier returned VALID: runs=1 entries=1. Public files were
checked for workstation paths and credential-like patterns.

## Scope and status

The numbered Phase 12 is formally closed. P12-G remains promoted within its
recorded accepted scope; this CORE-F candidate does not reopen Phase 12 or
change any numbered-phase checkpoint status. It addresses a detached-owner-
graph requirement for the ongoing CORE-F shared restore/application work.

This candidate does not complete the shared runtime/restore package or CORE-F,
and has not been promoted to Core canonical. Core canonical remains
a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea. No P13 readiness or broader
persistence/profile guarantee is implied.