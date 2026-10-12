# Independent Review — Evidence Packer File-URI Path Policy

Date: 2026-10-12

## Review target

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/phase12/P12GPoliticalBindingCompletion
- Candidate commit: 32341e71510e5c9d4b1c6eb9b878bb1eb286e359
- Candidate tree: 27bd29e9a0fbe5f331367f0d8be13fab57d5606e
- Actual base and current P12 canonical: eef376d51f36641918a13b2e937e704051814820
- Reviewed module blob: 2e2bde5f3b51f465dca65bc2513e7bf2a8bd6c22
- Reviewed test blob: 3cf4d911e39e3b75589f96e4f7f6d75814c3d0d5
- Applicable evidence policy: 58157f9ac7bf573e9a4ed1b2d485a2db190e7c4f (separate architecture canonical ref; not an ancestor of the P12 base)
- The candidate branch was local-only during review; no corresponding origin branch ref was present.

## Result

**PASS for the bounded URI/path-policy code delta only.** No blocking code finding remains for this change. This is not a whole-candidate validation or promotion decision.

## Findings and limits

Compared with the immediately preceding candidate 1fcfa4179821471ff898aba4a2cfdfb82a43fab6, the code delta is limited to the evidence module and its packager tests. The Assembly path classifier now recognizes case-insensitive file: values as absolute. Such a value proceeds to the existing supported-path check, which allows only the declared Windows Unity editor assembly suffix; an unsupported file URI therefore fails closed instead of being treated as a report with no absolute path. The public evidence scanner also rejects file-URI values wherever they occur in retained text.

The existing Windows Assembly-suite projection remains narrowly scoped to the Assembly fullname: it replaces only the machine root, preserves the assembly-relative identity, and checks semantic equivalence of the result. Unsupported Assembly suffixes, path-like test-case identities, multiple absolute Assembly paths, and other local paths remain rejected. The prior Windows projection behavior is unchanged by this delta.

The test changes add three assertions for public-text scanner rejection, direct Assembly policy rejection, and end-to-end package rejection of the localhost file-URI form. Existing coverage still verifies the supported Windows projection, preserved identity attributes and hierarchy, semantic hash equivalence, and package verification. The file-URI variants have finite coverage; this review does not claim exhaustive testing of every URI encoding.

The recorded revalidation reports 64 packager assertions, wrapper self-tests 33/33, git diff --check PASS, and unchanged Assets tree a61dad9e1755c28c87fc3f7e01e6e30bb1f9964f. The 64 count is consistent with the prior 61 assertions plus the three new assertions. The revalidation note was worktree-local and untracked at review time, so this record does not represent the note or underlying test logs as candidate-committed artifacts. Unity was not rerun because the Assets tree is unchanged and the delta is limited to tooling and its self-tests.

## Candidate-level blockers and authorization boundary

The P12-G VALIDATION.md fingerprint-label mismatch remains unresolved; this review does not clear it. The current closure matrix also still reports 61 packager assertions while the local URI revalidation note reports 64. Reconcile those records before relying on the combined validation narrative.

This review does not authorize publication of the candidate, canonical promotion, P12-G completion, P12-A readiness, P13 readiness, or Phase 12 closure.
