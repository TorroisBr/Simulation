# .NET TRX evidence — privacy-safe contract and bounded Master handoff

**Date:** 2026-10-13. **Workstream:** unnumbered validation infrastructure.
**Architecture base:** `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
**CORE-A evidence candidate:** `74677a0b5b74c7814874dd2d2b11aead2891bc42`;
implementation `2062e616d5a6bc53a2f154f33e9f4887a44972fe`, corrected runner
`f725fa87c6d1259d822180b84856b747c9e02980`.
**Existing shared packager source:** P12 canonical
`4cc31073f0ec6ba5c718084e4cebd6e914931d5d`, Tools/UnityValidation.
**Authority:** user requests privacy-safe evidence assessment, independently
reviewed design and documentation promotion; no tool implementation or CORE-A
promotion is performed by this record.
**Review/readiness:** [independent review](../architecture/DOTNET_TRX_EVIDENCE_REVIEW.md).
Only its PASS plus architecture promotion makes this Tools handoff READY_FOR_IMPLEMENTATION.
It does not claim a TRX adapter is already implemented.

## 1. Assessment and accepted strategy

Publishing byte-for-byte original TRX is not necessary to retain independently
inspectable, complete test-result evidence. It is necessary to preserve the
original bytes for original-to-derived comparison and diagnostic review under
policy retention. Accept two distinct artifacts:

1. Immutable original TRX and run manifest in verified access-controlled storage.
2. Complete privacy-safe normalized TRX reports and a provenance manifest in one
   durable, reviewer-accessible Git package (or a policy-qualified durable store).

The normalized reports are **not byte-identical originals**, not raw evidence,
and not reversible without the private context. The transformation is lossless
for the supported result-bearing model, with explicit authorized changes to
incidental metadata. Hashes prove byte identity/integrity relationships, not
availability, authenticity of execution, or source-to-derived equivalence by
themselves. Public archive verification checks the retained evidence; independent
comparison against accessible originals establishes the normalization attestation.
No new storage service, general evidence framework or CORE-A-only exception.

Extend the existing Evidence Packager with one explicit TRX format adapter.
Reuse protected-directory/file checks, hashes, deterministic ZIP encoding,
limits, privacy inspection, manifest provenance, Verify/Read and publication
review. Preserve the current NUnit adapter/schema/CLI default and existing
packages. Do not feed TRX to the NUnit parser, rename TRX to pretend NUnit,
hand-edit reports, rewrite case outcomes or replace reports with summary JSON.

Local-only originals are **not sole permanent promotion evidence**. Their
continued local presence does not establish durable public accessibility.
Complete normalized reports plus reviewed comparison/provenance satisfy the
permanent result obligation; protected originals retain their separate recovery,
review and diagnostic role. A permanently raw-dependent claim needs a qualifying
accessible protected archive for that claim or remains blocked.

## 2. Actual evidence and current limitations

Current independent implementation review `ad4d141d0ec00dee6b776d9373187173a330f0cc`
and docs applicability `9d2da57fa6ebb691c0ef49de96360621b583c742` pass, but both
explicitly preserve CORE-A's durable-evidence block. The candidate has no raw TRX
bundle and no implemented normalized TRX adapter. Its existing public manifest
is useful source/result provenance, not a substitute for the required reports.

The retained corrected run is `1aa63d7e04854212be037b09d299fccd`:

| Source artifact | Bytes | SHA-256 |
|---|---:|---|
| Cold TRX | 61,701 | `a69e23ba2e3f90ea63f673979c2128d00fb672ad6e87086fd587ee1f1b2b62f6` |
| Warm TRX | 61,701 | `7926c4e9a508976bdb6532a28f326dc3af30167ab16b6a8c29de7b5cfed8b0a4` |
| Private runner manifest | 68,178 | `5e189a598f3e1d21e465c150031e111c8c83041c4985b8377a23f5a017ea02ca` |
| Local original bundle | as retained | `e8a6814929a57a7b04a77fa81e2cd2acc8b12efe1aa5a44818fddde49209d1de` |

Both reports have distinct TestRun IDs, 45 definitions, 45 entries and 45 result
records, all Passed, Completed summary, total/executed/passed=45 and all other
native counters zero. Each contains one nonempty run-summary StdOut, local
account/computer metadata, assembly paths and a deployment directory label.
Neither contains case failure/error payload or attachments. Do not generalize
this passing sample into permission to remove failure diagnostics.

**New observed retention mismatch:** local originals and archive exist with
matching hashes, but the observed directory/files inherit ACLs including broader
principals. Their location is not verified owner-only protected evidence as
required by the existing packager. No disclosure is inferred. Before packaging,
copy without altering bytes into a freshly isolated owner-only directory using
the existing checked ACL mechanism, verify hashes and effective access, and
record remediation. Do not alter active implementation worktrees or rewrite the
historical claim into a protection PASS. No inherited broad access is accepted.

The old runner manifest binds source/tree, 27 input files, generated dependency
hashes and result records; it does not contemporaneously record DLL SHA/MVID.
Do not relabel arbitrary current binaries as historically executed artifacts.
Section 7 defines honest assembly binding and the bounded fallback if the actual
run outputs cannot be recovered. No unavailable original or identity is invented.

## 3. Supported format: trx-vstest-flat-v1

This is a closed profile for flat UnitTest TRX under namespace
`http://microsoft.com/schemas/VisualStudio/TeamTest/2010`, initially the identified
VSTest 17.14.1 producer used by CORE-A. It is not universal TRX support. Version
identity is an explicit manifest field, not guessed solely from namespace.
Retained command logs identify VSTest **17.14.1**, SDK 9.0.315 and CLR 8.0.28.
The logger binary identity recovered from that SDK is 247,632 bytes, SHA-256
`4a89511f6a3c00795e4119f4a3e192e58151c6f258e7c9b1f63aede73d76eec1`, product
`17.14.1-release-25428-01`. The testhost/Test.Sdk dependency is separately 17.12.0;
do not mistake that package pin for the actual logger version. Reverify producer
identity with source context; a new SDK/logger needs reviewed profile applicability.

