using System;

public static class OfficeSuccessionSystem
{
    public static bool TryPropose(
        SimulationRuntime world,
        OfficeId officeId,
        PersonId selectedCandidateId,
        long startAbsoluteDay,
        out OfficeSuccessionTransition transition,
        out OfficeSuccessionFailure failure)
    {
        transition = null;
        failure = OfficeSuccessionFailure.None;
        if (world == null)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.InvalidWorld,
                "A SimulationRuntime is required.");
            return false;
        }

        if (officeId == null)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.InvalidOfficeId,
                "A valid OfficeId is required.");
            return false;
        }

        if (world.TryGetOffice(officeId, out _) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.OfficeNotFound,
                "The office must be registered before succession.");
            return false;
        }

        if (world.IsOfficeVacant(officeId) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.OfficeNotVacant,
                "Office succession requires prior institutional vacancy recognition.");
            return false;
        }

        if (selectedCandidateId == null)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.InvalidSelectedCandidate,
                "An explicit selected candidate PersonId is required.");
            return false;
        }

        if (startAbsoluteDay < 0L || startAbsoluteDay > world.CurrentDay)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.InvalidStartDay,
                "StartAbsoluteDay must be within the current world timeline.");
            return false;
        }

        if (TryGetFormerIncumbent(world, officeId, out OfficeTenureRecord formerTenure) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.FormerIncumbentMissing,
                "A vacant office must have a closed tenure before succession.");
            return false;
        }
        PersonId formerIncumbent = formerTenure.Incumbent;

        if (world.TryBuildSuccessionCandidates(
                formerIncumbent,
                out SuccessionCandidateSnapshot candidates,
                out SuccessionCandidateQueryFailure candidateFailure) == false)
        {
            failure = MapCandidateFailure(candidateFailure);
            return false;
        }

        if (candidates.ContainsCandidate(selectedCandidateId) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.CandidateNotEligible,
                "The selected PersonId is not in the deterministic eligible candidate set.");
            return false;
        }

        if (world.PersonStore.TryGet(selectedCandidateId, out PersonRuntime selectedCandidate) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.CandidateNotRegistered,
                "The selected candidate must remain registered in the world.");
            return false;
        }

        if (TryValidateCandidateAtStart(
                world,
                selectedCandidate,
                startAbsoluteDay,
                out OfficeSuccessionFailure startFailure) == false)
        {
            failure = startFailure;
            return false;
        }

        transition = new OfficeSuccessionTransition(
            officeId,
            formerIncumbent,
            selectedCandidate,
            candidates,
            formerTenure,
            world.CurrentDay,
            startAbsoluteDay);
        return true;
    }

    public static bool TryApply(
        SimulationRuntime world,
        OfficeSuccessionTransition transition,
        out OfficeSuccessionFailure failure)
    {
        failure = OfficeSuccessionFailure.None;
        if (world == null)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.InvalidWorld,
                "A SimulationRuntime is required.");
            return false;
        }

        if (transition == null
            || transition.OfficeId == null
            || transition.SubjectPersonId == null
            || transition.ExpectedCandidate == null
            || transition.ExpectedCandidates == null
            || transition.ExpectedClosedTenure == null
            || transition.ExpectedWorldDay < 0L
            || transition.StartAbsoluteDay < 0L)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.InvalidTransition,
                "A valid office succession transition is required.");
            return false;
        }

        if (world.CurrentDay != transition.ExpectedWorldDay)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.StaleWorldDay,
                "The world day changed after office succession was proposed.");
            return false;
        }

        if (world.IsOfficeVacant(transition.OfficeId) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.OfficeNotVacant,
                "The office became occupied after succession was proposed.");
            return false;
        }

        if (TryGetFormerIncumbent(world, transition.OfficeId, out OfficeTenureRecord formerTenure) == false
            || formerTenure.Incumbent != transition.SubjectPersonId
            || ReferenceEquals(formerTenure, transition.ExpectedClosedTenure) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.StaleOffice,
                "The office vacancy history changed after succession was proposed.");
            return false;
        }

        if (world.TryBuildSuccessionCandidates(
                transition.SubjectPersonId,
                out SuccessionCandidateSnapshot currentCandidates,
                out SuccessionCandidateQueryFailure candidateFailure) == false)
        {
            failure = MapCandidateFailure(candidateFailure);
            return false;
        }

        if (string.Equals(
                currentCandidates.DiscoveryFingerprint,
                transition.ExpectedCandidateFingerprint,
                StringComparison.Ordinal) == false
            || currentCandidates.ContainsCandidate(transition.SelectedCandidateId) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.StaleCandidateSet,
                "The eligible candidate set changed after succession was proposed.");
            return false;
        }

        if (world.PersonStore.TryGet(
                transition.SelectedCandidateId,
                out PersonRuntime currentCandidate) == false
            || ReferenceEquals(currentCandidate, transition.ExpectedCandidate) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.CandidateNotRegistered,
                "The selected candidate registration changed after succession was proposed.");
            return false;
        }

        if (TryValidateCandidateAtStart(
                world,
                currentCandidate,
                transition.StartAbsoluteDay,
                out OfficeSuccessionFailure startFailure) == false)
        {
            failure = startFailure;
            return false;
        }

        if (world.TryAssignIncumbent(
                transition.OfficeId,
                transition.SelectedCandidateId,
                transition.StartAbsoluteDay,
                out InstitutionFoundationFailure assignmentFailure) == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.AssignmentFailed,
                assignmentFailure.ToString());
            return false;
        }

        return true;
    }

    private static bool TryGetFormerIncumbent(
        SimulationRuntime world,
        OfficeId officeId,
        out OfficeTenureRecord formerTenure)
    {
        return world.TryGetLatestClosedOfficeTenure(officeId, out formerTenure);
    }

    private static OfficeSuccessionFailure MapCandidateFailure(
        SuccessionCandidateQueryFailure failure)
    {
        return OfficeSuccessionFailure.Create(
            failure?.Code == SuccessionCandidateQueryFailureCode.CandidateNotRegistered
                ? OfficeSuccessionFailureCode.CandidateNotRegistered
                : OfficeSuccessionFailureCode.CandidateNotEligible,
            failure?.ToString() ?? "Candidate discovery failed.");
    }

    private static bool TryValidateCandidateAtStart(
        SimulationRuntime world,
        PersonRuntime candidate,
        long startAbsoluteDay,
        out OfficeSuccessionFailure failure)
    {
        failure = OfficeSuccessionFailure.None;
        if (candidate == null
            || (candidate.BirthAbsoluteDay.HasValue
                && candidate.BirthAbsoluteDay.Value > startAbsoluteDay)
            || candidate.IsDeadAt(startAbsoluteDay))
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.CandidateNotLiving,
                "The selected candidate must be factually alive on the office start day.");
            return false;
        }

        if (PersonMaturityQuery.TryCalculate(
                candidate,
                startAbsoluteDay,
                world.Calendar,
                world.Configuration.Population.MaturityAgeYears,
                out PersonMaturitySnapshot maturity,
                out _) == false
            || maturity.IsMature == false)
        {
            failure = OfficeSuccessionFailure.Create(
                OfficeSuccessionFailureCode.CandidateNotEligible,
                "The selected candidate must satisfy maturity semantics on the office start day.");
            return false;
        }

        return true;
    }
}

