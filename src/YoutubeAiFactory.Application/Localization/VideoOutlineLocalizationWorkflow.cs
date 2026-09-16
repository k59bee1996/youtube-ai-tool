using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Localization;

namespace YoutubeAiFactory.Application.Localization;

public sealed record LocalizedOutlineText(int Index, string Text);
public sealed record LocalizedOutlineSection(Guid SectionId, string Heading, string Objective, string Summary,
    string? ViewerQuestion, string? TransitionIntent);
public sealed record LocalizedVideoOutlineContent(string CanonicalContentFingerprint, string CoreQuestion, string CoreTension,
    string OpeningHookConcept, string ViewerPromise, string NarrativeProgression, string Payoff,
    string PacingStrategy, string HowOutlineImplementsExperiment,
    IReadOnlyList<LocalizedOutlineText> RisksToExperimentIntegrity,
    IReadOnlyList<LocalizedOutlineText> Warnings, IReadOnlyList<LocalizedOutlineSection> Sections);
public sealed record VideoOutlineLocalizationStatusDto(LocalizedVideoOutlineContent? Content,
    AnalysisJobDto? ActiveJob, AnalysisJobDto? LatestJob);

public sealed class RequestVideoOutlineLocalizationHandler(IYoutubeAiFactoryStore store,
    ArtifactLocalizationOptions options, TimeProvider timeProvider)
{
    public async Task<RequestArtifactLocalizationResult> HandleAsync(Guid projectId, Guid videoProjectId,
        Guid outlineId, string locale, CancellationToken cancellationToken)
    {
        var normalizedLocale = RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(locale);
        var outline = await store.GetVideoOutlineAsync(projectId, videoProjectId, outlineId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video outline was not found.");
        var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.VideoOutline, outlineId,
            outline.Outline.Version, normalizedLocale, cancellationToken);
        if (cached is not null)
        {
            if (LocalizedVideoOutlineCache.IsValid(outline, cached)) return new(null, "Completed", true);
            await store.DeleteArtifactLocalizationAsync(cached.Id, cancellationToken);
        }
        var payload = JsonSerializer.Serialize(new ArtifactLocalizationJobPayload(projectId, Guid.Empty,
            outline.Outline.Version, normalizedLocale, VideoOutlineId: outlineId),
            RequestCompetitorAnalysisLocalizationHandler.JsonOptions);
        var job = new Job("artifact-localization", payload, timeProvider.GetUtcNow(), options.MaxJobRetries,
            projectId: projectId, videoProjectId: videoProjectId, artifactType: LocalizableArtifactTypes.VideoOutline,
            artifactId: outlineId, artifactVersion: outline.Outline.Version, locale: normalizedLocale);
        var persisted = await store.EnqueueArtifactLocalizationJobAsync(job, cancellationToken);
        return new(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class GetVideoOutlineLocalizationHandler(IYoutubeAiFactoryStore store)
{
    public async Task<VideoOutlineLocalizationStatusDto> HandleAsync(Guid projectId, Guid videoProjectId,
        Guid outlineId, string locale, CancellationToken cancellationToken)
    {
        var normalizedLocale = RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(locale);
        var outline = await store.GetVideoOutlineAsync(projectId, videoProjectId, outlineId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video outline was not found.");
        var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.VideoOutline, outlineId,
            outline.Outline.Version, normalizedLocale, cancellationToken);
        var active = await store.GetActiveArtifactLocalizationJobAsync(LocalizableArtifactTypes.VideoOutline, outlineId,
            outline.Outline.Version, normalizedLocale, cancellationToken);
        var latest = active ?? await store.GetLatestArtifactLocalizationJobAsync(LocalizableArtifactTypes.VideoOutline,
            outlineId, outline.Outline.Version, normalizedLocale, cancellationToken);
        var content = cached is not null && LocalizedVideoOutlineCache.IsValid(outline, cached)
            ? JsonSerializer.Deserialize<LocalizedVideoOutlineContent>(cached.ContentJson,
                RequestCompetitorAnalysisLocalizationHandler.JsonOptions) : null;
        return new(content, active is null ? null : new(active.Id, active.Status.ToString(), active.FailureReason),
            latest is null ? null : new(latest.Id, latest.Status.ToString(), latest.FailureReason));
    }
}

public static class VideoOutlineLocalizationPrompt
{
    public const string Key = "video-outline-localization";
    public const int Version = 1;
    public static AiModelProfile ModelProfile => AiWorkflowProfiles.ArtifactLocalization;

    public static LlmRequest Create(VideoOutlineWithDetails outline, bool correcting) => new(Key, Version,
        "Translate only supplied reader-facing outline planning text into Vietnamese. Echo canonicalContentFingerprint exactly. Keep every section ID, list index, list order, count, and null field exactly. Do not translate or alter IDs, enums, statuses, versions, timestamps, claim/conflict/gap references, URLs, metrics, provider/model metadata, or workflow state. Do not add facts, narration, research, claims, or script prose. Return JSON only.",
        $"Canonical dynamic outline content:\n{JsonSerializer.Serialize(LocalizedVideoOutlineValidator.Source(outline), RequestCompetitorAnalysisLocalizationHandler.JsonOptions)}" +
        (correcting ? "\nPreserve identities and list topology exactly." : string.Empty),
        new Dictionary<string, string> { ["max_output_tokens"] = "7000" }, Schema(), ModelProfile);

    private static JsonNode Schema() => JsonNode.Parse("""
    {"type":"object","additionalProperties":false,"properties":{
      "canonicalContentFingerprint":{"type":"string","minLength":64,"maxLength":64},
      "coreQuestion":{"type":"string"},"coreTension":{"type":"string"},"openingHookConcept":{"type":"string"},
      "viewerPromise":{"type":"string"},"narrativeProgression":{"type":"string"},"payoff":{"type":"string"},
      "pacingStrategy":{"type":"string"},"howOutlineImplementsExperiment":{"type":"string"},
      "risksToExperimentIntegrity":{"type":"array","items":{"$ref":"#/$defs/indexedText"}},
      "warnings":{"type":"array","items":{"$ref":"#/$defs/indexedText"}},
      "sections":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{
        "sectionId":{"type":"string","format":"uuid"},"heading":{"type":"string"},"objective":{"type":"string"},
        "summary":{"type":"string"},"viewerQuestion":{"type":["string","null"]},"transitionIntent":{"type":["string","null"]}
      },"required":["sectionId","heading","objective","summary","viewerQuestion","transitionIntent"]}}
    },"required":["canonicalContentFingerprint","coreQuestion","coreTension","openingHookConcept","viewerPromise","narrativeProgression","payoff","pacingStrategy","howOutlineImplementsExperiment","risksToExperimentIntegrity","warnings","sections"],
    "$defs":{"indexedText":{"type":"object","additionalProperties":false,"properties":{"index":{"type":"integer","minimum":0},"text":{"type":"string"}},"required":["index","text"]}}}
    """)!.DeepClone();
}

public static class LocalizedVideoOutlineValidator
{
    public static void Validate(VideoOutlineWithDetails canonical, LocalizedVideoOutlineContent localized)
    {
        var source = Source(canonical);
        if (!string.Equals(localized.CanonicalContentFingerprint, source.CanonicalContentFingerprint, StringComparison.Ordinal) ||
            RequiredFields(localized).Any(string.IsNullOrWhiteSpace) ||
            !PreservesIndexes(localized.RisksToExperimentIntegrity, source.RisksToExperimentIntegrity.Count) ||
            !PreservesIndexes(localized.Warnings, source.Warnings.Count) ||
            localized.Sections.Count != source.Sections.Count ||
            !localized.Sections.Select(item => item.SectionId).SequenceEqual(source.Sections.Select(item => item.SectionId)))
            throw new StructuredOutputException("Localized video outline does not preserve canonical identities and topology.");
        for (var index = 0; index < source.Sections.Count; index++)
        {
            var original = source.Sections[index];
            var translated = localized.Sections[index];
            if (string.IsNullOrWhiteSpace(translated.Heading) || string.IsNullOrWhiteSpace(translated.Objective) ||
                string.IsNullOrWhiteSpace(translated.Summary) ||
                (original.ViewerQuestion is null) != (translated.ViewerQuestion is null) ||
                (original.TransitionIntent is null) != (translated.TransitionIntent is null) ||
                translated.ViewerQuestion is not null && string.IsNullOrWhiteSpace(translated.ViewerQuestion) ||
                translated.TransitionIntent is not null && string.IsNullOrWhiteSpace(translated.TransitionIntent))
                throw new StructuredOutputException("Localized video outline section content is incomplete.");
        }
    }

    internal static LocalizedVideoOutlineContent Source(VideoOutlineWithDetails outline)
    {
        var item = outline.Outline;
        var risks = JsonSerializer.Deserialize<string[]>(item.ExperimentRisksJson,
            RequestCompetitorAnalysisLocalizationHandler.JsonOptions) ?? [];
        var warnings = JsonSerializer.Deserialize<string[]>(item.WarningsJson,
            RequestCompetitorAnalysisLocalizationHandler.JsonOptions) ?? [];
        return new(CreateFingerprint(outline), item.CoreQuestion, item.CoreTension, item.OpeningHookConcept, item.ViewerPromise,
            item.NarrativeProgression, item.Payoff, item.PacingStrategy, item.HowOutlineImplementsExperiment,
            risks.Select((text, index) => new LocalizedOutlineText(index, text)).ToArray(),
            warnings.Select((text, index) => new LocalizedOutlineText(index, text)).ToArray(),
            outline.Sections.OrderBy(section => section.Sequence).Select(section => new LocalizedOutlineSection(section.Id,
                section.Heading, section.Objective, section.Summary, section.ViewerQuestion, section.TransitionIntent)).ToArray());
    }

    public static string CreateFingerprint(VideoOutlineWithDetails outline)
    {
        var item = outline.Outline;
        var material = JsonSerializer.Serialize(new
        {
            item.CoreQuestion,
            item.CoreTension,
            item.OpeningHookConcept,
            item.ViewerPromise,
            item.NarrativeProgression,
            item.Payoff,
            item.PacingStrategy,
            item.HowOutlineImplementsExperiment,
            item.ExperimentRisksJson,
            item.WarningsJson,
            Sections = outline.Sections.OrderBy(section => section.Sequence).Select(section => new
            {
                section.Id,
                section.Heading,
                section.Objective,
                section.Summary,
                section.ViewerQuestion,
                section.TransitionIntent,
            }),
        }, RequestCompetitorAnalysisLocalizationHandler.JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private static string[] RequiredFields(LocalizedVideoOutlineContent value) =>
        [value.CoreQuestion, value.CoreTension, value.OpeningHookConcept, value.ViewerPromise,
            value.NarrativeProgression, value.Payoff, value.PacingStrategy, value.HowOutlineImplementsExperiment];
    private static bool PreservesIndexes(IReadOnlyList<LocalizedOutlineText> values, int count) =>
        values.Count == count && values.Select(item => item.Index).SequenceEqual(Enumerable.Range(0, count)) &&
        values.All(item => !string.IsNullOrWhiteSpace(item.Text));
}

public static class LocalizedVideoOutlineCache
{
    public static bool IsValid(VideoOutlineWithDetails outline, ArtifactLocalization localization)
    {
        if (localization.CreatedAt < outline.Outline.UpdatedAt) return false;
        try
        {
            var content = JsonSerializer.Deserialize<LocalizedVideoOutlineContent>(localization.ContentJson,
                RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
                ?? throw new StructuredOutputException("Stored localized outline is empty.");
            LocalizedVideoOutlineValidator.Validate(outline, content);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or StructuredOutputException or InvalidOperationException)
        {
            return false;
        }
    }
}
