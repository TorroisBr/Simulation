using System;
using NUnit.Framework;

public sealed class DailyV1P18ReceiptOwnerTests
{
    [Test]
    public void CrimeOwnerPreparesCommitsReplaysAndRestoresReceiptAuthority()
    {
        var owner = new DailyV1CrimeP18ReceiptOwner();
        Assert.That(owner.IsExactEmpty, Is.True);
        Assert.That(owner.WasRestored, Is.False);
        Assert.That(owner.TryPrepare("crime-step-1", "fingerprint-a", out var mutation), Is.True);
        Assert.That(mutation.IsReplay, Is.False);
        Assert.That(mutation.Receipt.RevisionBefore, Is.Zero);
        Assert.That(mutation.Receipt.RevisionAfter, Is.EqualTo(1L));
        Assert.That(owner.CanCommit(mutation), Is.True);
        owner.InstallPrepared(mutation);
        Assert.That(owner.StepRevision, Is.EqualTo(1L));
        Assert.That(owner.CensusRevision, Is.EqualTo(2L));
        Assert.That(owner.TryPrepare("crime-step-1", "fingerprint-a", out var replay), Is.True);
        Assert.That(replay.IsReplay, Is.True);
        Assert.That(owner.CanCommit(replay), Is.True);
        Assert.That(owner.TryPrepare("crime-step-1", "different", out _), Is.False);
        Assert.That(owner.TryCaptureP12AReceiptOwnerSnapshot(out var snapshot), Is.True);
        var restored = new DailyV1CrimeP18ReceiptOwner();
        Assert.That(restored.TryInstallP12AReceiptOwnerSnapshot(snapshot), Is.True);
        Assert.That(restored.WasRestored, Is.True);
        Assert.That(restored.TryPrepare("crime-step-1", "fingerprint-a", out var restoredReplay), Is.True);
        Assert.That(restoredReplay.IsReplay, Is.True);
        Assert.That(restored.CensusRevision, Is.EqualTo(owner.CensusRevision));
    }

    [Test]
    public void JusticeOwnerKeepsThreeIndependentReceiptRevisionsAndRestoresThem()
    {
        var owner = new DailyV1JusticeP18ReceiptOwner();
        Assert.That(owner.IsExactEmpty, Is.True);
        var kinds = new[]
        {
            DailyV1JusticeP18ReceiptKind.BeginDay,
            DailyV1JusticeP18ReceiptKind.AdvanceSentences,
            DailyV1JusticeP18ReceiptKind.SyncWantedStatuses
        };
        for (int i = 0; i < kinds.Length; i++)
        {
            Assert.That(owner.TryPrepare(kinds[i], "justice-step-" + i, "fingerprint-" + i, out var mutation), Is.True);
            Assert.That(mutation.IsReplay, Is.False);
            Assert.That(owner.CanCommit(mutation), Is.True);
            owner.InstallPrepared(mutation);
        }
        Assert.That(owner.BeginDayRevision, Is.EqualTo(1L));
        Assert.That(owner.AdvanceSentencesRevision, Is.EqualTo(1L));
        Assert.That(owner.SyncWantedStatusesRevision, Is.EqualTo(1L));
        Assert.That(owner.CensusRevision, Is.EqualTo(6L));
        Assert.That(owner.TryPrepare(kinds[1], "justice-step-1", "fingerprint-1", out var replay), Is.True);
        Assert.That(replay.IsReplay, Is.True);
        Assert.That(owner.CanCommit(replay), Is.True);
        Assert.That(owner.TryPrepare(kinds[1], "justice-step-1", "changed", out _), Is.False);
        Assert.That(owner.TryCaptureP12AReceiptOwnerSnapshot(out var snapshot), Is.True);
        var restored = new DailyV1JusticeP18ReceiptOwner();
        Assert.That(restored.TryInstallP12AReceiptOwnerSnapshot(snapshot), Is.True);
        Assert.That(restored.WasRestored, Is.True);
        Assert.That(restored.CensusRevision, Is.EqualTo(owner.CensusRevision));
        Assert.That(restored.TryPrepare(kinds[2], "justice-step-2", "fingerprint-2", out var restoredReplay), Is.True);
        Assert.That(restoredReplay.IsReplay, Is.True);
    }

    [Test]
    public void P12ReceiptCodecRoundTripsCoreOwnedPopulatedReceiptLedgers()
    {
        var crimeOwner = new DailyV1CrimeP18ReceiptOwner();
        Assert.That(crimeOwner.TryPrepare("crime-step", "crime-fingerprint", out var crimeMutation), Is.True);
        crimeOwner.InstallPrepared(crimeMutation);
        var justiceOwner = new DailyV1JusticeP18ReceiptOwner();
        Assert.That(justiceOwner.TryPrepare(DailyV1JusticeP18ReceiptKind.BeginDay,
            "justice-step", "justice-fingerprint", out var justiceMutation), Is.True);
        justiceOwner.InstallPrepared(justiceMutation);
        Assert.That(crimeOwner.TryCaptureP12AReceiptOwnerSnapshot(out var crime), Is.True);
        Assert.That(justiceOwner.TryCaptureP12AReceiptOwnerSnapshot(out var justice), Is.True);
        Assert.That(P12AReceiptOwnerCodec.TryEncode(crime, justice, out var sections, out var encodeResult), Is.True);
        Assert.That(encodeResult, Is.Null);
        Assert.That(P12AReceiptOwnerCodec.TryDecode(sections, out var decodedCrime, out var decodedJustice,
            out var decodeResult), Is.True);
        Assert.That(decodeResult, Is.Null);
        Assert.That(decodedCrime.CensusRevision, Is.EqualTo(crime.CensusRevision));
        Assert.That(decodedJustice.CensusRevision, Is.EqualTo(justice.CensusRevision));
    }
}