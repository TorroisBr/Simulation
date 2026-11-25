# Independent exact-tip review — CORE-F restore-stage orchestration

**Verdict:** `VALIDATED_CANDIDATE` — PASS, no actionable findings.

## Identity

- Base: `ec357a6ea833fb6965e92153b28ad3ce0bdd439d`
- Reviewed candidate tip: `44388fd8fc0da0fe246a28c8ecdeb5987b593983`
- Candidate tree: `6eee4c5f65e6458df3c9c8cfac3929fb39e753c5`
- Reviewed code/test commit: `f4c9c0260ad884856990f1909b04caf1b8b12196`
- Reviewed code/test tree: `eb6b97f9ee3e148c697e0bceae360032fd42d51c`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Canonical is an ancestor of the candidate base.

## Findings

The Core restore operation stages P12-D, runtime authorities, P12-E, P12-F, then composes the Core session. Unity projection callbacks consume the completed owner stages; the restore coordinator retains the exact Core session and calendar references. P12-D binds the decoded site snapshot to the exact empty, revision-zero site owner and rejects populated site state. The existing application gate remains the only candidate publication path; failed staging or commit does not publish the candidate. Stage observer order remains compatible with the existing projection sequence.

The implementation does not add P10 site content or claim detached .NET host readiness. Full CORE-F remains incomplete; detached .NET hosting and separate-boundary T/T+1/T+2 continuation parity remain outstanding.

## Validation evidence checked

The candidate validation record and referenced artifacts correspond to the reviewed code commit/tree:

- CoreValidation: cold and warm 1,010/1,010; exit 0; stable inputs; Library absent. Protected original manifest and TRX hashes match the record.
- Unity focused restore: 1/1; P12 restore: 16/16; owner vector: 16/16; official EditMode Smoke: 5/5; full EditMode: 3,299/3,299. Runs passed with stable inputs. Protected manifests, XML, and log hashes match the record.
- The sanitized full EditMode package verifies; ZIP SHA-256 is `c01a9346898933a4d72d4b5bb2ce9efc9179aa608bcc25e784eea0f22a809e10`.
- Apparent text-hash differences were newline representation only: the Git blobs are LF while the Windows runner inventories are CRLF; converting the candidate sources to CRLF reproduces the recorded source hashes. The public JSON manifest is LF-normalized while the Windows working-tree hash is CRLF; the exact recorded CRLF manifest hash is `0a5bc630de90f1fa98a779f459f053fb015748338cd6b3b93fc429c4b493bf80`.
- `git diff --check` passes. The implementation diff contains no ProjectSettings files.

The exact-tip reviewer performed no edits and did not rerun the suites.
