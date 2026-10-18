# CORE-F current runtime source graph — candidate 687114f

This graph refresh was generated against the exact Unity source tree of
candidate `687114f33bc8d4ed81cdcccf15934ab178d3a6c0` (tree
`cb711113fe480ae4f0973e5cedecd139b7a67811`), using the Unity 6000.3.9f1
`Assembly-CSharp.rsp` from that same code tree. The compiler-response SHA-256
is `48de2ac003c5b68ab49750bb7c2033f855f67b3f5c76721aa0fa72b79f016314`.
The analyzer source is the preserved `CORE_F_SourceClosure` Roslyn generator;
SDK 9.0.315 was used.

Roslyn resolved 332 of 333 response references, omitted the same parser-added
`mscorlib` reference described in the earlier source-closure record, and
reported zero errors/eight warnings. The current Unity source graph contains
300 Assembly-CSharp files, 2,416 directed declaration edges, 120 SCCs, 205
runtime-reachable sources, 274 restore-reachable sources and 26 sources outside
both closures. The analyzer's old hard-coded graph count was corrected from
329 to the observed 300 in the committed manifest; the source files and edge
results were not changed.

`SimulationRuntime.cs`, `CityRuntime.cs`, and `NpcRuntime.cs` are in the same
168-file strongly connected component (SCC-001). In this current Assembly-CSharp
projection, 122 files have no direct UnityEngine symbol and 46 require an
explicit Unity/Core split. The whole component is not a migration checklist:
use the accepted selected Daily-v1 profile, owner/access contracts and
`CORE_F_RUNTIME_RESTORE_INTEGRATION_DESIGN.md` to separate Unity authoring and
excluded-profile adapters from the shared semantic runtime. Preserve P10 Ruin
as its separate proving profile; do not expand the P12 Daily-v1 profile.

This is current-source compiler/access evidence, not implementation readiness,
owner-closure proof or authorization to move files solely because they have no
direct UnityEngine symbol. The existing approved CORE-F shared runtime/restore
package remains the next implementation boundary. The CommercialKnowledge /
Inventory / Market owner-consumer integration is independently reviewed and
validated in `CORE_F_OwnerConsumerBoundary/REVIEW_687114F.md`.