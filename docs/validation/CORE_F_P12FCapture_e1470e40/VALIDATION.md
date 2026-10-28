# CORE-F P12-F Core-bound capture and save-operation lifetime

**Disposition: VALIDATED CANDIDATE only.** This bounded P12-F/Core-session integration is not CORE-F completion or Core canonical promotion. P12 and P13 status are unchanged.

## Candidate identity and scope

- Candidate branch: `codex/core/CORE-F-runtime-app-session-boundary-20261025`.
- Candidate commit/tree: `e1470e40c98cdd3cbf2b0ff2d6d2f67ebd0b5eba` / `f4f5a18982d151168f7ee2fd03e18e690bbd5ce3`.
- Candidate parent: `67e0904ae4907391d867119aaedeaf7f004b63c3`.
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The slice binds the selected PoliticalKnowledge store to the exact Core owner graph/PersonStore and keeps the Core-owned continuation-save witness active across P12-G owner staging, target admission, and continuation-image composition. The legacy route remains intact when no Core operation is present.

The change does not claim a Core-owned complete runtime, public `DailyV1Application.TryStart`, detached .NET host, or process-separated continuation. It does not change P12 profile scope or close CORE-F.

## Validation inputs and results

The successful focused, full EditMode, and Smoke executions share the stable Unity input SHA-256 `ebcc081e4563db3b1577593f3a534b9fe4ae47fe34b3729adce7e1f24beba9fc`. The runner manifests identify the preceding commit/tree `67e0904ae4907391d867119aaedeaf7f004b63c3` / `69b99ae...`; SHA-256 values for the changed production and test files were independently compared with the final candidate and match. The candidate commit contains the tested bytes without further source changes.

The two pre-existing local `ProjectSettings` edits remained outside the candidate commit and were unchanged by this correction. Their validated input hashes were `EditorBuildSettings.asset` `58bcbfb23da7aab5acd696e7d83e9d75a86442ed39a582159d2493049361ba28` and `ShaderGraphSettings.asset` `5c432c9c73fdf73fee91c8823ea02ab98e3b7a8edf341b5ae6901d699cce52a7`.

| Gate | Result | Run and retained evidence |
|---|---:|---|
| Focused operation-invalidation regression | 3/3 passed; no failures, skips, or inconclusive cases | Run `7671b1aa5d5a4a80a39837737632712e`; input stable; private manifest SHA-256 `3d52f3bcf886fd5c862ee7e7b5d4c478bc399d6d20ff5ea8305babcd4d282f09`; original XML SHA-256 `1b71dea38bcfab1ddcff2665dedee0fc40f71ef9b88de1dfccaaa7174dfa5491`; log SHA-256 `eda95780fc0efda7292718897ccbb34bbde65adc24c310da389d0d4397ef7370` |
| Full Unity EditMode | 3,269/3,269 passed; no failures, skips, or inconclusive cases | Run `fe531255baca4ee0a7b4a841b963fca1`; input stable; private manifest SHA-256 `9f764de3525df23c20fab7f66619202071bf2ed882371588455ef03b3f6945b5`; original XML SHA-256 `b3750893795a6581a0dbc0dc9e9c8000d77967938309780d04ec2610a08addfd`; log SHA-256 `b90fab8d16e8ffc94087fb1d4be4706adf4ff280fe98ec606f8db3363de59028` |
| Official Smoke filter | 5/5 passed; no failures, skips, or inconclusive cases | Run `016aed0163ce49d68cf00867838d51b5`; input stable and identical to full EditMode; private manifest SHA-256 `5c53e7a776db2ee8b72a4efcd531e89134c0c46eb066e7cb3ca0f581c28b4551`; original XML SHA-256 `dd322518e5a71bd8e9c2665fba5d213205e03f12ca4a77ddde9aff80c6e4502a`; log SHA-256 `905b4c978c5b3a51af3916da99374ca25387bd274992bc74806b445fe86bb6c0` |
| CoreValidation | 820/820 cold and warm; unchanged Core input files | Prior run `c6f6bde43433437f849a414795a0c49f`; private manifest SHA-256 `f87827117da33a0a97037d3b0e26d8c053902ed0bebd4ae81ade7071c5a60b80`. The e1470e40 correction changes no Core source or test file; exact unchanged Core inputs were independently verified. `worktreeLibraryAbsent=False`; this does not prove Library independence. |
| `git diff --check` | PASS | Candidate source/test diff |

An initial focused-run attempt `0f7575d3249344989fdad771e1307317` produced no XML because the new public NUnit method exposed an internal enum type (`CS0051`). It is excluded from all pass counts. The test parameter was changed to `int`, the same three cases were rerun successfully, and full EditMode plus Smoke passed afterward. The original failed log/manifest remain preserved privately and unchanged.

## Durable Unity result package

- Manifest: [p12f-capture-editmode-20261026.manifest.json](p12f-capture-editmode-20261026.manifest.json), SHA-256 `1868e4969840b43d85f001e75c746a5de45d32d90f0fe73d201f53e788bb4810`.
- Result archive: [p12f-capture-editmode-20261026.zip](p12f-capture-editmode-20261026.zip), SHA-256 `1468ff453855934068e659984c934e38e2554b0c34f6ab5f4f5484d2742e412a`, 173,256 bytes.
- Official package verification passed against the protected original full-suite manifest. The archive contains all 3,269 normalized NUnit results, including the focused cases and Smoke identities. The packager removed output fields and recorded the supported absolute local Assembly-suite path projection.
- Original XML, raw logs, and private manifests remain unchanged in owner-only E: evidence storage. Raw logs and private absolute-path metadata are not included in the public package.

The result package's source manifest records the pre-correction commit/tree. Final source applicability is established by the stable full input digest and exact hashes of changed code/test files, independently checked against candidate `e1470e40c98cdd3cbf2b0ff2d6d2f67ebd0b5eba`.

## Independent review

Exact-tip independent review passed at the candidate commit/tree; see [INDEPENDENT_REVIEW.md](INDEPENDENT_REVIEW.md). The candidate remains a bounded intermediate CORE-F capability only.
