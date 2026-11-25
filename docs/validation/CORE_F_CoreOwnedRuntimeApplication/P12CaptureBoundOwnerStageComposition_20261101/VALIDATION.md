# P12 capture-bound Core owner-stage composition validation

## Validated source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Validation began from committed HEAD `3eb0b515e08c74c9a2af3ce504de6bfec7e04e99`, tree `db7e9965fc33c4d2a01cc4ed15f7d3d35e3ab1ef`.
- Exact Unity input snapshot digest: `edd4a17420fd42981b4b01bd35abfb68c4a9d48ef24314bebfd7f1004b17ab67`.
- The manifest records `inputStable: true`; the three changed production files still match its captured hashes:
  - `P12DDailyV1OwnerPackage.cs`: `e9646b7845be17577862d39a633872cec1d8b3823357211e0c66483b6d03e028`
  - `P12FDailyV1OwnerPackage.cs`: `e66b8aa371885995f19735b3244cbe37821876a166360d72826f045aa6d034e5`
  - `P12GDailyV1RestoreCoordinator.cs`: `9246d4c2174c653abc2a09dc9ad79853348e27dfd2d0e069dbe6272a0a5d37db`

## Bounded change

Capture-bound P12-D now retains the exact selected Core D owner stage that it
already created while staging the captured root. Capture-bound P12-F likewise
retains the Core F owner stage produced from its staged owners. P12-G requires
the D/E/F Core stages and composes the application restore session through the
same Core application path for detached-image and capture-bound staging.
The existing capture staging attempt and its publication checks remain in
place; this change does not publish any candidate early.

This is a restore-composition increment only. It does not complete P12 capture
ownership, `DailyV1Application.TryStart`, the detached .NET host, separate-
process T/T+1/T+2 parity, or CORE-F acceptance. Core canonical remains
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| `SimulationRuntimeAdmissionTests` | **241/241 PASS** | Private run `8221a5a8ce074ee3b129b490f4c7ba6e`; input snapshot digest above; private manifest SHA-256 `d347672ad07164c90b7f130e13d08006eb124bf55374cdfa553878a0a8fc1c51`; original XML SHA-256 `47eba53f925eb6322c3ed10fd60e9e435c70973f60e75cbbe787ddd13d603613`. |
| Full Unity EditMode | **3294/3294 PASS** | Private run `f7aa5dca4cac472292f7dbf787573a85`; private manifest SHA-256 `e12acdc0869d766e9da0f51e5ea0b3fcf44be7063965a7ef01a3802b8706e36a`; original XML SHA-256 `d78396a4f53ea7adc6651a76ba084c66708d3b787e4a4d2960d286af6c07d418`. The normalized package is in `FullEditMode/`; package verification with the protected original manifest passed. |
| Official EditMode Smoke filter | **5/5 PASS** | Private run `702c671946a346989bdd99e264a0234d`; private manifest SHA-256 `63c01dd484cfbd46ed1d0f8d6e034b3a434318ee601ac809806f2405c63f5c39`; original XML SHA-256 `51833207bfcb7b75ee314c9b167bc19d3c73b061fe4a02e1b4e91e547c5b10f6`. |
| `git diff --check` | **PASS** | Checked on the candidate source diff. |

The full public package has bundle ID `p12-capture-bound-full-editmode`.
Its manifest SHA-256 is
`2e58f00f79d80ced32f4ea7ad8fd6568e775df7d1bfeb7722b5726360a444734` and its
archive SHA-256 is
`7cff04451055e98729da3887d67dc175e36adb14b8eda256df6abd5257a91627`.
It contains the normalized 3294-case result; verification confirmed one
archive entry and the recorded archive hash.

The evidence packager refused public packages for the two filtered XML files:
Unity leaves parent suite `testcasecount` values at the full 3294 cases while
including only the selected descendants (241 and 5). The original filtered
XML and manifests remain unchanged in protected local retention; they were
not repaired or reinterpreted. The unfiltered package contains the complete
suite, including those test identities.

## Preserved boundaries

- The exact selected definitions/effective-configuration reference check is
  preserved by the Core composition method.
- No roster-cardinality assumption or owner reselection fallback was added.
- D/E/F stage composition remains unpublished until the existing P12-G
  transaction and publication checks succeed.
- `TesteSimulacao.cs`, ProjectSettings, untracked user files, prior evidence,
  and private original XML/logs were not staged by this validation record.
