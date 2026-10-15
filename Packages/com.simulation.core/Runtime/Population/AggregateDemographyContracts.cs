using System;

/// <summary>
/// Structured reasons why an aggregate demographic proposal or application was rejected.
/// </summary>
public enum AggregateDemographyFailure
{
    None = 0,
    InvalidSettlement = 1,
    InvalidProvider = 2,
    InvalidRepresentedResidentFloor = 3,
    NegativeChange = 4,
    WouldUnderflow = 5,
    WouldOverflow = 6,
    WouldViolateRepresentedResidentFloor = 7,
    RevisionOverflow = 8,
    StaleState = 9,
    InvalidTransition = 10,
    RuntimeFaulted = 11,
    OperationIdentityConflict = 12
}

/// <summary>
/// Explicit randomness boundary for stochastic aggregate-demography providers. Providers,
/// rather than the aggregate state system, own and consume this dependency.
/// </summary>
public interface IAggregateDemographyRandomSource
{
    double NextUnitInterval();
}
