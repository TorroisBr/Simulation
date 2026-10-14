---
name: candidate-review
description: Independently review a submitted simulation checkpoint candidate against its actual base, architecture, technical design, tests, and replay-sensitive boundaries.
---

# Candidate review

Use after a worker submits a bounded candidate or integration candidate. Reviewer must be independent of the candidate's author and must not edit the candidate while reviewing.

1. Verify the full candidate SHA, exact base, branch, current canonical SHA, clean checkout, and whether the candidate tip changed after tests or earlier review. Read current architecture, owning Brief/State, checkpoint contract, reviewed technical design, and required validation gates.
2. Inspect the complete diff against the actual base, not a convenient branch comparison. Check scope, authority, mutation/stale/atomic behavior, deterministic ordering/randomness, knowledge boundary, reconstruction-sensitive state/inputs, temporal identity/cardinality assumptions where applicable, hotspot overlap, and test coverage.
3. Verify validation evidence belongs to this exact tip and includes the required focused, regression, full-suite, Smoke, and diff-check gates. Do not infer correctness solely from green tests. A changed code tip requires affected validation and a fresh review; classify newer-canonical impact using `dependency-refresh`.

Record the review result durably, with `REJECTED`, `NEEDS_CHANGES`, or `VALIDATED_CANDIDATE`, precise evidence, full base/candidate/current-canonical SHAs, test gaps, and integration constraints. `VALIDATED_CANDIDATE` alone does not promote code: the separate canonical-promotion workflow must still verify every condition in the autonomous bounded-promotion policy or surface a genuine human gate. Stop and report unresolved architecture/product decisions instead of resolving them in review.

## Evidence retention and reuse

Check the complete retained results, manifest hashes, full validation-input identity and current Phase gates under docs/operations/VALIDATION_EVIDENCE_POLICY.md. Verify original-to-retained case/hierarchy/result equivalence when normalization is used; hashes without accessible results are insufficient. Review the full candidate against its actual base. For a metadata-only successor, the same independent reviewer may attest exact final-tip applicability against the prior full review and unchanged inputs. No author self-approval or weakened suite gate.

## Core/host migration review

Check actual compiled/transitive dependencies, one authoritative source owner,
SDK/asmdef integration, serialized asset linkage, exact cross-host case mapping,
authoritative parity and P12 build/profile compatibility boundaries. Inspect
both host gate results and their complete input/toolchain identity. The current
Unity packager supports only its delivered report formats; use the bounded
handoff's approved evidence path, never assume a proposed TRX adapter exists. A pure compile alone is not a portable world host.

## TRX normalization review

Use docs/operations/DOTNET_TRX_EVIDENCE_CONTRACT.md for the bounded supported path.
Verify source originals, ACLs, native/derived counts, full case graph, immutable
failure payload, explicit metadata projection and source/assembly binding. Check
complete source-to-derived equivalence independently of aggregate/hash claims.
Distinguish archive integrity from original-comparison proof and retained local
files from durable public evidence. Bind original/tested refs and packaging-time
tool refs separately; no retroactive acceptance or fabricated rerun.
