# CORE-F Runtime Allocator Census Boundary

## Candidate and scope

- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate base: `db19e5557fb6e6d6e95a395cb2c0e478f87e5a60`.
- Implementation commit: `1aea41288ed03c67429eea586c03dc4ff38fb1a8`.
- Implementation tree: `f744a0ba9bbd9f38f7b9044e6634548af3773bfc`.

The Unity host continues to capture the complete selected Daily-v1 owner
vector and mutation epoch through its internal census protocol. Once the exact
Core owner graph is bound, the outbound host port validates the full vector
against Core recomposition, including the five Core P12-F rows and the three
`RuntimeIdAllocator` counter rows. It then omits those eight rows from the host
remainder. Core continues to use its internally complete provider inventory
for quiescence and operation baselines and recomposes the complete capture
vector. No census family, owner identity, revision, cardinality, or epoch is
dropped.

The focused Core test removes all eight rows before Core recomposition,
verifies a structurally complete vector, and rejects reintroduction of any
individual Core-owned row. The Unity integration test verifies that the host
port returns exactly the other rows at the unchanged mutation epoch and that
each retained row preserves its role, schema, owner object identity,
cardinality, and revision. The completed boundary still publishes a complete
owner vector.

Changed implementation/test files and SHA-256:

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `d3f0c92244c502503492ddd51e46ea68846f02794a0097da5407d459f3de142d` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `5288380749940c2e1a5c284b7dc3316d9d1f66661c89d87fa49b32541eb0a014` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `ddcefe8cfe3c967fd98758addf08c4ce44aaac5cc31995b128a14e505fcc0766` |

## Exact-source validation

All final code and test files above match the corresponding official runner
input hashes. Both Unity invocations reported stable before/after input
snapshots with digest
`9c2508dd3ad04072c87e57d4596754f31302181c64c3f18cf0d7627919f2c114`.
CoreValidation reported stable inputs with digest
`a9894ebe823ae0e5ca90b046f35ec332b773e9e6ce9d6b81b341d00efe68899a`.

| Gate | Result | Run identity |
| --- | --- | --- |
| Official CoreValidation cold | 1,015/1,015 PASS | `d62492a56c5c4e21b6372318722fdc97` |
| Official CoreValidation warm | 1,015/1,015 PASS | `d62492a56c5c4e21b6372318722fdc97` |
| Unity 6000.3.9f1 full EditMode | 3,299/3,299 PASS | `1b6d6c3a98f2482e8e1ee38695b4fa97` |
| Official EditMode `Smoke` filter | 5/5 PASS | `b0692717b0014fd2a822e15ebe1871ca` |
| `git diff --check` | PASS | implementation commit |

The official Unity packager created and verified the complete EditMode
result-only bundle against the protected source manifest. Its manifest is
[`coref-runtime-allocator-census-editmode-20261105.manifest.json`](Unity/coref-runtime-allocator-census-editmode-20261105.manifest.json)
with SHA-256
`8c2de8845ce6d2203053d3e4d2f598ab5c910489b1344918c00c6584d0578897`;
the normalized archive is
[`coref-runtime-allocator-census-editmode-20261105.zip`](Unity/coref-runtime-allocator-census-editmode-20261105.zip),
174,532 bytes, SHA-256
`2309d297746de75a6d37d072354ee995176a97154f6e10cdaea5acd6fca9ca45`.
The normalized archive omits test output under the approved policy; it retains
the complete test identities and outcomes. The standalone Smoke XML and log
remain unchanged in protected local evidence. The full EditMode package
contains the five Smoke test identities and their passing results.

CoreValidation's native private manifest is run
`d62492a56c5c4e21b6372318722fdc97`, SHA-256
`f7c7d87074698fe91b7a7ae46b0f1081c9194784dd51cdc6fe7b2adbf2a24aed`.
The original cold and warm TRX SHA-256 values are
`c8b8a6e732c05538bcb4d37cea390ec46b7584ac9509bf46136892040118bdfe` and
`762fc9c802c49ed24705c31a2617308b1226b11b4b0da9d7403f9f69db4430c3`.
Those originals remain protected and unmodified. No public .NET TRX package is
claimed: this CoreValidation run uses its native manifest schema, not the
separate `dotnet-trx-source-context-v1` adapter required by the approved
DotNetTrx packager. The run also observed a Unity `Library`, so it does not
establish Library-independent .NET execution.

The standalone Smoke run used the correct five-case EditMode filter. An
earlier PlayMode invocation with the GM Console class filter selected zero
tests and was excluded. A separate GM Console-only EditMode invocation passed
2/2 but was a partial selection, not the Smoke gate; the packager rejected its
filtered suite hierarchy, so it is not included or used as evidence. No result
XML was edited or reinterpreted.

The Unity snapshot includes the existing unstaged `ProjectSettings` input in
its stable inventory. That user change was not modified or staged. Protected
untracked validation evidence was also left untouched. This candidate makes
no claim that either user-owned item was committed.

This is a validated implementation checkpoint, not an independent review, Core
canonical promotion, full CORE-F acceptance, or Library-independent host
claim. No Unity `LongRun` gate was run because this census-boundary change does
not alter daily causal ordering or long-horizon behavior.
