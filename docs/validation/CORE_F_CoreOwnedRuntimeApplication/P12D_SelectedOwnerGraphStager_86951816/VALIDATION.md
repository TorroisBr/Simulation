# P12-D selected owner graph stager review correction

This record covers the bounded NPC-only selected-owner compatibility correction. It does not claim P12-D or CORE-F completion, P12-A readiness, canonical promotion, or Library-independent .NET validation.

## Source identity and change

- Candidate branch: `codex/core/CORE-F-p12d-selected-owner-stager`.
- Implementation commit: `86951816c898df8e79d913667d4394bdf3c8a57d`.
- Implementation tree: `d343fafb645bb597720e89d8cd0d6201c0757d1e`.
- Base: `425ea732cee2831860955d4446b57396526eaf65`.
- The P12-D owner collector now accepts zero City owners with a nonempty selected NPC roster. The Core graph stager has the same admission rule. A completely empty owner vector remains rejected, matching the prior exact-owner graph construction contract.
- Added Core and Unity regressions for the NPC-only vector and preservation of the exact NPC owner reference. Existing assertions were not weakened.
- The change does not make image restoration accept an empty City snapshot; its existing image-row precondition remains unchanged.

## .NET validation

Official `Tools/CoreValidation/Invoke-CoreValidation.ps1`, run `26bbb3b9351f4ef8a8e54be3eaf729fd`: **PASS**. CoreValidation passed **718/718 cold and 718/718 warm**. Locked restores, CalendarDemo cold/warm, Simulation.Lab build, and the fixed cold/warm Lab scenario also passed with exit code 0.

- SDK: `9.0.315`.
- Source commit/tree: `86951816c898df8e79d913667d4394bdf3c8a57d` / `d343fafb645bb597720e89d8cd0d6201c0757d1e`.
- Captured input digest: `0bb384879e086ca9243a0aef744baec1fb5e28bed5cfe2a5c2e8950ac70cd1d1`.
- Protected private runner manifest: 1,107,381 bytes, SHA-256 `6b24b13766549c575900af3575b70b97596650d1fc6d1bbcaf58c703b33636cd`.
- Original cold TRX: 1,025,753 bytes, SHA-256 `48e12bc48fa4d62e9a245923630fcdad1c5f4af7a2b43bcb3535c0259a90e6be`.
- Original warm TRX: 1,025,753 bytes, SHA-256 `cafb0e47af0f1c0fac4ea3c62a3f6a2764b92216bfc302f9549f785070dd0c73`.
- The runner reported that this worktree contains a Unity `Library`; this run does not establish Library independence. No public TRX package is claimed because the required `dotnet-trx-source-context-v1` adapter context was not emitted.

## Unity validation

Official wrapper, Unity `6000.3.9f1`, full EditMode run `aab07cadccbc48ca9dfb66a86c3bc40e`: **PASS, 3262/3262**. The full report contains all five test identities selected by the established `Smoke` filter.

- Source commit/tree: `86951816c898df8e79d913667d4394bdf3c8a57d` / `d343fafb645bb597720e89d8cd0d6201c0757d1e`.
- Input digest before and after: `4e1525555121cae6ece57a47e45acf9d087777bef5201f29dc24ab39586681f0` (stable).
- Protected original XML: 3,263,816 bytes, SHA-256 `c5fe262127f056986f8f3d44a0937e6cb6c4142d3f0ead137f0d2faf00df707d`.
- Separate official filtered Smoke run `3563de51a6d54a43be4f7149165a5ab6`: **5/5**, zero failed or skipped, with the same source commit/tree and stable input digest. Its original XML SHA-256 is `bb726bbc1fcd4b099ea901a440ea5731296476e1565ea6fb71d89ef9e9e3ea58` (6,358 bytes). No separate filtered public package is claimed; the full EditMode package contains those same five passing cases.
- Privacy-safe full EditMode bundle: `coref-p12d-editmode-full-86951816`.
- Public ZIP: 172,386 bytes, SHA-256 `c35ab9f0cfc975a1f96386cbded75c9ba09bb79de957fbb2f48b5a112757c541`.
- Public manifest in the Windows checkout: 443,069 bytes, SHA-256 `e78ba6683023b165e9109d07bf6d63c6b152c43a416f785f6e86baae34f8a380`. Committed LF Git blob: 433,601 bytes, SHA-256 `6752a3cf1240f0790c2f7bb54cbab84c7535ecb352b186a240c45f952dbc7ff1` (Git blob ID `006d727f88c55703b70834d1dc06a1f7d6db3d1a`).
- Evidence package verification against the protected private source manifest: **PASS**, one run and one normalized XML entry.

## Review and promotion boundary

The review of the preceding evidence-only tip `425ea732cee2831860955d4446b57396526eaf65` returned **NEEDS_CHANGES** for the empty-City restriction and requested the manifest line-ending convention be explicit. Both findings are addressed here. The exact-tip review of `86951816c898df8e79d913667d4394bdf3c8a57d` is pending. No canonical promotion was performed.
