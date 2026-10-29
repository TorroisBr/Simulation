# CORE-F P12-E Core owner source capture — validation

## Candidate and scope

- Reviewed implementation: `cecc8044f0db68dda179b98d3dc5d89a72530bbf` (tree `88342316f716bd8d1d32e7bcd18a6f9a24e3a0f9`).
- Parent candidate: `84b1a723f48ed1bf2207f501aedf2bce3ab59916`.
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Scope: move the selected P12-E Core-owner source capture into `Simulation.Core`, bind selection to the token's exact census identities, and revalidate the continuation-save boundary around capture. The existing Unity staging-attempt bridge remains freshness-only for legacy image staging; it does not select owners or export owner facts.
- Owner schemas, persistence bytes, P12 profile, or supported owner set are unchanged. This is a bounded capture/source-boundary milestone, not completion of P12-A or CORE-F.

## Validation

| Gate | Result | Run |
|---|---|---|
| CoreValidation cold and warm | 926/926 PASS each | `c63d763c34cf4d73895a8efa03dcba06`; private manifest SHA-256 `17A038AA1F1843EA504E141E0716474A9FD6E9A41B50C0556A8F5CC83C6F2FDE` |
| P12-E focused | 6/6 PASS | `9de7a93f30f840a19f0c094d54357b01` |
| P12-G restore regression | 15/15 PASS | `743b84e51e1541ae845b2fc3134bb2f9` |
| Closed Core operation fallback regression | 3/3 PASS | `099ece1eecf7493daa0f84b1b7542fff` |
| Full Unity EditMode | 3279/3279 PASS | `8e1912e9566f49d7a4eaf8c2d5d54111` |
| Official filtered Smoke | 5/5 PASS, zero failures/skips/inconclusive | `761e8b3ec9104f41a553f17996e457f9`; exact selection `EditMode`, filter `Smoke`; private run-manifest SHA-256 `8BA0056FC4E7172C9C74E578421A52870ACB981C4C211042DDC2B268E696C2FB` |
| `git diff --check` | PASS | Reviewed source commit |

The full EditMode input snapshot was captured immediately before the source commit; the Smoke snapshot was captured immediately after it. Both stable snapshots contain the same 1,972 project file paths and identical per-file SHA-256 values. The independent reviewer also confirmed the changed project source hashes match the committed candidate. The different Git `HEAD`/tree metadata reflects only the commit of those already-tested bytes.

The official normalized full EditMode package is included in `UnityEvidence/`. The package verifier passed: manifest SHA-256 `42E15E86698AE9E84239BC6436D73C57A662E31AAD6086EBB7BB6B470326DD19`; archive SHA-256 `CC114AACD16DD53C7625309E8DAE296CA373F138CF89DE8F50722BDE63B07A1C`. The complete report contains the five Smoke test cases, all passed.

The separate Smoke XML and log were independently checked against the protected run manifest and their hashes match. They remain private and unchanged: the official package tool rejected that filtered XML because Unity's parent `Simulation` suite reports a stale aggregate `testcasecount`. No XML was edited, normalized by hand, or claimed as packaged. The earlier PlayMode selector that executed zero tests is excluded from passing evidence.

CoreValidation's original TRX and logs remain in owner-only private retention. No normalized .NET TRX package is claimed because the approved adapter does not accept this runner's manifest schema.

## Status

The candidate is pushed on `codex/core/CORE-F-core-owned-runtime-application`. No canonical promotion or full CORE-F acceptance is claimed. Existing ProjectSettings edits and unrelated/private untracked evidence were not staged.