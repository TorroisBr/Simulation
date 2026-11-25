# CORE-F P12-E PoliticalDecision Core Census - Validation

Date: 2026-11-09

## Scope

Core now owns composition of the selected P12-E PoliticalDecision census row from the exact `PoliticalDecisionStore` in the selected Core owner graph. It binds the row to that exact store and its exact `PersonStore`, derives cardinality and revision from the owner witness, preserves the P12 section identity/schema/Required role, and rejects stale or foreign host rows. The row is removed from the Unity remainder only when its complete identity/cardinality/revision facts match; Core then reconstitutes the row before final census composition.

This slice does not change Justice Records, P18 receipt identity, any P12 schema or identity, or the accepted host-neutral identity contract. It does not claim P12-A readiness, complete P12 census coverage, detached restoration, or CORE-F completion.

## Exact implementation identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate base: `4621b39a0449a045b5bffeb54bf491ee8b5fa9a4`
- Implementation commit: `13938ffc4b44bf3ad5c678c9142772e5ca4bb810`
- Implementation tree: `c44318f323452fa6f154dbe2ff591814a2703382`
- `DailyV1RuntimeAdvanceStage.cs` SHA-256: `59ca5c0838f4ab5ffc4e6957f1f6f0b8d5f27e5fb023c1dea31e70acafdc0f9e`
- `DailyV1Application.cs` SHA-256: `45d290da7320c5a37637f7aa9c5847c6528fc5d04aa5b33beb820d04aadbed71`
- `DailyV1ApplicationSessionGateTests.cs` SHA-256: `be64c7fff91e4329a84a0abcf3e86030480887c3206aced9be777e33032f3e49`
- `DailyV1SelectedRuntimeOwnerGraphTests.cs` SHA-256: `ccf339f0218aa5c482d63dd0570bc27356a22428e9f59a7180bf383788b8478f`

The official test runs used the uncommitted candidate source snapshot based on the stated base. Each of the four validated file byte hashes is unchanged in the implementation commit. The following evidence-record commit changes documentation only.

## Validation

- Official CoreValidation run `9abe36abca684357a5c90abaca4a0638`: PASS, 1,081/1,081 cold and 1,081/1,081 warm; zero failed, errored, timed out, aborted, inconclusive, or unexecuted tests. Stable input digest: `d77b6198ab9305592912f02467cd0c4602583a07e5f534cd6939f5e0d7cbdf02`.
  - Manifest SHA-256: `1d574be326299c357a062c3febbaa479089b9ff71ec9884ad8262defdb278b3b`
  - Cold TRX SHA-256: `e4979331726252a707016c096e13040d334f606f6a4c60251f9bf2ff5d534179`
  - Warm TRX SHA-256: `4838a11e84bb80858c3552482ec1701a85079623c90233493e703c7dd738126d`
- Official full Unity EditMode run `c4f739b2668246e89f6001eb10a6ebd6`: PASS, 3,303/3,303; zero failed, skipped, or inconclusive. Unity Editor `6000.3.9f1`, harness `1.1.0`. The 1,978-file input snapshot was stable at `80d2a16389aaf102469f22a7c0633eefd35021a1bff1bd2d9a9b7291ffe366ac`.
  - Run-manifest SHA-256: `850b6fd48256e8e85671880d8f8849e41e5723c2ae2c0c672166ef3d59983f3f`
  - XML SHA-256: `013c73bfea266e4d57cc282658a6e6011c7a8b517dbf9503b1ce695894528cdf`
  - Raw log SHA-256: `e01e80bc7f8e09c0b04c255d8744c8dcfedfd5b434784f7571065a3adcbc56ac`
- Official Unity Smoke run `aee2e8e250774d6095a5947e15b58516`: PASS, 5/5; zero failed, skipped, or inconclusive. It used the same stable Unity input snapshot.
  - Run-manifest SHA-256: `7f5d7f075d88b40b5f392240fc04168b1ac1d5f618c20644d87bee6bf4a69be2`
  - XML SHA-256: `b0b752a8480030de2c73433385aa85c844ebc1e7826b24d221fbab85a4e6e02d`
  - Raw log SHA-256: `04d39509a6163f8b9a500d338829dd2ffc9941d80095cfa1fc3bbc70de0f6525`
- `git diff --check`: PASS after the final source/test edits and staged checkpoint review.

Official manifests confirmed stable inputs. Every listed report artifact was rehashed against its manifest with zero mismatches. Raw XML and logs remain in the official protected evidence store; this record publishes no private machine paths or report contents. CoreValidation executes the .NET test project; the Unity snapshot does not include that separate project.

## Review and integration boundary

The General Architect's independently promoted identity review `cbacf617` confirms that the shared P12 owner identity supports a host-neutral Core runtime while preserving Unity's exact compatibility identity; this slice does not modify that contract. The new implementation commit is a recoverable candidate checkpoint only. Independent exact-tip implementation review is pending. No Core canonical promotion has occurred.
