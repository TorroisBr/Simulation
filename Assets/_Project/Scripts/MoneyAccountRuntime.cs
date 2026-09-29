using System;
using UnityEngine;

[Serializable]
public sealed class MoneyAccountRuntime
{
    [SerializeField] private float balance;
    [NonSerialized] private long revision;

    public float Balance => balance;
    public long Revision => revision;

    internal bool CanInstall(long expectedRevision, float replacement)
    {
        return revision == expectedRevision && revision < long.MaxValue && IsValidNonNegativeFiniteAmount(replacement);
    }

    internal void InstallPrepared(long expectedRevision, float replacement)
    {
        // Caller preflights every participant before beginning a synchronous install.
        balance = replacement;
        revision = expectedRevision + 1;
    }

    internal bool CanInstall(long expectedRevision, long revisionIncrements, float replacement)
    {
        return revision == expectedRevision && revisionIncrements >= 0
            && revisionIncrements <= long.MaxValue - expectedRevision
            && IsValidNonNegativeFiniteAmount(replacement);
    }

    internal void InstallPrepared(long expectedRevision, long revisionIncrements, float replacement)
    {
        balance = replacement;
        revision = expectedRevision + revisionIncrements;
    }

    public MoneyAccountRuntime()
        : this(0f)
    {
    }

    public MoneyAccountRuntime(float initialBalance)
    {
        if (IsValidNonNegativeFiniteAmount(initialBalance) == false)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialBalance),
                initialBalance,
                "MoneyAccountRuntime requires a finite, non-negative initial balance.");
        }

        balance = initialBalance;
    }

    public bool CanDebit(float amount)
    {
        if (IsValidNonNegativeFiniteAmount(amount) == false
            || IsValidNonNegativeFiniteAmount(balance) == false
            || balance < amount)
        {
            return false;
        }

        if (amount == 0f)
        {
            return true;
        }

        float nextBalance = balance - amount;
        return IsValidNonNegativeFiniteAmount(nextBalance) == true
            && nextBalance < balance;
    }

    public bool CanCredit(float amount)
    {
        if (IsValidNonNegativeFiniteAmount(amount) == false
            || IsValidNonNegativeFiniteAmount(balance) == false)
        {
            return false;
        }

        if (amount == 0f)
        {
            return true;
        }

        float nextBalance = balance + amount;
        return IsValidNonNegativeFiniteAmount(nextBalance) == true
            && nextBalance > balance;
    }

    public bool TryCredit(float amount)
    {
        if (CanCredit(amount) == false || (amount > 0f && revision == long.MaxValue))
        {
            return false;
        }

        float nextBalance = balance + amount;
        balance = nextBalance;
        if (amount > 0f) revision++;
        return true;
    }

    public bool TryDebit(float amount)
    {
        if (CanDebit(amount) == false || (amount > 0f && revision == long.MaxValue))
        {
            return false;
        }

        float nextBalance = balance - amount;

        if (IsValidNonNegativeFiniteAmount(nextBalance) == false)
        {
            return false;
        }

        balance = nextBalance;
        if (amount > 0f) revision++;
        return true;
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount)
    {
        return amount >= 0f
            && float.IsNaN(amount) == false
            && float.IsInfinity(amount) == false;
    }
}
