# CORE-F Shared Runtime Source — independent technical-design review

**Verdict:** PASS for the bounded compatibility design.

| Identity | Value |
|---|---|
| Core canonical design base | `652ac7bf02943cf6160fd27f271a781db70657c5` |
| Architecture baseline | `758d4eaf7022e09f4b106ef25b390d14d6b55f7a` |
| Reviewed design SHA-256 | `36137D97DDBB633ECDE6C82E5EE1F7B60ED98F5457781B506531E254DECDE2FE` |
| Integrated Unity Editor fixture SHA-256 | `B0713750A006BBF1A5B75AD70EE183CF3CD0A2581D940DF49335B2037B70D5B5` |
| Numeric vector set SHA-256 | `2A5D2C653CEC82421936939A0FFBABABBB2F12B8834AFA645B890D884DEAF2F1` |

The independent reviewer verified that the design and evidence describe the
same accepted Unity Editor run (`d8b4c40e04ab466a9c56fdf6e5f5361d`): Unity
6000.3.9f1, Test Framework 1.6.0, 3,173/3,173 EditMode cases passed, zero
failures, and stable before/after inputs. The preserved raw XML SHA-256 is
`1c1bcfa171f5d3f38188fbe19bbf16bc7615baa4858ee4a0bb730ce18ebcbcda`; the raw
log SHA-256 is
`4125df2b07a5e392fac0942fd848515a6e496c45d2c5a8173bfe41378219efa5`. Both
originals remain in the account-protected evidence store outside Unity `Temp`.
The public manifest and archive hashes are recorded in the design. Verification
against the private run manifest passed.

All 247 public capture records matched the corresponding raw TestContext output
records. All 172 numeric vectors matched the final run's operation/payload
records exactly; independent comparison found no `Approximately` or `Pow`
result mismatch. The reviewer also verified the fixture/input identity, report
and package hashes, privacy projection, and protected evidence directory.

The PASS applies only to the bounded technical design for the frozen P18-D
codec boundary and selected numeric-compatibility surface. It does not claim
production implementation, owner/runtime migration, provider call-site switch,
restore or .NET host readiness, or CORE-F closure. Implementation remains
subject to exact-tip architecture/Core adoption and the specified .NET and
Unity parity gates.