No other namespace, nested InnerResults, ordered-test links, attachment/ResultFile,
data collector, external reference, unknown node/attribute or extension is
silently ignored. Unsupported structures fail packaging closed and remain local
for a separately reviewed adapter extension. No XML/network/attachment fetching.

Allowed element paths and attributes (namespace declarations only for this
namespace are syntactic; all other attributes are rejected):

| Element below TestRun | Allowed attributes / content |
|---|---|
| TestRun | id, name, runUser |
| Times | creation, queuing, start, finish |
| TestSettings | id, name; one Deployment child |
| TestSettings/Deployment | runDeploymentRoot |
| Results | UnitTestResult children only |
| Results/UnitTestResult | executionId, testId, testName, computerName, duration, startTime, endTime, testType, outcome, testListId, relativeResultsDirectory; optional Output |
| TestDefinitions | UnitTest children only |
| TestDefinitions/UnitTest | id, name, storage; one Execution and one TestMethod |
| TestDefinitions/UnitTest/Execution | id |
| TestDefinitions/UnitTest/TestMethod | codeBase, adapterTypeName, className, name |
| TestEntries/TestEntry | testId, executionId, testListId |
| TestLists/TestList | id, name |
| ResultSummary | outcome; one Counters; optional Output, RunInfos |
| ResultSummary/Counters | total, executed, passed, failed, error, timeout, aborted, inconclusive, passedButRunAborted, notRunnable, notExecuted, disconnected, warning, completed, inProgress, pending |
| ResultSummary/RunInfos/RunInfo | computerName, outcome, timestamp; one Text |
| Output (case or summary) | optional single StdOut, StdErr, DebugTrace, ErrorInfo children |
| Output/ErrorInfo | optional single Message and StackTrace |
| StdOut/StdErr/DebugTrace/Message/StackTrace/Text | text/CDATA only, no attributes |

Containers TestEntries, TestLists and RunInfos have no attributes. Mandatory root
sections Times/TestSettings/Results/TestDefinitions/TestEntries/TestLists/ResultSummary
occur exactly once. No duplicate singleton child; preserve child order. Definitions,
entries/results and lists must be nonempty and references must resolve; optional
output nodes may be empty. Preserve all observed original attributes, including
optional timing/result-directory fields; identity/outcome/required counts may
never be absent/empty. Reject DTD/entities, PI, comments and mixed/unrecognized
content instead of discarding it. Insignificant indentation and XML declaration
encoding are the only syntactic normalization outside the explicit field rules.
## 4. Exact privacy projection trx-private-metadata-v1

Preserve the complete accepted tree and original order, all case names/parameters,
GUIDs/run-local joins, adapter/type/assembly names, outcomes/counters, timestamps,
durations and error/failure/skip details. No nodes or output are dropped.
Only these value transformations are permitted:

| Location | Authorized deterministic transformation |
|---|---|
| TestRun/@name | `run-` followed by the unchanged TestRun/@id. This is a display label, not selection or run identity. |
| TestRun/@runUser | `<PRIVATE_RUN_USER>`; no username hash or raw value in public manifest. |
| Results/UnitTestResult/@computerName and ResultSummary/RunInfos/RunInfo/@computerName | Run-local `HOST-0001`, etc., by first appearance; same original name maps to same alias. No machine string/hash is published. |
| TestDefinitions/UnitTest/@storage and TestMethod/@codeBase | Replace the verified original worktree-root prefix with `<WORKTREE>/`, preserving validated relative assembly path. Already relative safe values remain unchanged; retain each original path's separator spelling after the prefix. |
| TestSettings/Deployment/@runDeploymentRoot | `<DEPLOYMENT>/` followed by unchanged TestRun/@id. It is an incidental output-root label; all semantic settings and selection remain elsewhere. |
| ResultSummary/Output/StdOut only | Replace exact, independently verified worktree-root prefixes with `<WORKTREE>/`; preserve every other character, line, adapter message and whitespace. No whole-output omission or account-name substitution. |

Literal placeholders are XML-escaped normally. Preserve absent/empty optional
metadata as absent/empty; do not add missing source attributes. Private context supplies original
worktree root and the run/assembly binding, obtained from actual run evidence,
not an arbitrary CLI search-and-replace expression. Match only a complete rooted
prefix with path-component boundary, support its explicitly recorded slash/case
spelling without rewriting the suffix, and reject traversal/root escapes,
ambiguous overlapping roots, preexisting reserved placeholders or conflated
assembly paths. Resolve assembly references to the declared safe relative path
and binary identities before projecting; no basename-only substitution. Relative
`storage` variants may have their original case; private comparison must establish
that they identify the same actual assembly as codeBase on the original host.

