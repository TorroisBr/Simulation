# Simulation Configuration Foundation

The configuration foundation separates authoring from the runtime contract. Future presets, Unity ScriptableObjects, JSON, APIs, and GM or campaign overrides may be sources of values, but the simulation core should consume only a validated `EffectiveSimulationConfiguration`.

The intended future resolution pipeline is:

```text
Engine defaults
      ↓
Preset
      ↓
World/campaign overrides
      ↓
Content/domain overrides
      ↓
Validation
      ↓
EffectiveSimulationConfiguration
```

## Policies and representation

`SimulationDomainPolicy` contains two independent decisions:

- Availability answers whether a domain exists in the world (`Unavailable` or `Available`).
- Autonomy answers whether the domain may start behavior autonomously (`ManualOnly` or `Autonomous`).

An unavailable domain cannot be autonomous. The validator reports that invalid combination; it is never corrected silently.

Population detail is represented separately from decision processing. `PopulationRepresentationMode` has `Aggregate`, `Hybrid`, and `FullyIndividualized`. `FullyIndividualized` means people may have individual identity; it does not mean every person is a complete `NpcRuntime` or makes a decision every tick. `NpcDecisionSimulationScope` independently chooses `RelevantOnly` or `AllMaterialized`.

The current effective configuration contains only the population section and a small set of explicit conceptual policies for mobility, crime, economy, and adventure. It does not integrate with those systems yet and does not duplicate their existing runtime or authoring types.

## Authoring, effective values, and future overrides

Authoring models can be Unity-specific later. The effective configuration is a plain immutable C# object and does not carry source provenance as domain truth. A preset is a set of values, not an alternate runtime path: `Preset = a set of values` and does not require an alternate runtime path.

Future overrides should be typed members of the relevant domain configuration. This foundation intentionally does not introduce `Dictionary<string, object>`, reflection-based property paths, a generic rules engine, persistence, networking, or JSON dependencies. Domain policy, domain parameters, content definitions, and contextual modifiers are separate concepts. Numeric parameters such as travel speed, crime chance, or demographic rates belong with their respective domains once their semantics are known.

There is no runtime integration in this foundation. Creating a new effective configuration is the deliberate mechanism for future campaign changes; hot reload is outside this scope.
