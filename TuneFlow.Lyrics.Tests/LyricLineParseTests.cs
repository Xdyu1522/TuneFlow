using System.Collections.Immutable;
using FluentAssertions;
using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Tests;

public class LyricLineParseTests
{
    [Fact]
    public void Parse_OriginalTrack_ShouldProduceImmutableSortedLines()
    {
        var content = TestResourceLocator.ReadAllText("song-0-original.lrc");

        var result = LyricsFacade.Parse(content);

        result.Lines.Should().BeOfType<ImmutableArray<ILyricLine>>();
        result.Lines.Should().OnlyContain(line => line is LyricLine);
        result.Lines.Length.Should().BeGreaterThan(70);
        result.Lines.Should().BeInAscendingOrder(line => line.StartTime);
        result.Lines[0].Text.Should().Be("作词 : ピノキオピー");
    }

    [Fact]
    public void Parse_TrackWithoutMetaTags_ShouldKeepMetaEmpty()
    {
        var content = TestResourceLocator.ReadAllText("song-1-translation.lrc");

        var result = LyricsFacade.Parse(content);

        result.Meta.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Parse_MetaSample_ShouldPopulateMetaFields()
    {
        var content = TestResourceLocator.ReadAllText("meta-sample.lrc");

        var result = LyricsFacade.Parse(content);

        result.Meta.Title.Should().Be("Sample Song");
        result.Meta.Artist.Should().Be("TuneFlow");
        result.Meta.Album.Should().Be("Unit Test Album");
        result.Meta.EmbeddedOffsetMs.Should().Be(120);
        result.Lines.Length.Should().Be(2);
        result.Lines[1].Text.Should().Be("world");
    }
}
