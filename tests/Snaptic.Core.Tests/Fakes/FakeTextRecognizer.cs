using SkiaSharp;
using Snaptic.Core.Abstractions;

namespace Snaptic.Core.Tests.Fakes;

public sealed class FakeTextRecognizer : ITextRecognizer
{
    public bool IsAvailable { get; init; } = true;
    public string? TextToReturn { get; init; }
    public Exception? ThrowOnRecognize { get; init; }
    public IReadOnlyList<string> Languages { get; init; } = ["en-US"];

    public string? LastLanguageUsed { get; private set; }

    public IReadOnlyList<string> GetAvailableLanguages() => Languages;

    public Task<string?> RecognizeAsync(SKBitmap image, string languageTag, CancellationToken ct = default)
    {
        LastLanguageUsed = languageTag;
        if (ThrowOnRecognize is not null) throw ThrowOnRecognize;
        return Task.FromResult(TextToReturn);
    }
}
