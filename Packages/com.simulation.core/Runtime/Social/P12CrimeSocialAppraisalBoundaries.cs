/// <summary>Existing P12 owner mutation and operation seams for CrimeSocialAppraisal.</summary>
internal enum P12CrimeSocialAppraisalOperation
{
    TheftAcceptance,
    KnowledgeAndAppraisal
}

internal interface IP12CrimeSocialAppraisalOwnerMutationBoundary
{
    bool CanCommit(object owner);
    void Committed(object owner);
    void Fault();
}

internal interface IP12CrimeSocialAppraisalOperationBoundary
{
    bool TryBeginOperation(P12CrimeSocialAppraisalOperation operation);
    bool EndOperation(P12CrimeSocialAppraisalOperation operation);
}
