# P12-G registered Crime receipt owner witness

## Scope

This is a test-only source-inventory witness for the selected
`UnityBootstrap-Daily-v1` runtime. The composition test reads the provider
stored in the sealed P12 census registration for
`p12b.crime-p18-receipts` and compares it with the exact installed
`CrimeSystem`. It requires the current sentinel witness to report cardinality
1 and local revision 0 through the provider that the protocol actually
registered.

No production behavior or P12-G restore capability changed. This closes only
the source registration-to-owner check for this sentinel. It does not validate
a freshly reconstructed target owner, complete the 299-row live inventory or
writer/epoch matrix, implement whole-graph staging/rejection/atomicity, prove
no-replay or continuation parity, or change readiness. P12-G and P12-A remain
`WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open.

## Exact-tree validation

Validation ran on the isolated candidate based on P12 canonical
`b516a0e977954c823bf23a074e6d962b6b7346d2` using Unity `6000.3.9f1`.
Each XML is the Unity Test Framework result artifact. The raw Unity logs are
stored together in `UnityLogs.zip` (SHA-256
`7C9BFE22C03A9CCC6EE0F53610E47BDA6759607535125AD7D717E6F2374CDE7F`).

| Suite | Result | XML | XML SHA-256 |
|---|---:|---|---|
| `SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne` | 1/1 PASS | [`Focused/EditMode-20261010-014113-cbfdca8c459048c0b95f8786b031312b.xml`](Focused/EditMode-20261010-014113-cbfdca8c459048c0b95f8786b031312b.xml) | `771889B9832B6C9362DB1A15FD019A80B43997E1761E6739E1094B4D0A6C71EF` |
| ALL EditMode | 2740/2740 PASS | [`AllEditMode/EditMode-20261010-014127-766cb22c71ed45de8e9f6b4d7e95bd95.xml`](AllEditMode/EditMode-20261010-014127-766cb22c71ed45de8e9f6b4d7e95bd95.xml) | `C6DCCC838E445BCBEEC695D543568EE72F3CC713A2386315131B5EE654269EAC` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 PASS | [`OfficialSmoke/EditMode-20261010-014204-1c314e9aed994c69be69e2387aae10dc.xml`](OfficialSmoke/EditMode-20261010-014204-1c314e9aed994c69be69e2387aae10dc.xml) | `BE54623CC9B286772965B83DD169BE041C9A3818EA93222A477E1B9116D808F3` |

`git diff --check` passed for the code diff.
