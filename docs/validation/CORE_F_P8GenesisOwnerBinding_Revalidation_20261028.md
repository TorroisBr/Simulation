# CORE-F selected P8 owner binding — review-fix revalidation

## Base and correction

This revalidation follows the independent NEEDS_CHANGES review of
`b14ded086a97ac65955f3248d747c8c5c4688844` (tree
`5d62efcd6d574f74609ed874e5caf16004c93d65`). It corrects the P14 City
local-material-flow regression fixture without changing production code.

The fixture retains a local production source and settlement identity while
omitting the optional spatial-anchor binding. Its Core City still reports
`HasLocalDailyMaterialFlow`; the graph-bound P8 anchor and position owners
remain exact-empty. The test obtains its complete vector from the live P8
census providers, asserts that the exact P8 witnesses match, then verifies
that P12 save admission rejects the excluded City-local-flow state. This
reaches the City capture rule instead of failing earlier on a fabricated P8
witness. Other test fixtures continue to use the live P8 owner witnesses.

The corrected Core test source SHA-256 is
`a3821beb0e24afd6f8c62368c233b8909c1aa8af778d725573e71035071328ec`. The
CoreValidation manifest for this run confirms that hash and reports stable
inputs. The three production sources are unchanged from the already validated
candidate and retain the hashes in
[`CORE_F_P8GenesisOwnerBinding_20261028.md`](CORE_F_P8GenesisOwnerBinding_20261028.md).

## Validation

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | 918/918 PASS | `a4f98426ae154ddd907fdd82af9f1f68` |
| Official CoreValidation warm | 918/918 PASS | `a4f98426ae154ddd907fdd82af9f1f68` |
| Unity EditMode | retained 3275/3275 PASS; production source inputs unchanged | `bf7a4b009a724389a5d79f273c5b2067` |
| Unity Smoke | retained 5/5 PASS; production source inputs unchanged | `fe83dc5d16c8481ab65a43887089f717` |
| `git diff --check` | PASS | corrected working candidate |

CoreValidation discovered 918 tests. Its cold and warm reports hash to
`b5b0a15204e470165a629129ea6ded3805a7656b48daac1b7c4641ddc4332517` and
`db323e588bc58f879e3b1de813b905dbc56068cdc541e48b43d97a7e3e62a47f` (each
1,316,720 bytes). Original reports, logs, and private manifests remain in
owner-only evidence retention. The previous official Unity reports remain
applicable because only the standalone .NET test fixture changed after those
runs; the Unity production-source hashes remain unchanged.

## Status and limits

The independent exact-tip review of the corrected candidate is pending. This
revalidation does not promote Core canonical or complete CORE-F. Core
canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
