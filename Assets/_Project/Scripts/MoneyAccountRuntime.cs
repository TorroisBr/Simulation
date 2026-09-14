using System;
using UnityEngine;

[Serializable]
public sealed class MoneyAccountRuntime
{
    [SerializeField] private float balance;

    public float Balance => balance;

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
        if (CanCredit(amount) == false)
        {
            return false;
        }

        float nextBalance = balance + amount;
        balance = nextBalance;
        return true;
    }

    public bool TryDebit(float amount)
    {
        if (CanDebit(amount) == false)
        {
            return false;
        }

        float nextBalance = balance - amount;

        if (IsValidNonNegativeFiniteAmount(nextBalance) == false)
        {
            return false;
        }

        balance = nextBalance;
        return true;
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount)
    {
        return amount >= 0f
            && float.IsNaN(amount) == false
            && float.IsInfinity(amount) == false;
    }
}
