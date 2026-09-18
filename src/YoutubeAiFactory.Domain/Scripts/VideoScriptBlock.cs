using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Scripts;

public sealed class VideoScriptBlock
{
    private VideoScriptBlock() { }

    public VideoScriptBlock(Guid videoScriptSectionId, int sequence, ScriptBlockType type, string text, int wordCount)
    {
        Id = Guid.NewGuid();
        VideoScriptSectionId = Guard.NotEmpty(videoScriptSectionId, nameof(videoScriptSectionId));
        if (sequence < 1) throw new DomainException("Script block sequence must be positive.");
        if (!Enum.IsDefined(type)) throw new DomainException("Script block type is invalid.");
        Sequence = sequence;
        Type = type;
        UpdateText(text, wordCount);
    }

    public Guid Id { get; private set; }
    public Guid VideoScriptSectionId { get; private set; }
    public int Sequence { get; private set; }
    public ScriptBlockType Type { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public int WordCount { get; private set; }

    public void UpdateText(string text, int wordCount)
    {
        if (wordCount < 1) throw new DomainException("Script block word count must be positive.");
        Text = Guard.Required(text, nameof(text), 20_000);
        WordCount = wordCount;
    }
}
