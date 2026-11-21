# Core Local SellGoods Continuation — Validation

**Base:** `1a84a093ea1c3b669c2a97c67a0fd27e400780ed`

**Scope:** Completes the one-shot Core Local SellGoods continuation boundary. A completed LocalPlayer choice remains admissible only with its exact actor, admitted action, P11 terminal disposition, and matching P11/P12 decision record. An already installed autonomous SellGoods action remains admissible only with its exact autonomous decision record. Market-sale census refresh permits only the exact affected Inventory owner and City Market owner; the latter can gain exactly one stock-row record for the selected item. The regression exercises a successful first-row sale, then validates and advances the same session again without redispatching or replaying the completed choice.

This is an intermediate CORE-F checkpoint. It does not claim detached-process restoration, public `DailyV1Application.TryStart`, complete host-neutral execution, or CORE-F acceptance.

## Validation results

- Official CoreValidation: **1,183/1,183 cold and 1,183/1,183 warm PASS**, including the new Market-row cardinality and completed-choice continuation regressions. Run `55289abead454c13ae186d5c50465565`; native manifest SHA-256 `7377597f15948072e9a326303ded27cfe9f070c26aa30fccef8263fd6afb46bf`.
- Official Unity 6000.3.9f1 full EditMode: **3,325/3,325 PASS**, including all five Smoke test identities; zero failed, skipped, or inconclusive. Run `ebb99405432e4a6993bdcab4763e2933`; original XML SHA-256 `8a3695f3a12fe665a4f2800159bbc94c66579d8b7a3dd092ccd5afa55a79f67e`; original log SHA-256 `fe77f47eb9dee0ca2c54d53e00a8c92e2b4f9b3fffb8fb922bf71795d882304e`.
- Unity runner source-input snapshot remained stable: repository HEAD `1a84a093ea1c3b669c2a97c67a0fd27e400780ed`, committed tree `a647cd66b46c28d8f6f05bc502dfef45da61f395` before and after the run. The three changed production package files were present with identical before/after hashes. CoreValidation bound the changed .NET test source through working-tree input digest `a5fe8ec8e6feb4bc0d44b75a9573eb764780bf29e86a8d10f8e349bf2a041844`.
- `git diff --check`: PASS.

Original TRX/XML reports, logs, manifests, and execution traces remain in private evidence storage. This record exposes only run identities and integrity hashes; it does not publish raw evidence or claim a normalized public evidence package.
