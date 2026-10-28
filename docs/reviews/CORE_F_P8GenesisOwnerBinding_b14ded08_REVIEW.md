# CORE-F P8 genesis owner binding — independent review of `b14ded08`

## Verdict

**NEEDS_CHANGES** for candidate `b14ded086a97ac65955f3248d747c8c5c4688844`
(tree `5d62efcd6d574f74609ed874e5caf16004c93d65`), based on
`4b025b3ad18dbf6283cc44ba6d809863b9686d9c`. Core canonical at review was
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Finding

The production identity binding looked consistent: Core genesis composed the
position owner against the exact `PersonStore` and spatial authority, bound the
exact position and legacy-anchor owners into the selected graph, and the Unity
selected path retained those instances. The compatibility path kept its prior
clone behavior.

The regression `SaveAdmissionRejectsCoreCityLocalMaterialFlowState` did not
prove the behavior named by the test. It used generic census rows after genesis
had bound exact P8 owners. With a P14 City local-flow fixture that also created
a spatial anchor, the generic P8 rows mismatched the live graph-bound witness;
P8 validation rejected the evidence before the City P14 capture exclusion was
reached. Green tests therefore did not establish the claimed City capture
behavior.

The test must use live, exact P8 witnesses and reach the City exclusion, or be
relabelled as a P8 mismatch test with separate P14 capture coverage. The
candidate must not be accepted until that gap is corrected, CoreValidation is
rerun for the changed test source, and the corrected exact tip is reviewed.

The reviewer did not independently open the protected CoreValidation and Unity
manifests during this review. The reported validation values are retained in
the candidate record but were not independently verified as part of this
review.
