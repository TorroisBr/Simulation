# Core Local SellGoods Continuation — Validation

**Base:** `1a84a093ea1c3b669c2a97c67a0fd27e400780ed`

**Scope:** Completes the one-shot Core Local SellGoods continuation boundary. A completed LocalPlayer choice remains admissible only with its exact actor, admitted action, P11 terminal disposition, and matching P11/P12 decision record. An already installed autonomous SellGoods action remains admissible only with its exact autonomous decision record. Market-sale census refresh permits only the exact affected Inventory owner and City Market owner; the latter can gain exactly one stock-row record for the selected item. The regression exercises a successful first-row sale, then validates and advances the same session again without redispatching or replaying the completed choice.

This is an intermediate CORE-F checkpoint. It does not claim detached-process restoration, public `DailyV1Application.TryStart`, complete host-neutral execution, or CORE-F acceptance.

## Validation results

- Official CoreValidation: **1,183/1,183 cold and 1,183/1,183 warm PASS**, including the new Market-row cardinality and completed-choice continuation regressions. Run `55289abead454c13ae186d5c50465565`; native manifest SHA-256 `7377597f15948072e9a326303ded27cfe9f070c26aa30fccef8263fd6afb46bf`.
- Official Unity 6000.3.9f1 full EditMode: **3,325/3,325 PASS**, including all five Smoke test identities; zero failed, skipped, or inconclusive. Run `ebb99405432e4a6993bdcab4763e2933`; original XML SHA-256 `8a3695f3a12fe665a4f2800159bbc94c66579d8b7a3dd092ccd5afa55a79f67e`; original log SHA-256 `fe77f47eb9dee0ca2c54d53e00a8c92e2b4f9b3fffb8fb922bf71795d882304e`.
- Official filtered Smoke: **5/5 PASS**, run `03b0a13b5e7d4523a23d09d399fedf22`, against candidate `46b7856e5cde3cf1e2f502af7250426b0a9b1d61` and tree `241735597f63b1a6ac2a11f0e578f7a1a1cf098b`; before/after source snapshots match. Original XML SHA-256 `de981450373af167b0e46cd46e1e1cf210f7cf3a2572e8957ee72cf60ccd77b6`; original log SHA-256 `901eef999dda18d933454d6d3d1a231080264200b08af4c992870b247a7bcf0d`.
- Unity runner source-input snapshot remained stable: repository HEAD `1a84a093ea1c3b669c2a97c67a0fd27e400780ed`, committed tree `a647cd66b46c28d8f6f05bc502dfef45da61f395` before and after the run. The three changed production package files were present with identical before/after hashes. CoreValidation bound the changed .NET test source through working-tree input digest `a5fe8ec8e6feb4bc0d44b75a9573eb764780bf29e86a8d10f8e349bf2a041844`.
- `git diff --check`: PASS.

## Independent review

Independent exact-tip implementation review: **PASS** on candidate `46b7856e5cde3cf1e2f502af7250426b0a9b1d61` (tree `241735597f63b1a6ac2a11f0e578f7a1a1cf098b`, parent `1a84a093ea1c3b669c2a97c67a0fd27e400780ed`); no actionable findings. The review verified the P11/P12 actor, action, payload, PersonId and terminal-disposition provenance; that the next daily advance neither redispatches nor replays the completed choice; and that the Market census permission is bound to its exact owner, section, thread, operation and one-row increase. Existing assertions were retained.

Original TRX/XML reports, logs, manifests, and execution traces remain in private evidence storage. This record exposes only run identities and integrity hashes; it does not publish raw evidence or claim a normalized public evidence package.