No other substitutions, blanket regex redaction, field deletion or output stripping.
In particular test IDs/names, class/method/adapter identity, selection, dates,
relativeResultsDirectory, error Message/StackTrace, case StdOut/StdErr/DebugTrace,
run StdErr/DebugTrace and RunInfo/Text stay unchanged as parsed text. CDATA/text
encoding may normalize, their combined value may not. Failure paths/credentials
in those immutable fields cause a privacy rejection, not an edited failure.
A bounded future diagnostic projection can be independently designed if needed;
this first profile does not pretend to solve arbitrary private assertion payloads.

The run-summary root-prefix alias preserves the path suffix and message content;
it may not remove an error or change its severity. A claim requiring the literal
original absolute prefix is outside this public profile and needs protected
claim evidence. In actual CORE-A sources the only two summary-output absolute
paths are under the verified worktree root, so this bounded rule fits them.
Do not assume that two permitted path occurrences make every future report safe.

Public manifest data is a **whitelisted projection**, never an unreviewed copy
of the private runner manifest. Apply the same display-name/host/path rules to
any corresponding manifest field, preserve result and effective command values,
and retain only portable relative input/artifact paths and semantic versions.
Keep raw manifest hash/length privately linked. Do not publish original scalar
username/machine/path values, path maps, per-secret hashes, environment dumps,
account ACL identifiers or unredacted logs. Publish safe role labels and retention
locator handles instead; private access/recovery instructions remain private.
Privacy-scan XML, manifest, ZIP names and actual outgoing Git object closure;
structural validity and a regex scan alone are not publication approval.

### Public manifest whitelist and source derivation

`validation-evidence-trx-package-v1` permits exactly the following public keys.
Containers and array-item shapes are closed; unspecified keys are rejected.
No raw private JSON object is passed through. All required evidence below must
be available/bound; absent facts cannot be filled from the current machine.

| Public field / object | Exact content and source |
|---|---|
| schema | Literal `validation-evidence-trx-package-v1`. |
| bundleId | Existing shared safe bundle ID syntax; stable input supplied once. |
| format | `{reportProfile,counterProfile,projectionPolicy}` with literals `trx-vstest-flat-v1`, `vstest-17.14.1-flat`, `trx-private-metadata-v1`. |
| tool | `{commit,tree,version,files,runtime}` from actual approved packager; files are portable `{path,bytes,sha256}` records. runtime has only powershellEdition, powershellVersion, dotnetVersion, compressionAssemblyVersion from existing pinned runtime assertion. |
| archive | `{file,bytes,sha256,format,entryPolicy}`; safe bundle ZIP filename, actual bytes/hash and existing fixed ZIP format; entryPolicy literal `normalized-trx-only`. |
| runs[] | Items described below, in original runner/attempt order. |

Each `runs[]` item has exactly:

| Field | Exact content and source |
|---|---|
| runnerRunId | Original private runner manifest runId, 32 hexadecimal characters; no replacement. |
| source | `{commit,tree,originalInputDigest,inputStable,files,generatedDependencies,influencingChanges}` from original source/input capture, verified against identified tree/context. files/generatedDependencies preserve their original ordered `{path,bytes,sha256}` entries; influencingChanges are `{path,gitStatus,bytes,sha256}` for actual dirty/untracked influencing inputs, empty for clean CORE-A. Each must be covered by the full input inventory; unresolved influence blocks. |
| originalManifest | `{bytes,sha256}` for actual original private runner manifest; never embed its private text. |
| producer | `{vstestVersion,sdkVersion,logger,identityMode,provenance}`. Required values 17.14.1, 9.0.315; logger `{file,fileVersion,productVersion,bytes,sha256}` has basename `Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger.dll`, file version 17.1400.125.42801, product 17.14.1-release-25428-01 and the section 3 identity. identityMode is captured or recovered-validated-output, accurately labeled. provenance is the evidence-ref array below. |
| execution | `{sdkVersion,clrVersion,targetFramework,configuration,osVersion,osArchitecture,processArchitecture,cultures,packageLocks,provenance}`. Preserve actual safe semantic values from original capture/bound evidence, not installation paths/list-runtimes text. packageLocks are original relative `{path,bytes,sha256}` records already in inputs. No environment variables, usernames or machine-name field. |
| discovery | `{count,cases}` from original test discovery, exact ordered strings/multiset; no renaming or parameter stripping. |
| commands[] | `{label,executable,argv,exitCode,elapsedMilliseconds,derivation,provenance}` per known original operation. Exact rules below; no invented recorded argv. |
| assemblies[] | `{path,fullName,mvid,bytes,sha256,identityMode,observedAtUtc,provenance}` from actual section 7 binding; safe relative output path, original full assembly identity, UUID and byte identity; observation time supplied by fixed recorded/recovery context, not packaging wall clock. |
| attempts[] | `{label,nativeRunId,nativeRunLabel,originalReport,normalizedReport,summaryOutcome,nativeCounters,caseOutcomeCounts,resultCount,gatePassed,exitCode,elapsedMilliseconds,comparison}`. label cold/warm for current run; nativeRunLabel is `run-` plus unchanged TRX id. originalReport `{bytes,sha256}`; normalizedReport `{entry,bytes,sha256,semanticSha256}`. Counts/outcomes/IDs from independently parsed report; command facts from bound original run; comparison literal `source-projection-verified` only after actual full comparison. Complete cases/details remain in the archived TRX, not duplicated/abridged in JSON. |
| demonstrations[] | `{label,exitCode,elapsedMilliseconds,output,derivation,provenance}` for claimed human-demo evidence, preserving complete safe original output. Current CORE-A cold/warm CalendarDemo lines are required, unmodified; sensitive/unsupported output blocks, not silently omitted. |
| measurements | `{worktreeLibraryAbsent,projectBinObjBytes,resultBytes}` copied from original measurement, with only portable `{path,bytes}` storage entries. No current-machine recomputation labeled as original. |
| retention | `{handle,ownerRole,policy,plannedPostPromotionDays,aclVerification,provenance}`; exact handle/access rules below. policy is this contract version, planned days 30 subject to recorded binding limits, aclVerification literal `verified-owner-only-known-copies` only after actual checks. |
| existingReviews | `{sourceImplementation,toolImplementation}` full existing Git refs; the new independent comparison/package attestation is external to this manifest, avoiding a self-hash cycle. |

