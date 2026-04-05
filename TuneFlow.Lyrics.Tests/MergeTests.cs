using FluentAssertions;
using TuneFlow.Lyrics.Merging;

namespace TuneFlow.Lyrics.Tests;

public class MergeTests
{
    [Fact]
    public void Merge_TranslationTrack_ShouldAttachMatchedContent()
    {
        var origin = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-0-original.lrc"));
        var translation = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-1-translation.lrc"));

        var merged = LyricsFacade.Merge(origin, translation, new MergeOptions
        {
            MergeType = MergeType.Translation,
            MaxTimeDeltaMs = 100
        });

        merged.Lines.Length.Should().Be(origin.Lines.Length);
        merged.Lines.Should().OnlyContain(line => line.Romanization == null);
        merged.Lines[2].Translation.Should().NotBeNull();
        merged.Lines[2].Translation!.Text.Should().Be("呐呐呐。 发送了@你的");
    }

    [Fact]
    public void Merge_RomanizationTrack_ShouldAttachMatchedContent()
    {
        var origin = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-0-original.lrc"));
        var romanization = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-2-romanization.lrc"));

        var merged = LyricsFacade.Merge(origin, romanization, new MergeOptions
        {
            MergeType = MergeType.Romanization,
            MaxTimeDeltaMs = 100
        });

        merged.Lines.Length.Should().Be(origin.Lines.Length);
        merged.Lines.Should().OnlyContain(line => line.Translation == null);
        merged.Lines[2].Romanization.Should().NotBeNull();
        merged.Lines[2].Romanization!.Text.Should().StartWith("ne e ne e ne e");
    }

    [Fact]
    public void Merge_AllowUnmatchedFalse_ShouldDropUnmatchedLines()
    {
        var origin = LyricsFacade.Parse(TestResourceLocator.ReadAllText("mini-origin.lrc"));
        var translation = LyricsFacade.Parse(TestResourceLocator.ReadAllText("mini-translation-shifted.lrc"));

        var merged = LyricsFacade.Merge(origin, translation, new MergeOptions
        {
            MergeType = MergeType.Translation,
            MaxTimeDeltaMs = 100,
            AllowUnmatched = false
        });

        merged.Lines.Select(line => line.Text).Should().ContainInOrder("hello", "world");
        merged.Lines.Select(line => line.Text).Should().NotContain("bye");
    }

    [Fact]
    public void Merge_AllowUnmatchedTrue_ShouldPreserveOriginLineWhenNoMatch()
    {
        var origin = LyricsFacade.Parse(TestResourceLocator.ReadAllText("mini-origin.lrc"));
        var translation = LyricsFacade.Parse(TestResourceLocator.ReadAllText("mini-translation-shifted.lrc"));

        var merged = LyricsFacade.Merge(origin, translation, new MergeOptions
        {
            MergeType = MergeType.Translation,
            MaxTimeDeltaMs = 100,
            AllowUnmatched = true
        });

        merged.Lines.Length.Should().Be(3);
        merged.Lines.Single(line => line.Text == "bye").Translation.Should().BeNull();
    }

    [Fact]
    public void Merge_SameDocument_ShouldNotCreateSelfReference()
    {
        var origin = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-0-original.lrc"));

        var merged = LyricsFacade.Merge(origin, origin, new MergeOptions
        {
            MergeType = MergeType.Translation,
            MaxTimeDeltaMs = 0
        });

        merged.Lines.Should().OnlyContain(line => line.Translation == null);
    }

    [Fact]
    public void Merge_ShouldDetachAttachedTrackInstance()
    {
        var origin = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-0-original.lrc"));
        var translation = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-1-translation.lrc"));

        var merged = LyricsFacade.Merge(origin, translation, new MergeOptions
        {
            MergeType = MergeType.Translation,
            MaxTimeDeltaMs = 100
        });

        var mergedTrack = merged.Lines[2].Translation;
        var sourceTrack = translation.Lines.Single(line => line.StartTime == mergedTrack!.StartTime);

        ReferenceEquals(mergedTrack, sourceTrack).Should().BeFalse();
        mergedTrack!.Translation.Should().BeNull();
        mergedTrack.Romanization.Should().BeNull();
    }
}