public static class EstateSuccessionSystem
{
    public static bool TryPropose(
        SimulationRuntime world,
        EstateId estateId,
        PropertyId propertyId,
        PersonId selectedCandidateId,
        long transferAbsoluteDay,
        out EstateSuccessionTransition transition,
        out EstateSuccessionFailure failure)
    {
        transition = null;
        failure = EstateSuccessionFailure.None;
        if (world == null)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.InvalidWorld,
                "A SimulationRuntime is required.");
            return false;
        }

        if (estateId == null)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.InvalidEstateId,
                "A valid EstateId is required.");
            return false;
        }

        if (propertyId == null)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.InvalidPropertyId,
                "A valid PropertyId is required.");
            return false;
        }

        if (selectedCandidateId == null)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.InvalidSelectedCandidate,
                "An explicit selected candidate PersonId is required.");
            return false;
        }

        if (transferAbsoluteDay != world.CurrentDay)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.InvalidTransferDay,
                "Estate succession transfers must apply on the current world day.");
            return false;
        }

        if (world.EstateStore.TryGet(estateId, out EstateRecord estate) == false)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.EstateNotFound,
                "The estate must be opened before estate succession.");
            return false;
        }

        if (world.PropertyOwnershipStore.TryGet(propertyId, out PropertyOwnershipRecord property) == false)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.PropertyNotFound,
                "The property must be registered before estate succession.");
            return false;
        }

        if (property.OwnerPersonId != estate.DeceasedPersonId)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.PropertyNotOwnedByEstate,
                "The property owner must match the estate deceased PersonId.");
            return false;
        }

        if (world.PersonStore.TryGet(estate.DeceasedPersonId, out PersonRuntime deceased) == false
            || deceased.IsDeadAt(world.CurrentDay) == false)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.DeceasedPersonNotFactuallyDead,
                "Estate succession requires a factually dead Person.");
            return false;
        }

        if (world.TryBuildSuccessionCandidates(
                estate.DeceasedPersonId,
                out SuccessionCandidateSnapshot candidates,
                out SuccessionCandidateQueryFailure candidateFailure) == false)
        {
            failure = MapCandidateFailure(candidateFailure);
            return false;
        }

        if (candidates.ContainsCandidate(selectedCandidateId) == false)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.CandidateNotEligible,
                "The selected PersonId is not in the deterministic eligible candidate set.");
            return false;
        }

        if (world.PersonStore.TryGet(selectedCandidateId, out PersonRuntime selectedCandidate) == false)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.CandidateNotRegistered,
                "The selected candidate must remain registered in the world.");
            return false;
        }

        if (PropertyTransferSystem.TryProposeTransfer(
                world.PersonStore,
                world.PropertyOwnershipStore,
                propertyId,
                selectedCandidateId,
                transferAbsoluteDay,
                out PropertyOwnershipTransferTransition propertyTransition,
                out PropertyTransferFailure propertyFailure) == false)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.PropertyTransferFailed,
                propertyFailure.ToString());
            return false;
        }

        transition = new EstateSuccessionTransition(
            estate,
            propertyId,
            selectedCandidate,
            candidates,
            propertyTransition,
            world.CurrentDay,
            world.EstateStore.Revision);
        return true;
    }

    public static bool TryApply(
        SimulationRuntime world,
        EstateSuccessionTransition transition,
        out EstateSuccessionFailure failure)
    {
        failure = EstateSuccessionFailure.None;
        if (world == null)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.InvalidWorld,
                "A SimulationRuntime is required.");
            return false;
        }

        if (transition == null
            || transition.EstateId == null
            || transition.PropertyId == null
            || transition.DeceasedPersonId == null
            || transition.SelectedCandidateId == null
            || transition.ExpectedEstate == null
            || transition.ExpectedCandidate == null
            || transition.ExpectedCandidates == null
            || transition.ExpectedPropertyTransfer == null
            || transition.ExpectedEstateStoreRevision < 0L)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.InvalidTransition,
                "A valid estate succession transition is required.");
            return false;
        }

        if (world.CurrentDay != transition.ExpectedWorldDay)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.StaleWorldDay,
                "The world day changed after estate succession was proposed.");
            return false;
        }

        if (world.EstateStore.Revision != transition.ExpectedEstateStoreRevision
            || world.EstateStore.TryGet(transition.EstateId, out EstateRecord currentEstate) == false
            || ReferenceEquals(currentEstate, transition.ExpectedEstate) == false)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.StaleEstate,
                "The estate changed after succession was proposed.");
            return false;
        }

        if (world.TryBuildSuccessionCandidates(
                transition.DeceasedPersonId,
                out SuccessionCandidateSnapshot currentCandidates,
                out SuccessionCandidateQueryFailure candidateFailure) == false)
        {
            failure = MapCandidateFailure(candidateFailure);
            return false;
        }

        if (string.Equals(
                currentCandidates.DiscoveryFingerprint,
                transition.ExpectedCandidateFingerprint,
                StringComparison.Ordinal) == false
            || currentCandidates.ContainsCandidate(transition.SelectedCandidateId) == false)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.StaleCandidateSet,
                "The eligible candidate set changed after succession was proposed.");
            return false;
        }

        if (PropertyTransferSystem.TryApplyTransfer(
                world.PersonStore,
                world.PropertyOwnershipStore,
                transition.ExpectedPropertyTransfer,
                out PropertyTransferFailure propertyFailure) == false)
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.PropertyTransferFailed,
                propertyFailure.ToString());
            return false;
        }

        return true;
    }

    private static EstateSuccessionFailure MapCandidateFailure(
        SuccessionCandidateQueryFailure failure)
    {
        return EstateSuccessionFailure.Create(
            failure?.Code == SuccessionCandidateQueryFailureCode.CandidateNotRegistered
                ? EstateSuccessionFailureCode.CandidateNotRegistered
                : EstateSuccessionFailureCode.CandidateNotEligible,
            failure?.ToString() ?? "Candidate discovery failed.");
    }
}
