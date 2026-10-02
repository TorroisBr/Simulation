# P12-B money-transfer operation design review

**Verdict: PASS — independent exact-tip technical design review**

- Candidate branch: `codex/phase12/P12BMoneyTransferOperationDesignRefresh`
- Exact reviewed design tip: `ea1d5b58ab8c5204c7559d13be10034e3a8732eb`
- Refreshed P12 canonical base: `ed14e8dd9575a56461f7648d7ff786e114224785`
- Promoted code-bearing integration base: `9d1474b4299d8e888dd387e02e9018d9e8627f84`
- Combined WI-A/P12 hotspot review: `313095ecec87b11928708607821c6b9ad9b5e275`

The review verified that the prior sequencing discrepancy is resolved: the refreshed design cites the durable PASS record for the combined WI-A/P12 `SimulationRuntime` and bootstrap interaction. The revalidation confirms WI-A preserves the promoted P12 owner notifications and named-operation admission; it was a targeted source and test review, and tests were reviewed rather than rerun.

The transfer boundary is appropriately limited to the existing NPC-to-NPC `float` overload and its exact source/destination MoneyAccount sections. It requires current roster identity, exact account/section binding, local revision baseline, and precommit admission; keeps one named scope through debit, credit, compensation, and result selection; relies on the owner hooks without duplicate notifications; and preserves current result semantics. It retains the finite zero-value successful no-op without account revision or epoch advancement. The raw-account overload is rejected before writes whenever the service is P12-bound, while standalone unbound use remains supported.

The accepted P12-B decomposition identifies money transfer as a remaining operation family within existing scope, not a new checkpoint or product behavior. Crime/Justice writes and the later reverse transfer remain outside the individual transfer operation boundary. The design does not claim complete owner or operation coverage, universal shared-epoch coverage, global quiescence, capture eligibility, export/hydration, P12-A readiness, P12-B completion, or P13 readiness.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked; Phase 12 remains open. This review record does not promote the design to canonical or authorize any additional scope.
