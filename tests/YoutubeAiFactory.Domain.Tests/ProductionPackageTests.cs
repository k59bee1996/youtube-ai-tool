using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Production;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class ProductionPackageTests
{
    [Fact]
    public void Edit_requires_revalidation_and_approval_freezes_package()
    {
        var package = Create();
        package.RecordEdit(
            "Updated visual",
            "Measured pacing",
            "Muted colors",
            "Readable type",
            "Sparse score",
            "Protect the packaging variable.",
            DateTimeOffset.UtcNow
        );
        Assert.Equal(ProductionGroundingStatus.Pending, package.GroundingStatus);
        Assert.Null(package.GroundingAiRunId);
        Assert.Throws<DomainException>(() => package.Approve(DateTimeOffset.UtcNow));
        package.RecordGroundingResult(
            ProductionGroundingStatus.Passed,
            Guid.NewGuid(),
            "[]",
            DateTimeOffset.UtcNow
        );
        package.Approve(DateTimeOffset.UtcNow);
        Assert.Equal(ProductionPackageStatus.Approved, package.Status);
        Assert.Throws<DomainException>(() =>
            package.RecordEdit("x", "x", "x", "x", "x", "x", DateTimeOffset.UtcNow)
        );
    }

    [Fact]
    public void Rejects_invalid_duration_and_lineage_versions() =>
        Assert.Throws<DomainException>(() =>
            new ProductionPackage(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                Guid.NewGuid(),
                1,
                Guid.NewGuid(),
                1,
                1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "engine",
                "prompt",
                1,
                "fingerprint",
                "provider",
                "model",
                "English",
                0,
                "visual",
                "pacing",
                "color",
                "type",
                "audio",
                "experiment",
                "[]",
                "[]",
                DateTimeOffset.UtcNow
            )
        );

    [Fact]
    public void Shot_rejects_notes_that_cannot_fit_the_database_column() =>
        Assert.Throws<DomainException>(() =>
            new ProductionShot(
                Guid.NewGuid(),
                1,
                ProductionShotType.Illustration,
                "Supported illustration",
                "Wide",
                "Static",
                10,
                1,
                ProductionFactualityMode.GenericAtmosphere,
                null,
                new string('n', 4_001)
            )
        );

    private static ProductionPackage Create() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            1,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "engine",
            "prompt",
            1,
            "fingerprint",
            "provider",
            "model",
            "English",
            120,
            "visual",
            "pacing",
            "color",
            "type",
            "audio",
            "experiment",
            "[]",
            "[]",
            DateTimeOffset.UtcNow
        );
}
