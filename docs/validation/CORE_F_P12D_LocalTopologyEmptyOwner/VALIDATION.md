# CORE-F — selected NPC LocalTopology exact-empty owner validation

**Candidate status:** validated implementation candidate only. This record does not promote Core canonical, close CORE-F, complete P12-D, or change P12-A/P12-G readiness.

## Scope

For the accepted `UnityBootstrap-Daily-v1` profile, the Required per-NPC `p12f.local-topology-knowledge.places` and `.connections` rows now share one immutable Core-owned identity witness. Both rows remain exact-empty (cardinality 0, revision 0); capture verifies the matching Unity serialization projection is also empty and belongs to the same NPC runtime identity. The existing P12-D codecs and payload schema are unchanged. Unselected/legacy NPCs retain their existing LocalTopology owner path. P10 Ruin topology content, non-empty LocalTopology, and broader profile admission remain excluded.

The candidate changes only the selected Core NPC owner graph, selected census/capture bindings, and assertions that identify the selected row authority. The adjusted regressions continue to assert Required role, exact zero counts/revision, shared alias identity, projection emptiness, and strict rejection when the selected projection drifts.

## Exact source identity

- Candidate base: `ced294c0f2bd56b9b585f882e21bb01c23a1f177`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Implementation and test source tree: `d8828a60ad2488cabd34526071129809045206d5` (excludes this record and packaged evidence).
- CoreValidation worktree input digest: `bfdd4760dbb249fa9b9035dde0af37284be6a1a071abeb4ede6b3e1a275230ac`; runner reports inputs stable.

## Validation

| Gate | Result | Evidence run |
|---|---:|---|
| CoreValidation cold | 818/818 passed | `c49eed018553438990c49165c51793e4` |
| CoreValidation warm | 818/818 passed | `c49eed018553438990c49165c51793e4` |
| Focused bootstrap composition | Passed | `a0dbe9b89ecd42cdb88e7d160df1561a` |
| Full Unity EditMode | 3266/3266 passed | `d880644c1ba44026ac6699f399efd782` |
| Official Smoke, EditMode filter | 5/5 passed | `2c18c0cecd7145ec9c8598eba43e5a6d` |
| `git diff --check` | Passed | final pre-check before candidate staging |

The cold/warm CoreValidation manifest records 818 discovered and executed test identities, zero failures, stable inputs, and successful build, demo, and Lab gates. The Unity run manifests record stable pre/post inputs. The protected original XML, logs, and runner manifests remain unchanged in the owner-only evidence store, keyed by the run IDs above.

The official Unity evidence packager created and verified the sanitized full EditMode package in this directory. Its archive SHA-256 is `2e4ef418c6ca20dafc1c92813992dcc9d48a59fa40d54c861998f730074d3817`. All five separately executed Smoke test identities also occur in that full EditMode report. The packager rejects the filtered Smoke XML because its suite-level testcase count describes the full suite; the original 5/5 Smoke run is retained privately and no XML was modified or reinterpreted.

One earlier CoreValidation invocation was excluded: it started with a working directory different from this E: project and stopped before tests at `restore-lab-locked` because the relative Lab project path could not be resolved there. The accepted `c49eed...` run was subsequently executed with the E: candidate as its working directory and all temporary, package, output, and evidence paths on E:.

## Limits and integration notes

- This is a bounded Core owner/census/capture composition slice; P12-B and P12-A status are unchanged.
- It does not admit P10 LocalTopology values or alter existing persisted values/schema.
- No CORE-F completion or detached-host claim is made.
- The validation checkout had a Unity `Library`; the CoreValidation runner therefore makes no Library-independence claim.
- Existing `ProjectSettings` user changes were excluded from the candidate and remain unstaged.