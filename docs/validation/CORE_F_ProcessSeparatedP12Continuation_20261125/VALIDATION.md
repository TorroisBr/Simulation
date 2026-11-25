# Process-separated P12 continuation — validation

## Verdict

PASS for the bounded host-neutral Daily-v1 process-continuation checkpoint. This is not CORE-F completion, a Core-canonical promotion, or a public application API claim.

## Source and evidence identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Source parent at validation: `a136ba7dc316b2da09633361ad35bc44bbf32cb3`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Official CoreValidation run: `148ffaf8da394748a580357d28d53a9e`
- Official input digest: `657a0ade6e8a941028fb9ce46428a15f5ff7c509c1f971bcb15b3b6ba0a4f876` (563 source inputs; stable)
- Official manifest SHA-256: `2ee74f1426fc7a1360720e4045211b81045b2cdba50fb1b6ddbdf23c6932d120`
- Cold TRX SHA-256: `8344e610e026e14ae1cfaf54a95a01febadfd8de4ab87e6625890480b880923f`
- Warm TRX SHA-256: `da0916888ebb40bbce05996bac158c81b653f8995c1c44d12accc3140d10706b`

The protected official manifest binds each of the five changed source/project/test files to its current byte length and SHA-256. Those checks passed. Evidence remains in its protected, owner-only storage; this public record contains no private workstation path.

## Process-separated scenario

The focused regression launches four distinct `dotnet` operating-system processes:

1. Process A creates a Core Daily-v1 session, advances one day, and saves through the production P12 v2 store.
2. Process B starts independently, restores A’s saved artifact, advances one day, and saves.
3. Process C starts independently, restores B’s artifact, advances one day, and saves.
4. A separate Genesis control starts from the same selected content, advances three days without process restart, and saves.

The test verifies four distinct process IDs, `Genesis`/`Loaded` start results, retained WorldId through A→B→C, a separate control WorldId, exact profile and completed day/sequence, all 299 registered P12 section records, materialized owner rows, exact-empty sections, and deterministic equality of the decoded authoritative state at day 3. It also verifies that restarted processes establish fresh process-local mutation epochs and publish their own committed mutations.

Observed result details are retained in the protected CoreValidation TRX. A and B materialized 217 and 220 rows with 164 exact-empty sections; C and the uninterrupted control each materialized 221 rows with 165 exact-empty sections. The C/control decoded-state digest matched: `c018b63a78cb753637bbfe9ce50bcc9e7e2ab421ddac10211c7d2f6b09a21acb`. Their complete P12 image hashes intentionally differ because the independent Genesis control receives a distinct Core-generated WorldId; the test does not normalize owner payload identities.

This proving profile serializes its supported owners and exact-empty sections; the count of registered sections is not a claim that every census family contains a materialized row. Existing 89-family census composition and exact-empty contracts remain unchanged.

## Validation

- Focused process-separated test: 1/1 PASS; four distinct OS processes.
- Official pinned CoreValidation: cold 1212/1212 PASS; warm 1212/1212 PASS; zero failures, errors, aborts, or timeouts.
- Runner invocation used `-SkipLab`; standalone Simulation.Lab gates were not run by this invocation.
- `git diff --check`: PASS for the candidate changes.
- No Unity EditMode or Smoke run was performed for this .NET-only host/test addition.
- The checkout’s Unity Library was present during CoreValidation (`libraryAbsent=False`). The process host itself runs from its copied .NET output and exercises Core/P12; this result does not establish a Library-free checkout or full environment independence.
- Disk free space changed from approximately 11.15 GiB to 11.14 GiB across the official CoreValidation run. This is below the 15 GiB warning threshold and above the 8 GiB floor; no cleanup was performed.

## Scope and limits

The test host is a bounded verification executable that invokes existing internal Core application and P12 contracts. It does not make `DailyV1Application.TryStart` public, introduce a new P12 schema, populate unsupported receipt sections, broaden persistence compatibility, or close CORE-F. The shared production P12 save/restore codec and application lifecycle are exercised; Unity remains outside the authority path in these processes. Core canonical is unchanged.
