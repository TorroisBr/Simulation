# P12-E Unified Core Owner Staging — Validation

## Candidate and scope

- Candidate source commit: `88920f0e2c4af7c6084a96e73947abf6c982eca3`.
- Candidate tree: `5be4aa5513f8c9f368491acc05979ce1c480d02a`.
- Parent candidate: `7e7e8018f6bb2448411d83344af9398c267a3bb0`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Scope: the ordered Core P12-E detached restore stage now includes the existing
  `P12EMilitaryOwnerSnapshot`. Core creates and returns the ArmedForce,
  contingent-manpower, and force-position owners as part of the same private
  stage as the other P12-E Core owners. The exact staged P8 spatial authority is
  used; `stagedLocalTopologyComposed` remains `false` for the accepted
  Daily-v1 profile.
- No LocalTopology, P10 profile expansion, Phase closure, or complete CORE-F
  application/host claim is made.

## Validation

All listed runs used source commit `88920f0e2c4af7c6084a96e73947abf6c982eca3`
and tree `5be4aa5513f8c9f368491acc05979ce1c480d02a`.

| Gate | Result | Run / source identity |
|---|---|---|
| CoreValidation cold and warm | 923/923 PASS each; stable inputs; Unity `Library` absent | Run `43622a46fb494236818d02ec47fb2b18`; input digest `78d581dbea929e877585db2d642382966de91e74b9a0d92f4ed963b425b7b7cb`; private manifest SHA-256 `f91cf9c34a419e626c5fc58f51ab243b3d54b386cbb63678bbc40697dcfc313f` |
| CoreValidation original cold / warm TRX | Preserved unchanged in protected evidence | SHA-256 `2beb985839431b4ba5c51a5d7583731e2f163e190b7ce4fc9f76816884f36be2` / `4820a68336c52b84a83fa57e88da34b865b505a0a4786e5e9360694a40346141` |
| Standalone Simulation Lab | PASS; locked restore/build passed; cold and warm fixed scenario outputs matched | Run `4c91af367eb247e695f45b1094568dc1`; private manifest SHA-256 `a0173d9d181590ddee1293deb78b420ac9f9569fb3ed02d47f75b02cbf4d08f9` |
| P12-E focused EditMode | 111/111 PASS | Run `8579fa6afb15446189a5cdd18aa0ce5c`; input digest `c51b94bc7d17db206284b9638c06a8038c921f0b5bbadecbfa9b8f1f3e407455`; private manifest SHA-256 `659cff5beaed75cd4c3221b1e74ccfbce07098acd57d2e9b496d14baa1816fb1` |
| Official EditMode Smoke filter | 5/5 PASS | Run `2f13cd852bda4b0a85515dc0799e8359`; same input digest; private manifest SHA-256 `670bdbd82a70b24e75b961410bb29fb0b47b17b5d0a750d5068ed47a009104da` |
| Full EditMode | 3279/3279 PASS; zero failed, skipped, or inconclusive | Run `2109c08d25394146822767bea5a52262`; same input digest; private manifest SHA-256 `13789dec1cc89c7e7c63d37fa5fad27a7981c3f6c9b4d910a0e5f30770158073` |
| `git diff --check` | PASS on candidate source commit | The three pre-existing ProjectSettings modifications and unrelated untracked/private files were not staged or committed. |

Unity used `6000.3.9f1`. The P12-E, Smoke, and full EditMode runs share one
stable before/after input digest. The first Unity import in the isolated
validation worktree generated local changes to `ProjectSettings/EditorBuildSettings.asset`
and `ProjectSettings/ShaderGraphSettings.asset`; those files were not staged or
copied to the candidate worktree. The code and test sources remained at the
candidate commit throughout validation.

## Public evidence package

The complete normalized EditMode XML package is in
[`Evidence_88920f0`](Evidence_88920f0/). The approved verifier passed against
the private original run manifest. Manifest SHA-256:
`c7ddc570356079bdd653bc931b9ac5ca4be80a28afa23655b21e0d51d25b7fa8`.
Archive SHA-256:
`397b362ed1c3a2105a6e0e06428439bdd0167014047eaff33cce4c46ca81c8b3`.
The normalized archive contains the full EditMode result report, including all
five Smoke identities and the P12-E cases. Original Unity XML/logs and all
CoreValidation raw TRX/logs remain in owner-only local evidence retention and
were not modified. No public .NET TRX package is claimed: the approved TRX
adapter does not accept this CoreValidation manifest schema.