`provenance` arrays use only `{kind,commit,blob,path,bytes,sha256,field,mode}`:
Git refs/blob/path identify public source or the captured runner; private artifacts
use hash/length, safe filename and a static schema field pointer, never an absolute
locator. Only applicable keys are present. kind is `git-source`, `original-manifest`,
`original-trx`, `retained-command-log` or `retained-output`. mode is `captured` or
`recovered-validated-output`. A record that cannot supply required fields fails
binding, rather than using an untyped free-text claim. Recovery facts are attested
in the separate review; private maps/access instructions stay under retention handle.

All SHA fields are lowercase 64-hex; Git refs 40/64-hex as applicable, UUIDs parsed
and preserved. Sizes/counts are nonnegative invariant integers. Input/artifact
paths must be safe relative paths, unique at their documented scope, with no
absolute root, traversal, URI, credential/query or account directory; path spelling
from original source inputs is preserved, not silently relabeled. The original
input-digest algorithm/order/version is bound to the captured runner blob; never
hash the differently projected public object and call it the old input digest.
Text fields, labels and refs still undergo full privacy/content inspection.
Unknown source fields carrying results/required inputs fail closed; declared
incidental source-only fields (for example local paths/run labels) are retained
privately and omitted/projected exactly as above, never passed through by default.

Command derivation is closed:

- `executable` is literal `dotnet`; `label` is one of sdk-version, dotnet-info,
  dotnet-runtimes, restore-tests-locked, restore-demo-locked, cold-build, list-tests,
  cold-test, warm-test, cold-demo, warm-demo for current runner. Preserve original
  ordering and command labels; a different runner needs reviewed adapter support.
- `argv` is a token array preserving all option names/order/case and semantic values
  (projects, target/framework, Release configuration, filters, logger filename,
  no-build/no-restore and discovery). Transform only exact token values that are
  recorded worktree-root paths to `<WORKTREE>/` + unchanged relative suffix and
  exact evidence-root directory tokens to `<EVIDENCE>/` + unchanged run-relative
  suffix. An absolute path inside another token/string is unsupported unless the
  bound runner defines its exact token shape; no regex edits inside filters,
  quoted strings, logger configuration or other semantic values. For current
  `--results-directory`, replace its complete directory argument with
  `<EVIDENCE>/` + runnerRunId. Never publish private redaction maps.
- Current runner manifest does not capture argv arrays. For CORE-A use
  `derivation=recovered-from-bound-runner`: inspect exact captured Invoke-CoreValidation
  script blob, original command logs and original report/manifest associations.
  Reconstruct each literal argument array and resolved public relative project
  from that source; prove executed SDK/configuration/selection via those logs,
  discovery and TRX. The source exposes no arbitrary test-filter input. Record
  the script commit/blob and each inspected log hash in provenance. Never call
  reconstructed vectors contemporaneously recorded. Original elapsed/exit facts
  already recorded for gates are copied exactly; supporting command facts absent
  from the manifest require retained log/source evidence or are left unclaimed
  (optional supporting commands may be absent; mandatory build/test/demo commands
  and their claims may not be invented).
- Future vectors actually captured by an approved runner use `derivation=captured`,
  with original manifest field pointers/log linkage and the same projection.
  Selection claims must equal the command/discovery/report multiset; fixture
  cultures, TFM and configuration must agree with captured sources. Execution
  facts not directly captured but recovered from bound metadata are labeled with
  recovered provenance; installed-runtime inventories alone are not selected CLR
  execution proof. A required unresolved command/toolchain fact blocks binding.
- The independent private comparison constructs these expected public values
  from original sources/context and compares every public key/value/array. It
  also verifies all result-bearing original fields are either present in public
  TRX/manifest or explicitly unsupported. Whitelist validity and matching hashes
  alone do not establish that source provenance was truthful.

Retention handles are exactly `evidence-` plus a random 32-lowercase-hex UUID,
generated once when the private context is created and reused unchanged for
repackaging. They encode no account/path/machine data. ownerRole is literal
`GeneralOrchestrator`. The private mapping binds that handle to the exact selected
run copies, original hashes, ACL checks, owner/access/recovery instructions and
planned deadline/any binding-limit exception; the public package contains only
the handle, role, policy and safe attestation. No raw locator or per-secret hash.
If a binding rule requires a different duration, record a reviewed safe policy
exception ref rather than silently changing the retention promise.

