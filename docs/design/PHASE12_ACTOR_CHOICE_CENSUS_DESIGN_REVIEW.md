# P12-B ActorChoiceStore census witness design review

**Result:** Independent technical design review PASS.

**Design:** `codex/phase12/P12BActorChoiceCensusDesign` at
`b5504722cc4a12ff271e25f5ea171ee0668bf4ce`.

**Design base:** `codex/phase12/canonical` at
`1ada62b031e738e2bdd5d3d623e028a114961d6e`.

**Refreshed canonical checked:** `codex/phase12/canonical` at
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`, including the promoted
RuntimeIdentity census. `ActorChoiceStore.cs` is unchanged; the added
RuntimeIdentity composition provider is compatible with the proposed additive
ActorChoice provider surface.

The review found no blocking issue. The two-section design correctly
distinguishes retained P11 inputs from P18 temporal inputs, shares one opaque
installed-owner identity and monotonic revision, accounts for same-cardinality
disposition writes and idempotent temporal replays, and binds to the
`ActorChoiceStore` clone installed in `SimulationRuntime`. The accepted P12-B
through P12-G capability authorization is sufficient for this passive census
slice; no new checkpoint acceptance is required.

The reviewer suggested a small documentation clarification: name all four
temporal disposition methods explicitly and state that `runtime-faulted`
means the existing mutation-guard return path, without promising new
transactional semantics for unexpected thrown exceptions. This is
nonblocking and will be reflected in implementation/review evidence.

This design does not complete P12-B, connect writes to the shared mutation
epoch, establish owner-thread/quiescence, deliver export/hydration, or make
P12-A ready. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`.
