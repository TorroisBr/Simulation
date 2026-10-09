# P12-G restored-boundary admission validation

**Code candidate:** `6f0dde51b1100c201346bb8a12c15f51952a5848`

**Assets tree:** `d7b179d40b8a0e70ade5ff8592028448bf7a7192`

**Design revalidation:** `562d7404f346867d793e1ec6ecaa3aca640f8607`; independent documentation review PASS is recorded at `5f0429bb983f10675340600c606cfd31740cb261`.

**Scope:** the runtime can admit a fresh selected Daily-v1 runtime at a restored completed boundary by issuing a new candidate-bound eligibility token with `RestoredContinuation` provenance and preserving the supplied absolute day and completed-core sequence. It rejects mismatched identity/day, nonpositive sequence, wrong owner thread, unsupported profile, already-published/advanced candidates, active operations, and unavailable or nonquiescent census state. It does not transfer the source runtime token, advance time, replay operations, or increment the sequence during admission.

Unity Editor `6000.3.9f1`. All result XMLs report Passed with zero failures, skipped tests, or inconclusive tests. Exact artifacts are retained in `P12GCrimeSocialRuntimeOperation/Focused/`.

| Gate | Result | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|
| `SimulationRuntimeAdmissionTests` | 75/75 PASS | `E16C744FEC2271EB02D53415915B773BD638B003797B275C1C9BFF5CDF22A052` | `4B5ED7A496B65D3FE5CD89B7F10E3C77D659C7F6AB50A7C3DC6D754896292FEF` |
| ALL EditMode | 2738/2738 PASS | `D2D24950AE6515B03465DD8246A8E19A0E7A364D06005923C67FDA566832681A` | `30D87F0742202F114EDEC3A80365445FAA6F9C41910B672F4308FF6063B048A1` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `5A2CC3975CC3EE27816AA70F92CCD42EEE5F2BF8AA87ED7C014457F4D821A05E` | `1511E320BD4332BB7E103F23F63BAED7AEFC8CFB7506B56F410B93FEFC8F9EBF` |
| `SimulationBootstrapCompositionTests` | 26/26 PASS | `660A6A15F0D5BE42BD51021944A7E4D35FD5F92988E1F8D8C66AB1E7FE31814C` | `CD06F0CBC61A87FA1FE4D120B5B2EB0DE526E89F675A0AC377CF2CFE073309E4` |

`git diff --check 678b01dc9c9dddf05cbd0a64033afc7b1ed1615b..6f0dde51b1100c201346bb8a12c15f51952a5848` passed.

This is a bounded prerequisite capability only. It does not provide restore-envelope parsing, whole-graph assembly, staged target-owner exact-zero validation, active-session publication/swap, failure atomicity, no-replay/parity proof, or complete graph quiescence. P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`.
