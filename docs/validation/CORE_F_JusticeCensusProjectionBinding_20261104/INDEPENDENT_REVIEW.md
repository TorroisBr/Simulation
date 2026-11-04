# Independent Candidate Review — Justice Census Projection Binding

## Verdict

**VALIDATED_CANDIDATE** for the bounded Justice census projection-binding change. This verdict is not a Core canonical promotion or CORE-F completion.

## Exact identities

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Code candidate: `e466605bbe768d2e32ed31b51875e7095605974e`
- Candidate tree: `45e8b4691148e6ae286c6b8e7acbbef3ffd5b6a3`
- Base: `1e3cbc8dbb4c0cfc4ca10a3762d158ec3170ff53`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Scoped four-file binary diff SHA-256: `4e8e4b3a0850b326485446f835109daa9a980c703e9778aef382692577126fb7`
- Independent reviewer: read-only review agent `/root/review_guard_arrest_p12`

## Findings and checks

Core checks the Justice records and P18 receipt rows only when exact Core-owned census sources are active. The Unity host supplies the projection identity only when its `JusticeSystem` and `CoreOwner` reference the exact selected graph owner. Core validates the required row schema, role, identity, cardinality, and revision; rejection clears output rows and the mutation epoch. The same Core validator is used for normal admission and restored-vector capture. The existing mixed/legacy path remains gated by the prior source-capability flag.

The reviewer found no correctness blocker. The XML summary comments are attached to their corresponding methods and parse as one summary each (18 opening and 18 closing tags in the file).

Coverage includes Core admission rejection for missing and duplicate Justice rows, wrong schema/role/identity/cardinality/revision, plus existing exact-owner restore recapture and failure tests. There is no separate malformed-Justice-row negative test specifically at the restored-vector call site; that path invokes the same validator exercised by the admission matrix.

## Validation evidence

- CoreValidation: 1,013/1,013 cold and 1,013/1,013 warm; runner manifest reports stable inputs and a successful build. Evidence identities and private originals are documented in [`VALIDATION.md`](VALIDATION.md).
- Unity EditMode: 3,299/3,299; official Smoke: 5/5.
- The normalized Unity package passed the repository verifier: one run, one XML entry. The public record and package contain no local workstation paths.
- `git diff --check`: PASS.
- The source-text change after validation only relocates an XML summary comment; no executable C# statement or test changed. The validation record explicitly identifies the tested source snapshot.

## Integration constraints

This candidate adds only exact Justice census binding to capture. It does not complete CORE-F, claim detached .NET execution, change P12 profile scope, or advance Core canonical. `ProjectSettings/ProjectSettings.asset` and the pre-existing untracked guard evidence were outside the candidate commit and remain untouched.