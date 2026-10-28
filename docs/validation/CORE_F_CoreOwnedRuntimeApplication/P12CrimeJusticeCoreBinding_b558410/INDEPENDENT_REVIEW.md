# Independent exact-tip review: Core-owned P12 Crime/Justice writes

## Verdict

**VALIDATED_CANDIDATE — PASS.** No actionable implementation or test-integrity findings on the reviewed six-file change. This review is candidate acceptance only; it does not promote the candidate or claim CORE-F completion.

## Exact identities and scope

- Repository: `TorroisBr/Simulation`
- Candidate branch/tip: `codex/core/CORE-F-core-owned-runtime-application` / `b558410f8d71ec5bc924a90cc310f4c3959a72a5`
- Candidate tree: `7b836dac891d5e2552e21e9e876f225f7c383605`
- Base: `a376b65588977c51ae84fcee0ab236464bc00177`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch ref was verified at the exact candidate commit; Core canonical was independently checked at the stated SHA.
- The complete candidate commit changes exactly six files: three Unity host/runtime files, two Core application files, and one Core test file. No ProjectSettings or unrelated files are included.

The reviewed boundary is the existing selected P12 Crime/Justice owner mutation path: Core session ownership of the coordinator/protocol pair and NPC Crime/Justice revision/invalidation, with Unity retaining the selected actor projection.

## Review findings

No actionable findings.

- The Core execution boundary pins the exact protocol and coordinator together only for the admitted selected execution, exact owner graph, and owner thread. It verifies that the coordinator is bound to that protocol. Repeated binding of the same pair is idempotent; replacement is rejected and closes the relevant protocol(s).
- Each selected NPC mutation derives the P12 section from the exact graph-owned NPC RuntimeId, checks the exact owner instance and definitions, owner-thread/admission state, revision capacity, and coordinator admission before the status or hidden-state leaf is changed.
- On success, the Core boundary increments that Core owner’s CrimeJusticeRevision once and informs the pinned coordinator for the same section. Reserved daily phases continue to accumulate changed sections under their existing reservation; immediate operations retain their existing epoch notification path.
- Unity’s adapter now performs projection maintenance only after the Core owner write: it refreshes the existing status projection or mirrors hidden days, then delegates revision/invalidation publication to Core. The older host-owned callbacks remain available for compatibility paths outside this selected stage.
- The four Crime/Justice daily phases and their separate reservations are unchanged. Owner, section, coordinator, and protocol identity checks remain exact.
- The tests now exercise the Core execution-boundary admission and commit path, assert the Core owner revision advances exactly once, and assert stable coordinator/protocol binding. The material prior assertions were retained; the changed direct coordinator test calls were replaced by the stronger Core-boundary path.

## Exact-source validation evidence

Private manifests were readable in the E: validation store. I compared their changed-source SHA-256 entries with the current E: candidate worktree files. The exact hashes match:

- `NpcRuntime.cs`: `64408b541a6b48f295b23f7d8b531535b43d932c5713f16e32762190d66df743`
- `P12CrimeJusticeInvalidation.cs`: `2e97bbd14f20d829640c2a4d44d62bf5f753d6ffcb2c49d562fe80ca1dd2cd0b`
- `SimulationRuntime.cs`: `0e29f816593d5ce6d11ed26b72106b76f7f8776ba69393b41767b7e0e642f280`
- `DailyV1ExecutionBoundaryState.cs`: `fa88e38818c6e36b893276ceafebfb33caef4cfa990e8cd80d885293f44a27e7`
- `DailyV1P12CrimeJusticeMutationCoordinator.cs`: `59fed9ab171673103cd5b4894133af58ad5040eeca15b176ff33516af7a62697`
- `DailyV1P12CrimeJusticeMutationCoordinatorTests.cs`: `64f7b57bd6bf27697e3e072ebd36867f47a7de2d817a70a8aeaf205c10613ba2`

The official CoreValidation manifest records the base commit plus the changed worktree inputs; its hashes for the three Core/test files match the candidate worktree. CoreValidation passed **905/905 cold and 905/905 warm**, run `8d9a74e96a53438990f4756ba2f28475`.

The stable full Unity EditMode run passed **3275/3275**, run `f2706cef53b748809a84b5caaf0c89cf`; the stable official Smoke run passed **5/5**, run `902c2fec8d194a13a46a49383a26fc20`. Their input manifests match all five changed production-file hashes above, including the Core boundary and coordinator source.

The corrected focused Crime/Justice run reports **13/13**, run `00debdf705c54bbdb84c9190905a881a`. Its manifest has a different `DailyV1ExecutionBoundaryState.cs` hash (`01abcc9950ede14b909240e461b0d4481828b353de2bfef0f13fd4fd8ae4b254`) from the candidate/current hash above, so I do not treat that focused run as exact-tip evidence. The later stable full EditMode run is exact for the candidate production sources and includes the complete suite; the earlier zero-test filtered attempt is not counted.

The private manifests and original reports were not published or modified. The exact GitHub patch contains no added trailing whitespace. The supplied evidence does not include an independently inspectable `git diff --check` run record.

## Limits

This verifies the bounded selected P12 Crime/Justice Core mutation boundary only. It does not establish complete CORE-F runtime/application/host integration, Phase closure, or canonical promotion.
