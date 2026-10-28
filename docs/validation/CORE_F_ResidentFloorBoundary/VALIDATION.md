# CORE-F selected resident-floor boundary

This candidate moves the registered living-Person portion of selected Daily-v1
represented-resident floor calculation into Core. Core counts exact `PersonStore`
rows against its selected City owners and checks that a bound owner graph uses
the same `PersonStore` instance. Unity retains its existing full-roster presence
query and supplies only the residual beyond those Core-owned Person rows. The
ordinary runtime snapshot still reports the same total floor. This preserves
legacy and other host-only resident projections without claiming that Core owns
those facts.

The selected person-death commit, full runtime composition, public
`DailyV1Application.TryStart`, detached P12 restoration and the independent
.NET host remain outstanding. This is a candidate checkpoint, not Core
canonical or CORE-F completion.

## Exact input and validation

The change is based on candidate `7f7632db6e1e2c451cc56a1cf34eb4f2659ce555`,
committed tree `cf7f2ff9799638a9c511c23693af8c0d5467a24b`. The final official
CoreValidation input digest is
`3e2943c73ea7697e3b93e1b4e9a77c8c4b8f6012b0be2ba95b41b730106346d0`; its
private run manifest SHA-256 is
`7de790c5defbcfcfd3f357b5f2c01d99eda3834491045545546b749c5987e077`.
The current full Unity EditMode input snapshot is
`7cad7612dfbd4427e2d442b9f4651f6caaec122252ac91336a2f158861eedb63` and its
private manifest records `inputStable=true`. Current hashes for the Core stage,
Unity adapter, runtime and changed Unity regression test match the respective
official run manifests. The Core manifest also matches both modified Core test
sources. The Unity run includes the existing local ProjectSettings state in its
private input inventory; those user changes are excluded from this candidate.

- Official CoreValidation: cold 835/835 and warm 835/835; run
  `4626475ebcbd4182a79e8fc0363b3cc3`. The runner records that this worktree
  contains a Unity Library, so this result does not claim Library independence
  and is not packaged as portable .NET TRX evidence. Original reports and the
  private run manifest remain in protected E: evidence storage.
- Official full Unity EditMode: 3271/3271 passed, zero failed, skipped or
  inconclusive; current exact-source run `de3cb8e8426a47c18e93c2064c3cc145`;
  private manifest SHA-256
  `2471474af04a756c56e8cce3b061516dbe034cdb05bf69f016e6d274bc17fbec`;
  original XML SHA-256
  `4b6851164cdf40856b70aa2af079d2935c31d6f01d731c670d29a6f85b1125e0`;
  raw log SHA-256
  `1cc620bd1e00993239e1f1175bd94c9652738f217f28cdd498b8e040067c6e17`.
- Official filtered Smoke: 5/5 passed, zero failed or skipped; run
  `5cd1087a18224bf58e69d73eb61eea6e`. The full current EditMode run above
  includes those smoke cases on the current source snapshot.
- `git diff --check`: passed after preparing this record.

## Evidence package and limitations

The approved Unity evidence packager and verifier accepted the current full
EditMode report. The package is
[`coref-resident-floor-editmode-current-20261026.zip`](Evidence/coref-resident-floor-editmode-current-20261026.zip)
with manifest
[`coref-resident-floor-editmode-current-20261026.manifest.json`](Evidence/coref-resident-floor-editmode-current-20261026.manifest.json).
The archive SHA-256 is
`0747ddf439c2c5e7cc15b13f79c2899e34d81c61e755895c57fcb797672b9921`; the
manifest SHA-256 is
`7101a453423a8849600e108ff50d965fadacb938a8b68ac581a152db6ac5d93e`. Official
verification passed against the protected private run manifest.

The earlier Packaged full EditMode and the filtered Smoke XML remain preserved.
The strict packager rejected the filtered Smoke XML because its `Simulation`
and assembly ancestor nodes retain unfiltered `testcasecount` values while the
report contains only five cases. The count-integrity rule was not weakened, and
the original report was not modified. Its raw manifest, XML and log remain in
protected E: evidence storage. The full EditMode package contains the smoke
cases and is independently inspectable.

Independent exact-tip review of this resident-floor change is not yet recorded.
The earlier application-session follow-up review at `ad2b8d93` covers only its
separate five-file commit. The unrelated
`ProjectSettings/EditorBuildSettings.asset` and
`ProjectSettings/ShaderGraphSettings.asset` modifications remain unstaged and
excluded.