# CORE-F source/access revalidation after institution and claim owner promotion

**Verdict:** source graph refreshed; shared runtime/restore integration remains
`WAIT_DEPENDENCY` for a complete semantic owner/member/access ledger. This
revalidation is not an implementation authorization or a CORE-F completion
claim.

## Current promoted boundary

Core canonical is `2764b50b49c0773328ebcc56b2c668a2c609b7ff`; the bounded
institution and political-claim owner package was promoted at `06b0e4f0` and
the following State-only tip records it. Architecture canonical remains
`797eb02f06fc5ca759a7e47283589d97a5efe73d`. Production source is unchanged
from reviewed code commit `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`, tree
`e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`.

## Refreshed compiler/source graph

The current Unity `Assembly-CSharp` response resolves 315 source inputs, 332 of
333 metadata references, with the same single parser-added `mscorlib.dll`
omission documented in the baseline analysis. Roslyn reports zero errors and
eight existing warnings (`CS0108`, `CS0618`). The declaration graph has 2,837
directed edges, 50 shared-partial declaration links, 116 SCCs, and a largest
SCC of 190 files. `SimulationRuntime` reaches 220 application files across 31
SCCs; `P12GDailyV1RestoreCoordinator` reaches 289 across 91 SCCs. Twenty-six
application sources are outside the restore closure and 69 are restore-only.
The inventory contains 477 Unity-symbol rows and 2,625 Simulation.Core symbol
rows. The updated `Simulation.Core.csproj` has 53 explicit source inputs.

The previous published graph was 329 application files, 2,990 edges, 128 SCCs,
235 runtime-reachable files and 303 restore-reachable files. The current source
inventory delta is exact: 15 prior Assembly-CSharp paths now live in the shared
Core input set (seven population, one genealogy store, three institution, three
political-claim, and one property-contract source); one genealogy mutation-guard
binding source is newly present in Assembly-CSharp. The net application-source
count is therefore 14 lower. The complete current source and edge rows, rather
than this aggregate comparison, are the inventory authority.

`source-inventory.csv` and `core-build-inputs.csv` were checked row by row
against the production commit; there were zero blob mismatches. Compiler graph
generation compiled the current Unity source snapshot and made no source or
test edits. It did not run Unity tests or modify any protected ProjectSettings.

## What this establishes and what remains open

The owner promotion moves the approved institution and political-claim owner
implementations and their required Core bridges into the shared assembly. The
refreshed graph establishes the current source blobs, Unity/Core assembly
boundary and compile-level declaration dependencies. It does not establish the
runtime behavior or complete source ownership for every admitted Daily-v1 owner.

The following exact prerequisites remain before the dependent
`CORE-F-Shared-DailyV1-Application-and-Host` package can be marked
`READY_FOR_IMPLEMENTATION`:

1. Rebase the 89-family owner/capture/target and 24-operation ledgers from their
   historical source identities to current canonical source blobs, then join
   each selected family to its live owner instance, constructor, supported
   writers and epoch path, capture codec, staged hydrator and required test
   identity. No aggregate count substitutes for these row-level joins.
2. Freeze the member-level source/access ledger for the remaining definition-
   bound owners and the 190-file application SCC. For each required constructor,
   internal member and cross-assembly edge, identify the single source owner,
   same-instance bindings, Unity adapter cut and admitted/rejected path. Preserve
   canonical definition-object identity; the compile graph alone is insufficient.
3. Confirm that the remaining selected source graph has complete owner/writer/
   epoch/export/hydration coverage and can compile from shared source without
   Unity dependencies except the named adapters. The newly moved owner groups do
   not close the remaining definition-bound City/NPC/economy graph.
4. Only after that ledger and API boundary pass review, integrate the shared
   application/session gate, owner-thread admission, bootstrap, v1-compatible
   persistence plus host-kind-v2 descriptor path, detached restore/publication,
   and fixed .NET host/fixture in the serialized `SimulationRuntime`/P12-G
   hotspot.
5. Then provide the specified cold/warm .NET host tests, existing 49 Core
   identities, 89-family/24-operation cases, finite P12-G negative/atomicity
   coverage and T/T+1/T+2 round-trip parity before final CORE-F acceptance.

No missing product or architecture decision was found. Existing contracts
already define these boundaries. No downstream code implementation is marked
ready by this graph refresh, and no P12 profile or numbered Phase status
changes.
