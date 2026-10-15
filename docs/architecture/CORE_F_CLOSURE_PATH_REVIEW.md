# Independent CORE-F closure-path design review

- **Reviewed architecture base:** `4b69e43516b3fc548611ce704ef45af9e54e3d0e`
- **Reviewed candidate tip:** `cb70ec79ff6dc4e98da08e026cc73940d204ed00`
- **Production source baseline:** `0094febf7f97333d0653eaf9688fe1d24d73327f`
- **Review scope:** exact candidate documentation and portable graph artifacts; source spot checks against the pinned production baseline. No tracked files changed. No implementation or Unity/.NET test runs were performed.

## Verdicts

1. **Bounded demographic owner package (population plus genealogy): PASS / READY_FOR_IMPLEMENTATION after architecture publication.** This is a bounded extraction handoff for nine existing owner files, not a claim that their implementation or Core promotion has occurred.
2. **Full CORE-F: WAIT_DEPENDENCY.** The shared Daily-v1 application, complete selected source/member and accessibility closure, detached restore integration, fixed .NET host, and new continuation/parity evidence remain open dependencies.
3. **Runtime/restore and host design: WAIT_DEPENDENCY for implementation.** The design is a useful integration contract, but the document correctly does not declare its source cut complete or its host executable.
4. **CORE-X: DEFERRED.** No compatibility expansion is implied.

## Findings

No blocking design defect was found in the bounded owner package or in the stated readiness boundaries.

- The population package retains the existing `PopulationOperationReceiptSnapshot`, uses narrow bridge methods for cross-assembly access, retains the exact Unity guard instance through the shared read-only `IAuthoritativeMutationStatus` interface, and has no second latch, fault setter, `InternalsVisibleTo`, generic count setter, or exposed receipt dictionary. Both-unbound pair operations, one-bound rejection, same-guard identity, callback order, rollback receipt pruning, and receipt-revision behavior are stated explicitly.
- The genealogy package retains the same owner instance for runtime reads/writes, keeps person membership and P12 wire behavior in Unity, preserves the null-record failure precedence, and routes only the named birth rollback and staged-owner operations through the bridge. The existing per-edge stage callback remains at the actual hydration edge loop, with callback throws contained by the existing private-stage boundary. The complete direct caller ledger covers the reviewed source references; lifecycle use through `PersonGenealogySystem` is represented.
- The restored genealogy revision correction is appropriately narrow: only the internal restored-continuation context selects an exact export/stage clone; ordinary construction keeps its current edge-replay normalization. The target-vector mismatch remains a rejection guarantee, so the design does not claim a wrong world can publish.
- The correction has a supported selected-profile test boundary. At source `0094`, `GenealogyCensusTests.SelectedDailyCensusRegistersGateOneOwnersAndTracksParentageOperation` starts the existing selected sample, registers valid `PersonRuntime` values with `SimulationRuntime.TryRegisterPerson`, then exercises admitted `TryAddParentage` and `TryRemoveParentage`. It demonstrates authentic person membership and runtime writer ingress without a new profile producer or snapshot override. A focused regression can use that composition to add then remove one edge (Count 0, Revision 2), complete a positive outer advance, and exercise actual P12-G capture/image restoration; the ordinary-constructor normalization companion remains a separate assertion. This review did not execute that regression.
- The fixed host identity, selected-profile identity, effective-definition fixture, accepted owner-family formula, preserved 49 CORE-B identities, and separate 89-family/24-operation/P12-G obligation maps are not conflated. The host design names missing code, missing evidence, execution ownership, and entry/exit gates. Existing writer code may stay in Unity only while its same-owner and callback path remains represented; that does not satisfy full .NET closure.

## Evidence and limits

The reviewed base candidate contains 14 documentation/evidence files; exact-tip successor `cb70ec79ff6dc4e98da08e026cc73940d204ed00` adds only the audit-retention documentation and inputs. No production or test source changes were made. The graph report states that it analyzed 329 application source files and 2,990 directed source-file edges, with 235 runtime-reachable and 303 restore-reachable files; it also explicitly limits the graph to declaration-reference reachability rather than behavioral or restore proof. The review checked the committed inventory against the pinned production source identity and confirmed the listed blobs match `0094`. This is planning evidence, not execution evidence.

The candidate's readiness transition is bounded: the two owner designs become ready for implementation only after this independent pass and architecture publication. The host design and full CORE-F remain waiting for the exact post-owner source/member closure and real .NET/Unity evidence. No whole-191-file move is authorized by the graph counts.

## Review actions

Reviewed the complete base-to-tip documentation delta, the owner API and caller ledgers, the closure execution contract, runtime/restore integration boundaries, current architecture/roadmap status, and source checks at `0094` for genealogy owner cloning, restore staging, guard binding, selected-profile admission, and the authentic Person-registration/parentage fixture. Reviewed graph provenance and the candidate's explicit non-claims. On the exact-tip successor, I checked the retained reference inventory count/hash and scanned its published inputs for machine paths. The audit-source delta from the prior retained auditor is limited to exact `Simulation.Core` metadata substitution and a privacy-safe missing-source diagnostic; the graph algorithm and published graph inventories are unchanged, and no rerun is claimed. No implementation changes or test runs were made.