## 5. Outcome accounting and counter semantics

Source TRX remains run authority. Packaging success is not a passing test gate.
Keep native ResultSummary/@outcome and **all 16 counter attributes verbatim**.
Preserve the raw result outcome of every case; derive a separately named
`caseOutcomeCounts` histogram, never repair or replace native counters.

The supported enum is Error, Failed, Timeout, Aborted, Inconclusive,
PassedButRunAborted, NotRunnable, NotExecuted, Disconnected, Warning, Passed,
Completed, InProgress, Pending. Unknown labels reject. Pending/InProgress or
incomplete selection can never satisfy a completed checkpoint gate; retain their
nonpassing status or reject as incomplete, never convert them to Passed.

For the pinned `vstest-17.14.1-flat` producer, validate:

- native total equals flat result-record count;
- native passed/failed equal their corresponding case counts;
- native executed equals passed + failed;
- the remaining native counters are zero as emitted by this producer;
- the derived histogram includes every result and sums to total; expose separate
  NotExecuted/NotRunnable and Inconclusive counts even when native counters are zero.

This producer does not populate the other counters from skipped results.
Do not force `executed + notExecuted = total` or edit the native XML to make that
false assumption hold for skipped cases. Source:
[VSTest 17.14.1 TrxLogger](https://github.com/microsoft/vstest/blob/v17.14.1/src/Microsoft.TestPlatform.Extensions.TrxLogger/TrxLogger.cs),
[TestRunSummary](https://github.com/microsoft/vstest/blob/v17.14.1/src/Microsoft.TestPlatform.Extensions.TrxLogger/ObjectModel/TestRunSummary.cs).
Native NotExecuted does not distinguish every originating framework reason;
keep its available reason/output and never invent a richer original status.
Other producer counter conventions require a separate reviewed profile.

Bind each result to exactly one TestEntry by (testId, executionId), to a UnitTest
by testId and its Execution ID, and to an existing TestList. Enforce unique
run/definition/entry/result IDs at their appropriate scopes and no unused or
unaccounted definitions/entries in this flat profile. Definition display name and
result display name are preserved separately; do not falsely require equality
for parameterized cases. Compare the exact discovered test-name multiset plus
all definition/method/class/parameter IDs; duplicate display names are not a
license to merge cases. Duplicate executions/retries need explicit distinct
attempt records; no selecting the green one and discarding the earlier result.

CORE-A gate PASS still requires: stable complete inputs, correct source/assembly
binding, nonempty exact expected case selection, every case Passed, zero of every
other derived outcome, Completed summary, successful original command exit and
no run-level Error/Failed/Aborted or undisposed warning/defect. Counter consistency
alone or Completed summary alone cannot pass a gate. Well-formed failing evidence
may be packaged with gatePassed=false and full diagnostics; malformed or unsupported
sources fail packaging. Public Verify must distinguish archive validity from gate
status. Synthetic skip/inconclusive/failure cases remain nonpassing in all forms.

## 6. Normalization, verification and cryptographic linkage

1. Verify original run manifest/report bytes and expected hashes against the
   retained source records; validate protected ACLs and no reparse/escape.
   Open originals read-only and hash before/after transformation. Reject mutation.
   Use bounded XML parsing (DTD/resolver disabled, limits at least as strict as
   existing 256 MiB/report, 512 MiB expanded package and 64 MiB manifest limits).
2. Parse the closed grammar, validate IDs/references/counters/selection, and
   independently reconstruct the original result graph and its gate disposition.
   Compare run-manifest cases/outcomes/cold-warm association against raw TRX;
   disagreements block, never overwrite one source with the convenient value.
3. Apply only section 4 projection. Serialize UTF-8 without BOM, LF, fixed XML
   declaration, ordinal expanded-name attribute ordering, unchanged child order,
   explicit text escaping, no insignificant indentation. Preserve significant
   whitespace and concatenated text/CDATA values exactly, including diagnostics.
   This is a defined internal serialization, not a claim of W3C C14N support.
4. Reparse the normalized report, validate the same closed grammar and result
   graph, and compare the full canonical projected original tree with it:
   element names/namespaces, presence, child order, attributes, text, every case
   and result-bearing field. Compare semantic bytes as well as hashes, counters,
   full case multiset and original gate status. A serializer-only self-comparison
   or comparing aggregates is insufficient.
5. Store each cold/warm attempt separately in stable ordinal ZIP paths:
   `results/<runner-run-id>/cold.trx`, `warm.trx` for CORE-A. General future gate
   labels must be safe unique manifest identifiers; retain exact native TestRun
   IDs and ordering. No coalescing cold and warm or their measurements.
   Reuse the packager's fixed ZIP metadata/compression/toolchain; deterministic
   bytes are guaranteed only within its identified pinned runtime.
6. Public manifest schema `validation-evidence-trx-package-v1` records format,
   counter/projection policy versions, tool commit/tree/blobs and runtime;
   original TRX/manifest hash+length; normalized hash+length+entry; projected
   semantic digest; complete result/gate counts and cold/warm provenance;
   source commits/trees, full original input inventories/digests, generated
   dependency identities, selection/portable arguments, SDK/CLR/OS/architecture,
   assembly records (section 7), original-to-normalized comparison disposition,
   existing tool/source review refs and private retention handle/deadline/owner role. No publication
   timestamp inside deterministic bytes; source execution times remain intact.
7. `Verify` without originals checks archive/entry hashes and lengths, safety,
   complete expected entries, report grammar/graph/counters/selection, manifest
   consistency, fixed serialization and reproducible archive bytes. It reports
   `packageIntegrityVerified`, actual gate status, and `originalComparisonNotPerformed`.
   It cannot prove original equivalence from a manifest/hash assertion alone.
8. `Verify` with original run context repeats the full source comparison and
   reports `originalComparisonVerified`. Independent reviewer must inspect both
   bytes/graphs (including all 45+45 current cases), verify allowed changes,
   privacy, input/assembly binding and exact tool tip. Record a durable attestation
   binding original/normalized hashes, policy/tool refs, tested source and review
   candidate; final metadata-only applicability remains required.

Archive-only checks remain usable without local secrets or source originals.
They establish consistency/integrity of the published evidence, not proof of a
trustworthy runner or continued original availability. Original comparison and
review provide the origin assurance; Git provenance gives accessible history.
## 7. Source, assembly identity and evidence reuse

Preserve the original runner commit/tree, input digest and every captured file
hash/length, including dirty/untracked influencing inputs, plus SDK/CLR/package/
configuration and effective selection. A final source commit differing only in
docs is not the originally executed commit: record both and prove applicability.
Separate **capture-time** runner/input facts from **packaging-time** adapter/tool
facts; never backdate the latter or recompute a different inventory and claim it
was the original digest.

Assembly manifest records portable path, SHA-256, bytes, full assembly name and
MVID for the executed test assembly and Core dependencies, configuration/TFM and
binding method. Future execution capture should hash these immediately before
and after selected test gates and reject changes. For already retained runs,
a generic `recovered-validated-output` method is permitted only when independent
review verifies the original TRX codeBase/storage targets and unchanged original
output context: exact run worktree/source and every captured input, actual binary
and matching PDB presence, file observations consistent with run times and the
original build/no-build commands. Preserve recovery time/method and limitations;
file timestamps alone, a rebuild or an arbitrary current checkout are insufficient.
Copy recovered original outputs into protected diagnostic retention, hash before/
after copying and before/after comparison. No execution attestation is inferred
from a PE hash. Do not publish DLL/PDB data merely to publish safe identity fields.

For CORE-A the original detached SDK worktree remains at `f725fa87...`, clean,
with **27/27 captured input identities matching**. Both source TRX assembly targets
resolve to retained output there; test/Core DLLs and PDBs exist, and their last
writes are consistent with the original run. The warm gate used no-build.
Read-only recovered identities (not contemporaneous manifest fields):

| Assembly | Bytes | SHA-256 | MVID |
|---|---:|---|---|
| Simulation.Core.Tests | 21,504 | `2bb9950f25fb6c8c935b022e4e8c43c9e636f013ca4462e81a7ec084eb616505` | `0ca9abd7-fa47-49be-93b8-3f3d0121b396` |
| Simulation.Core | 13,824 | `26569417bc8d538bb97746adf575108dfb212ca065e42922bdb99bdf2401f625` | `3bb1f3d1-3369-4cd5-bd28-d3994ee2e575` |

Master/reviewer must reverify this context at packaging time and preserve the
recovery attestation, not silently insert these into the historical manifest.
If outputs/context changed or cannot be bound, recover an existing qualifying
archive; if none exists, only the affected .NET run needs a correctly identified
rerun. That would resolve an actual identity gap, not a documentation-only rerun.
Nothing here makes current Unity-worktree binaries substitutes for the old SDK run.

Pure normalization and documentation changes do not rerun Unity or .NET.
The new Tools capability has its own synthetic/real-read-only validation. Reuse
CORE-A's previously reviewed calendar/test/runner results only after exact-tip
input and contract applicability review. Existing source/test and captured tool
identities remain recorded accurately. No blanket revalidation waiver.

If packaging-only files are integrated after a run, retain the original full
inventory and list the complete final-candidate delta. Independent review must
prove those changes affect only post-execution packaging, not the runner,
build/test selection, fixtures, source, assemblies, effective configuration or
captured execution. A broad recorded Tools directory may differ; do not claim
whole-inventory equality. Bind its old capture-time version and the new adapter
separately and run the Tools regression gates. If any actual execution input or
harness behavior changed, run affected fresh validation. Avoid merging the old
architecture checkout's executable tree or editing the active CORE-A worktrees.

## 8. Protected original retention and access

Apply the existing owner-only fixed-local-drive ACL checks to every known owned
copy of the selected run retained as private evidence, including private manifests, original ZIP/TRX,
recovered assembly observations/bytes needed for comparison and path maps.
Inherited owner-only file permissions within a verified isolated parent are okay;
inherited broad principals, reparse points, root escapes and ambiguous ownership
are not. Do not publish usernames/SIDs or ACL dumps. A readonly bit, directory
name, archive encryption label or hash alone is not effective access control.

For current originals: first inventory all known owned selected-run TRX, manifest,
logs and archive copies. Verify the exact source run directory is dedicated to
that run, owner-controlled, contains no unrelated files, and is not an active
implementation worktree/reparse path. Master may correct ACLs on **that exact
run directory and those files only**, using existing owner-only checks, while
preserving every content byte/hash. Verify ACL propagation/effective access on
all known copies. An isolated copy alone does not cure the broadly accessible
original. If isolation of the existing exact source cannot be safely established,
record the remaining copy/access and leave this gate blocked; no blanket parent
permission change or security bypass.

Where recovery/worktree removal warrants a separate diagnostic copy, create its
isolated destination before copying, hash source/destination and verify access.
The previous known source copy must also pass the scoped ACL checks; do not
leave it broad and claim complete remediation. Record secure locators and access/
recovery methods privately. Do not edit TRX content, rewrite metadata or delete
previous copies. Source worktree assemblies are read-only recovery inputs; protect
any diagnostic copies without changing permissions/content in active worktrees.
Private context/maps/intermediate data stay inside verified protected directories,
never an unprotected temporary folder. Public preview may leave that boundary only
after all privacy checks.
This handoff authorizes no automated deletion or blanket permission changes.
If access cannot be established under applicable rules, block packaging/access
rather than bypassing security controls.

Retention covers review, source comparison, promotion and the policy's planned
30-day post-promotion diagnostic window, subject to binding privacy/security
limits and necessary unresolved diagnostic claims. Record owner, planned deadline,
original hashes, protected locator and recovery/access procedure; no guarantee of
perpetual local storage. Keep originals longer where a material claim requires
it and permitted. If a binding limit requires earlier discard, compare originals
independently before permitted discard and retain complete safe decision evidence;
claims still requiring inaccessible raw values cannot pass. No automatic cleanup.

Before losing/removing the execution worktree, preserve required originals and
assembly/context evidence in the owned protected location. Owner maintains recovery
from worktree loss and gives authorized independent reviewers least-privilege
access without publishing secrets. If a claim requires permanent raw review,
local-only diagnostic retention is insufficient: establish an approved protected
store with durable access/backup/recovery or leave that claim blocked. The public
complete normalized Git evidence remains the permanent source for ordinary
result auditability; no external store is mandatory for normal CORE-A promotion.

## 9. Exact General Orchestrator handoff

**Tools slice:** TRX-EVIDENCE-v1, unnumbered. Scope is adapter/packaging/verification
and evidence recovery, with no runtime, domain or test behavior changes.
**Candidate branch:** `codex/tools/trx-evidence-v1`.
**Code base:** current reviewed integrated production; initial inspected base
`4cc31073f0ec6ba5c718084e4cebd6e914931d5d`. Architecture documents are imported
as a bounded delta, never by merging the architecture branch's old Assets tree.
**Integration target:** the Core infrastructure front `codex/core/canonical`.
If not yet created, initialize from reviewed production base, not from unpromoted
CORE-A code. Tools promotion does not promote CORE-A or unlock CORE-B/C by itself.
Record Tools delivery and the still-pending CORE-A evidence gate in the normal
owning `docs/CORE_STATE.md`; preserve existing candidate State/history on integration.
Master executes under its active authorized infrastructure mandate; this design
assignment does not implement Tools or promote CORE-A.

Expected implementation surface:

- Add `Tools/UnityValidation/TrxValidationEvidence.psm1`, closed grammar/profile,
  projection, source/derived comparison and Core manifest adapter.
- Extend `Package-UnityValidationEvidence.ps1` with `-ReportFormat UnityNUnit|DotNetTrx`
  (Package defaults to UnityNUnit), same Package/Verify/Read actions. Dispatch explicitly;
  old invocations/manifest schemas remain supported unchanged. Verify/Read infer
  and validate the declared package schema; reject conflicting format arguments.
- Reuse existing primitives from UnityValidationEvidence.psm1; any small visibility/
  dispatch changes must preserve exact capture/runner behavior. Do not import the
  simulation runtime or the current candidate's strict pass-only TRX assessor
  as the package's universal result model. Producer profile handles its own
  complete outcome accounting, including valid nonpassing runs.
- Add synthetic self-tests under Tools/UnityValidation/tests and update README.
  No new evidence service, independent ZIP library/manifest system, generic test
  runner, CORE-A special parser, attachment framework or .NET dependency upgrade.
- Private input context combines current `simulation-core-validation/v1` original
  manifest, original report paths and a reviewed assembly/recovery context record.
  Existing CLI RunManifestPath/VerifyRunManifestPath identify these versioned
  adapter-context manifests; no secret values in public arguments/outputs.

Define adapter context schema `dotnet-trx-source-context-v1`: original runner
manifest hash/length and protected local locator; exact source refs/input digest;
producer profile; one ordered record per gate containing safe gate label, native
runId, original TRX hash/length and protected locator; original command exit,
expected discovery multiset, assembly-binding record and private worktree-root
mapping; retention owner/deadline/ACL-check result. Values must agree with actual
sources, not merely the context's claims. Source manifest supplies source/input/
selection/result facts; the context supplies mapping/recovery, not replacement
results. Verify original-context-to-runner/TRX agreement before packaging.

Public package fields reuse shared integrity/version machinery with the explicit
TRX manifest schema; old Unity manifests/archives remain byte-verifiable. Use one
cumulative public package for current cold+warm reports; no raw original copies
in outgoing ancestry. Existing implementation/review refs remain recoverable.
An initially blocked normalized attempt is not an accepted historical package.
## 10. Required implementation validation and closure

Before this Tools capability can satisfy a promotion gate:

1. Independent exact-tip code review of the complete delta, grammar/projection,
   manifest/assembly binding, access controls, original comparison and unchanged
   Unity behavior. No author self-approval or green-count-only review.
2. New synthetic TRX tests cover all supported case/root outcomes, especially
   Failed, NotExecuted and Inconclusive; native-zero non-pass counters; empty/
   incomplete discovery; duplicate display names with distinct IDs/parameters;
   bad joins/counts/run IDs/summary; unknown namespace/attribute/child; nested
   cases, attachments, DTD/entities, PI/comments, invalid encoding and size limits.
3. Privacy/equivalence tests cover account/computer labels, storage/codeBase/
   deployment roots, exact summary StdOut root substitution and unchanged rest
   of text; no changes to failure reasons/errors/case output; sensitive immutable
   payload rejection; ambiguous roots/prefixes/escapes/case variants, alias collision,
   reserved tokens, Unicode/cultures and missing private mapping. Mutations of any
   one case, outcome, message, counter, manifest claim or cold/warm association
   must be detected even if totals or other hashes are recomputed.
4. Archive tests cover deterministic bytes on pinned runtime, ordinal ordering,
   path traversal/symlink/duplicate/unexpected/missing entry rejection, zip bombs,
   tampering and no-overwrite/partial-output cleanup. Original bytes remain
   unchanged across successful and rejected operations. ACL tests reject actual
   broad inheritance/ownership/reparse problems and verify secure-copy behavior.
5. Run the full existing packager synthetic suite (historical 41 assertions) and
   legacy runner suite (historical 32 tests), plus all new tests and diff-check.
   Counts are anchors, not ceilings. Verify a retained existing Unity package
   with unchanged public semantics and no schema/output-policy regression.
6. On the two current original CORE-A TRX reports, privately compare all source
   bytes/hashes, graph/identities/timings/counters/output against generated
   normalized records, confirm precisely authorized changes and no private data
   in the public archive/manifest/outgoing objects. Reverify the 27-input source
   context and recovered assembly bindings; attest all 45 cold + 45 warm cases.
7. Publish the reviewed Tools capability and durable normal State/provenance,
   then create the actual compliant CORE-A result package using that approved
   code. A proposed adapter, private prototype or preapproval sanitized report
   cannot satisfy the gate. The adapter's own validation record is distinct from
   the validation results it packages.

Public archive-only verification must work without private originals. Original-
comparison verification must fail or explicitly report not performed without
those originals; it may not silently pass the stronger claim. Safe manifests
record distinct tool/source/implementation review refs. The new comparison review
attestation lives in the ordinary review/State record and binds package hashes;
never create a manifest/review self-hash cycle.

The current pass-only Core runner is not broadened merely for packaging. For a
future failed run, packaging requires complete actually captured context just as
for a passing run. An incomplete failure.json or absent input/selection provenance
is not silently promoted to a complete run manifest. The tool may retain a valid
nonpassing report when its complete context is available; missing metadata leaves
that run's closure claim unresolved. Future capture hooks must persist full facts
on failure and capture binary identity; their execution behavior requires their
own focused regression/review. CORE-A needs no runner change or new execution
when its original complete context remains verifiable as inspected.

No Unity/.NET domain rerun merely to normalize evidence, change docs or inspect
reports. New Tools behavior requires its own real implementation validation above.
If the actual runner/harness, domain/test inputs or required claims change, refresh
only the affected execution gates and corresponding independent review. This is
ordinary scope-based applicability, not a general full-suite exemption.

## 11. CORE-A unblock and downstream readiness

**Decision:** a package conforming to this promoted contract, produced by the
approved implemented adapter and independently compared against originals, **may
satisfy CORE-A's durable .NET evidence gate**. The original-only publication clause
is prospectively replaced for this gate; no earlier report becomes compliant
retroactively and no old validation result/review is rewritten as a promotion.

The minimum finite execution path is:

```text
promoted TRX contract + reviewed Tools implementation
  -> ACL-isolated original copy + source/assembly recovery verification
  -> complete normalized cold/warm package + private independent comparison
  -> final CORE-A input/contract/review applicability + normal promotion preflight
  -> CORE-A promotion (General Orchestrator, separate from this architecture task)
  -> CORE-B/C refresh their actual dependencies and baseline
```

CORE-A's required .NET cold/warm 45/45, Unity calendar 64/64, P12 131/131,
Daily-v1 169/169, ALL EditMode 3080/3080 and Smoke 5/5 remain the recorded gate
set, with zero forbidden outcomes; this policy does not execute or waive them.
Keep historical Unity package and execution applicability intact. Final CORE-A
review must bind unchanged reviewed source/tests, old runs, new package/comparison,
actual final integrated tree and refreshed refs. Source/fixture/runner/harness
changes require appropriate fresh gates; docs/packaging-only successors use the
explicit applicability proof in section 7, never a fabricated input-equality claim.

Current remaining conditions are implemented/validated/reviewed TRX adapter,
verified original ACL remediation, durable normalized evidence and independent
final applicability. Original output recovery is feasible at inspection, not a
new run result. No further product/architecture choice is needed. CORE-A has
not been promoted by this document; CORE-B/C do not become implementation-ready
solely because the evidence contract is promoted. Their design candidates and
own dependencies remain recoverable and independently refreshable.

No P12 scope change, P13 readiness, Phase reopening, runtime implementation,
external publication bypass or protected-store service is introduced.
