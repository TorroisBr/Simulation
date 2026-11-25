# Independent review — P12-D City/NPC restored-owner slice

Verdict: **PASS**

- Base: `6676c7015c48992818181f17cae0edeb74c98848`
- Candidate: `60fd2401a8ea5be889f084149a9ee9d493082c3d`
- Candidate tree: `bceabfe42e7c4641eac2fbb689ce51a7d5d620a0`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The independent exact-tip review inspected the complete candidate delta, including the City/NPC Core restore factories, Unity binding paths, additive Core tests, City regression coverage, and the final `Configuration.meta` normalization. It found no actionable defects, assertion weakening, duplicate authority, scope expansion, or identity/revision loss.

The review verified that restored values are staged before publication; City and NPC projections bind to the same selected Core owner instances; and invalid/foreign graph data is rejected without publishing partial owner vectors. The NPC coverage includes current-action payload, lifecycle/crime/hidden state, travel identity/progress/revision, and travel-plan values. The folder metadata retains its existing GUID and has stable serialization.

Exact-tip evidence is recorded in [the validation record](../validation/CORE_F_CoreOwnedRuntimeApplication/P12D_CityNpcRestoration_60fd2401/VALIDATION.md). Focused NPC, ALL EditMode, Smoke, and CoreValidation results were 30/30, 3261/3261, 5/5, and 709/709 cold plus warm respectively. The CoreValidation run used the candidate's Windows checkout projection and stable input manifest. This review does not establish completion of P12-D, CORE-F, or Core canonical promotion.