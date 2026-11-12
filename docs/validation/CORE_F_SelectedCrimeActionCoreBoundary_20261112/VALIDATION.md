# CORE-F selected Crime action Core/P12 boundary — validation

Status: validated candidate checkpoint only. This record does not complete CORE-F or authorize Core-canonical promotion.

## Source identity

- Candidate code commit: `33704f60d0a940b53a77bc63f5820f84fa8159ef`
- Candidate tree: `5200c2b3f904569940ea4ab18b6d041893a28e3e`
- Parent/base: `fe566597ea63d8cfde1fd7779970cf0877bf3ab2`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Both validation runners recorded the parent HEAD `fe566597ea63d8cfde1fd7779970cf0877bf3ab2` / tree `8d2edf8ad4f1d8d559887b82910400acbcc27955` while the four candidate implementation/test files were uncommitted. After commit, we compared every captured Core source and generated-dependency hash (541 + 4) and every Unity input hash (1,982) against the committed/current inputs; there were zero mismatches. The .NET-only test path is outside the Unity inventory.
- Unity input digest: `a8a308481b2c307994c5f33c5c9dc995b15c54aa0ed38790dcf8aac370ba73f5` (`inputStable=true` for full EditMode, focused admission, and filtered Smoke).
- Core input digest: `694f1d1e8b4267a98e9b376a99b3518eba4107cfa34810a9df3097c0109569f4` (`inputStable=true`; 541 source/input records and 4 generated dependencies; all recorded bytes/hashes still match the candidate).

## Results

| Gate | Result | Run ID |
|---|---:|---|
| Official CoreValidation cold | 1,106/1,106 | `10e424b7d0e34dccb7bf4e1611658d95` |
| Official CoreValidation warm | 1,106/1,106 | `10e424b7d0e34dccb7bf4e1611658d95` |
| `SimulationRuntimeAdmissionTests` | 246/246 | `89a8e1d1654c4e7793415596a389c0a4` |
| Full Unity EditMode | 3,318/3,318 | `c9ab17887bc449998ff54761a78dabd2` |
| Official filtered Smoke (`EditMode`, filter `Smoke`) | 5/5 | `e50bb903d1f74fb1b09dd84b901ff7e1` |
| `git diff --check` on the candidate source commit | PASS | `33704f60d0a940b53a77bc63f5820f84fa8159ef` |

The full EditMode report contains the complete focused admission results and all five Smoke identities. The filtered focused and Smoke originals, all full logs, and CoreValidation cold/warm originals remain in private owner-only E: evidence retention. No raw generated report or log is included here.

## Unity result package

The approved Unity packager produced a normalized result-only package from the complete EditMode report. Independent package verification against the protected original run manifest returned `VALID`.

- Public manifest SHA-256: `77d8caef2fb2d1e07327539ed9311c0664136cf79b62f53d3f8a99317ff8a31b`
- Results ZIP SHA-256: `c0037bc59de262255ec3a126a117e49bb0d371b4a163f9b5e0c03ad8837ddf1d`
- ZIP bytes: `175530`
- The package contains the full 3,318-case EditMode report, including all five Smoke identities. The separate filtered reports remain private and unchanged.

## .NET evidence boundary

CoreValidation's original cold/warm TRX reports and runner manifest remain in protected local retention under run ID `10e424b7d0e34dccb7bf4e1611658d95`; they are not published. No normalized TRX package is claimed. The approved adapter requires a separate `dotnet-trx-source-context-v1` record; the Core runner manifest alone is not that context. An initial packaging call supplied the wrong input type and failed schema validation before producing an archive. No original report was edited, discarded, or republished. A permanent .NET report package remains a promotion prerequisite.

## Scope and limits

This checkpoint moves ordinary selected Hide/Escape P12 operation-scope ownership into Core while retaining the existing exact host binding, owner identities, section construction, thread/session checks, and Unity projection callbacks. ForcedEscape and non-admitted compatibility paths are unchanged. The candidate remains unpromoted; it does not claim complete CORE-F runtime/application/host delivery.