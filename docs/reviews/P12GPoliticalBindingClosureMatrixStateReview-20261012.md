# P12-G State, Matrix, and URI Revalidation Docs Review

Date: 2026-10-12

Status: **NEEDS_CHANGES**

## Exact review target

- P12 canonical at review: `eef376d51f36641918a13b2e937e704051814820` (`origin/codex/phase12/canonical`, fetched immediately before review).
- Candidate branch: `codex/phase12/P12GPoliticalBindingCompletion`.
- Exact documentation candidate: `8fb76b13885c56266d9221f6617b4732a2292621`, parent `32341e71510e5c9d4b1c6eb9b878bb1eb286e359`, Git tree `f4dfa89f9678fa4478afcefb21c606fe8b8698aa`.
- `eef376d` is the merge base and a clean ancestor of the candidate. The docs delta changes only `docs/PHASE12_STATE.md`, `docs/validation/P12GPoliticalBindingClosure/CLOSURE_MATRIX.md`, and adds `docs/validation/P12GPoliticalBindingClosure/PACKAGER_URI_REVALIDATION.md`. `git diff --check` passes for the exact delta.
- Reviewed document blobs at `8fb76b1`: State `8fd13dfc6140cd95715f38f4d02ad1e7df07e04b`; matrix `18e41a4cc55b82bff3083351dafd1f04b9f9fdb3`; URI revalidation note `85d81ee198c8dbaed407079db28c5e67e73c3053`; unchanged `VALIDATION.md` blob `605655cebec63a2a2cb9f1f5795d0044e05e7f8a`.
- Underlying tool-code tip: `32341e71510e5c9d4b1c6eb9b878bb1eb286e359`, tree `27bd29e9a0fbe5f331367f0d8be13fab57d5606e`; its `Assets` tree is `a61dad9e1755c28c87fc3f7e01e6e30bb1f9964f`.
- Unity-validated source: `a01d73297228c6ee466bb01a636ca0bcdecc9f68`, Git tree `de049a4ff5efb78845ce643080ee056787f2cf20`; coordinator blob `e1ccf80c05fc3e3b8e2ec739376bd78b1f208ea2`; political test blob `2960505666ce4e1451120c9ef1d010059bb0a217`.
- URI/path-policy tool delta at `32341e7`: evidence module blob `2e2bde5f3b51f465dca65bc2513e7bf2a8bd6c22`; packager test blob `3cf4d911e39e3b75589f96e4f7f6d75814c3d0d5`. Its independent exact-tip review record is `0a41988fec13b0d036c4e286c8ce2f83f5cd7d89`, based on P12 canonical `eef376d`.
- Contract authority: General Architect handoff `f3077ac5decbbb726a7e3ab80fc098fa4a8689fb`, adopted against architecture baseline `58157f9ac7bf573e9a4ed1b2d485a2db190e7c4f`.

This review covers the committed State/matrix/URI-revalidation documentation delta and verifies cited code/evidence identities. It does not review the full P12-G implementation candidate or grant canonical promotion.

## Reviewer independence and method

I did not author the State, matrix, or URI-revalidation note. I previously implemented the bounded political fixture/coordinator seam, so this is an independent documentation review only, not an independent implementation review. I inspected the exact document delta against the adopted handoff, checked the current section's linked evidence and cited test methods against the committed source, verified object identities and ancestry, and checked the exact docs diff. No candidate or protected file was edited. No Unity tests were run because the reviewed delta is documentation-only.

## Review findings

- The State accurately records the bounded fixture and packager scope, correct code and source identities, the updated Window 2/3 evidence crosswalk, and the limits on the synthetic political source. It keeps P12-G `WAIT_DEPENDENCY`, P12-A `WAIT_DEPENDENCY`, P13 `BLOCKED`, and Phase 12 `OPEN`.
- The matrix follows the adopted handoff's finite evidence plan. Window 2 joins the selected 299-row Daily-v1 inventory to source providers, target guards, exact-empty/alias dispositions, typed rejection, temporal rejection, reachable partial-mutation failures, recapture, admission, and retained publication evidence. It does not expand to arbitrary callers or future profiles.
- Window 3 identifies the named admitted causal classes and maps the enumerated typed C–F projection and boundary/root facts to continuation evidence. It distinguishes synthetic political bindings from ordinary Daily-v1 political history, preserves exact-empty and omitted/derived read-model dispositions, and does not claim runtime-wide quiescence, universal epoch coverage, storage compatibility, or future-profile parity.
- The matrix links and the new URI-revalidation note's references resolve in the candidate tree. The active State section's evidence links resolve, and cited test methods are present at the stated committed source tip. The older State history remains explicitly historical and does not override the active matrix.
- The URI revalidation note identifies the exact tool-only code delta and source blobs. It records 64 packager assertions, 33/33 validation-harness checks, the exact-tip tool review, the unchanged `Assets` tree, and the reason Unity was not rerun. It keeps the original 61-assertion result separate and does not rewrite historical evidence.
- The State and matrix correctly state that the URI-policy review is limited to its tool delta and does not clear the full candidate's validation-record blocker.

## Required correction

The unchanged `docs/validation/P12GPoliticalBindingClosure/VALIDATION.md` rotates three source identities. Comparing its labels with the Git objects at the validated source confirms:

| Field | Correct Git identity | Value currently labeled in `VALIDATION.md` |
|---|---|---|
| Validated `Assets` tree | `a61dad9e1755c28c87fc3f7e01e6e30bb1f9964f` | `e1ccf80c05fc3e3b8e2ec739376bd78b1f208ea2` |
| Coordinator source blob | `e1ccf80c05fc3e3b8e2ec739376bd78b1f208ea2` | `2960505666ce4e1451120c9ef1d010059bb0a217` |
| Political test source blob | `2960505666ce4e1451120c9ef1d010059bb0a217` | `a61dad9e1755c28c87fc3f7e01e6e30bb1f9964f` |

This remains the sole finding requiring correction for this review. The active State assigns these identities correctly, but the validation record must be reconciled and independently checked before full-candidate exact-tip review/publication can pass. Preserve the existing test results and package contents; a label-only correction does not require Unity reruns if the executable and test-source trees remain unchanged.

## Verdict and boundaries

**NEEDS_CHANGES**, solely for the `VALIDATION.md` source-identity label rotation above. The exact State/matrix/URI-revalidation documentation delta passes this review. No readiness or scope changes follow: P12-G remains `WAIT_DEPENDENCY`, P12-A remains `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains `OPEN`.
