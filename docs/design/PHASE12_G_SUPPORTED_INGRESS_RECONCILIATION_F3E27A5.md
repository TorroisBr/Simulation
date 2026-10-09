# P12-G selected-profile supported-ingress reconciliation

**P12 canonical:** 02009f9063dd252bd4b177fd6aef1e74dcd947f5
**Architecture canonical:** 47eff220c7ce00f6e7c759bdc2b76780bb46f628
**Candidate tip before this audit:** f3e27a553ec7164f09ca8d404ad6c55a05b3f8b5
**Scope:** source-linked classification of Expedition and P12-E Military/Conflict/War/Battle ingress for the accepted UnityBootstrap-Daily-v1 profile.

This reconciles the older Daily-v1 live-graph crosswalk with the later current-profile disposition and the production call graph. It closes only the static supported-ingress classification below. It does not validate the complete live inventory, introduce operation scopes, or make P12-G implementation-ready.

## Selected Daily-v1 input and Expedition

SampleScene selects Simulation-DailyV1 with UnityBootstrapDailyV1 (Assets/Scenes/SampleScene.unity:386-388). The selected asset has no authored P10 Ruin site (Assets/_Project/Data/Simulations/Simulation-DailyV1.asset:104); the separate GeneralTest profile carries the Ruin. The normal TesteSimulacao input loop maps Space to Simulate (Assets/_Project/Scripts/TesteSimulacao.cs:149-163). The selected bootstrap constructs SimulationRuntime without an AdventureExpeditionAutonomySystem, whose optional constructor parameter defaults to null; the daily loop invokes that producer only when non-null (TesteSimulacao.cs:290-311; SimulationRuntime.cs:826, 8682-8693).

TesteSimulacao.TryStartExpedition remains a public facade, but there is no normal selected-profile caller in production code. A successful start requires the target site already be registered in the installed ExplorableSiteStore (TesteSimulacao.cs:106-118; ExpeditionSystem.cs:1197-1207). Thus the supported Daily-v1 path has no successful Expedition producer and the older crosswalk's missing Expedition operation-scope concern does not justify adding a new scope for this profile.

Expedition remains a Required P12-F owner. P12-F captures and privately stages it as part of its fixed owner package. Keep that owner and preserve its source payload through staging; do not infer a permanent one-Activity/one-Actor or zero-Expedition architecture rule. If a future admitted profile adds a site and a normal Expedition producer, refresh its owner/operation/quiescence matrix.

## P12-E Military, Conflict, War, and Battle owners

The eight P12-E base owner sections remain Required. Required permits zero cardinality, and these owners are captured and privately staged by the P12-E package. SimulationBootstrapComposition binds their census providers to the exact stores installed in its runtime (Assets/_Project/Scripts/SimulationBootstrapComposition.cs:127-145). P16/P17 proving extensions remain separate profiles; their extension operations require the respective profile/capability gates (SimulationRuntime.cs:887-922, 1627-1681, 1909-1995).

No normal selected Daily-v1 caller was found for the base ArmedForce/Manpower/Conflict/War/Battle mutators. This classifies the selected supported flow; it does not make the owners Excluded or imply zero cardinality for all profiles. SimulationRuntime exposes mutable store references and direct in-process calls remain possible. Those APIs are not evidence of an additional normal Daily-v1 ingress, and this audit does not add security-oriented rejection or claim that arbitrary direct calls are impossible.

## Reconciliation and remaining P12-G gates

The current-profile disposition is retained: no supported Daily-v1 Expedition producer; Military and related base owners remain Required; direct mutator reachability remains a technical caveat. The older crosswalk should be read with this qualification rather than as a demand to add unsupported gameplay or operation scopes.

This closes the static selected-ingress classification for these two areas only. P12-G §7 still requires:
- validating all 299 expected sections against every owner/provider composed by normal selected bootstrap, including dynamic and conditional owners and omitted noncausal read models;
- finishing exact B-F interface and target-owner composition checks;
- implementing and validating fresh restored-boundary admission;
- establishing the single active-session holder and one publication exchange;
- whole-graph rejection, failure atomicity, no-replay, and continuation parity evidence.

Status is unchanged: P12-B through P12-F remain promoted within their recorded scopes; P12-G and P12-A remain WAIT_DEPENDENCY; P13 remains BLOCKED; Phase 12 remains open. No Unity tests were run because this is a source/documentation audit.