using System.Collections.Immutable;
using FluentAssertions;
using TuneFlow.Lyrics.Exporting;
using TuneFlow.Lyrics.Merging;
using TuneFlow.Lyrics.Models;

namespace TuneFlow.Lyrics.Tests;

public class LyricExporterTests
{
    [Fact]
    public void Export_Interleaved_ShouldOutputOriginalThenTranslationThenRomanizationPerLine()
    {
        var document = CreateSampleDocument();

        var result = LyricExporter.Export(document, new LyricExportOptions
        {
            ExportFormat = ExportFormat.Lrc,
            ExportMode = ExportMode.Interleaved,
            LineBreak = "\n"
        });

        result.Should().Be(
            "[00:01.000]orig-1\n" +
            "[00:01.050]trans-1\n" +
            "[00:01.080]roma-1\n" +
            "[00:02.000]orig-2\n" +
            "[00:02.030]trans-2\n" +
            "[00:02.060]roma-2");
    }

    [Fact]
    public void Export_Interleaved_WithTranslationOnly_ShouldExcludeRomanization()
    {
        var document = CreateSampleDocument();

        var result = LyricExporter.Export(document, new LyricExportOptions
        {
            ExportFormat = ExportFormat.Lrc,
            ExportMode = ExportMode.Interleaved,
            LineBreak = "\n",
            IncludeKinds = ImmutableHashSet.Create(LyricTrackKind.Translation)
        });

        result.Should().Be(
            "[00:01.000]orig-1\n" +
            "[00:01.050]trans-1\n" +
            "[00:02.000]orig-2\n" +
            "[00:02.030]trans-2");
    }

    [Fact]
    public void Export_Interleaved_ShouldSkipMissingTrackLines()
    {
        ILyricLine line1 = new LyricLine
        {
            StartTime = TimeSpan.FromSeconds(1),
            Text = "orig-1",
            Translation = new LyricLine { StartTime = TimeSpan.FromMilliseconds(1050), Text = "trans-1" }
        };
        ILyricLine line2 = new LyricLine
        {
            StartTime = TimeSpan.FromSeconds(2),
            Text = "orig-2"
        };
        var document = new LyricDocument
        {
            Lines = ImmutableArray.Create(line1, line2),
            TrackKind = LyricTrackKind.Original
        };

        var result = LyricExporter.Export(document, new LyricExportOptions
        {
            ExportFormat = ExportFormat.Lrc,
            ExportMode = ExportMode.Interleaved,
            LineBreak = "\n",
            IncludeKinds = ImmutableHashSet.Create(LyricTrackKind.Translation)
        });

        result.Should().Be(
            "[00:01.000]orig-1\n" +
            "[00:01.050]trans-1\n" +
            "[00:02.000]orig-2");
    }

    [Fact]
    public void Export_Separated_ShouldOutputBlocksByLanguageOrder()
    {
        var document = CreateSampleDocument();

        var result = LyricExporter.Export(document, new LyricExportOptions
        {
            ExportFormat = ExportFormat.Lrc,
            ExportMode = ExportMode.Separated,
            LineBreak = "\n"
        });

        result.Should().Be(
            "[00:01.000]orig-1\n" +
            "[00:02.000]orig-2\n" +
            "[00:01.050]trans-1\n" +
            "[00:02.030]trans-2\n" +
            "[00:01.080]roma-1\n" +
            "[00:02.060]roma-2");
    }

    [Fact]
    public void Export_Separated_WithoutIncludeKinds_ShouldOutputOriginalOnly()
    {
        var document = CreateSampleDocument();

        var result = LyricExporter.Export(document, new LyricExportOptions
        {
            ExportFormat = ExportFormat.Lrc,
            ExportMode = ExportMode.Separated,
            LineBreak = "\n",
            IncludeKinds = ImmutableHashSet<LyricTrackKind>.Empty
        });

        result.Should().Be(
            "[00:01.000]orig-1\n" +
            "[00:02.000]orig-2");
    }

    [Fact]
    public void Export_ShouldNotEndWithTrailingLineBreak()
    {
        var document = CreateSampleDocument();

        var result = LyricExporter.Export(document, new LyricExportOptions
        {
            ExportFormat = ExportFormat.Lrc,
            ExportMode = ExportMode.Interleaved,
            LineBreak = "\n"
        });

        result.EndsWith("\n").Should().BeFalse();
    }

    [Fact]
    public void Export_ShouldFormatHourAndEndTimestamp()
    {
        ILyricLine line = new LyricLine
        {
            StartTime = new TimeSpan(1, 2, 3),
            EndTime = new TimeSpan(1, 2, 4),
            Text = "with-end"
        };
        var document = new LyricDocument
        {
            Lines = ImmutableArray.Create(line),
            TrackKind = LyricTrackKind.Original
        };

        var result = LyricExporter.Export(document, new LyricExportOptions
        {
            ExportFormat = ExportFormat.Lrc,
            ExportMode = ExportMode.Separated,
            LineBreak = "\n",
            IncludeKinds = ImmutableHashSet<LyricTrackKind>.Empty
        });

        result.Should().Be("[01:02:03.000]with-end[01:02:04.000]");
    }

    [Fact]
    public void Export_Interleaved_FromSongResources_ShouldMatchCombinedGoldenFile()
    {
        var origin = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-0-original.lrc"));
        var translation = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-1-translation.lrc"));
        var romanization = LyricsFacade.Parse(TestResourceLocator.ReadAllText("song-2-romanization.lrc"));

        var withTranslation = LyricsFacade.Merge(origin, translation, new MergeOptions
        {
            MergeType = MergeType.Translation,
            MaxTimeDeltaMs = 10,
            AllowUnmatched = true
        });
        var merged = LyricsFacade.Merge(withTranslation, romanization, new MergeOptions
        {
            MergeType = MergeType.Romanization,
            MaxTimeDeltaMs = 10,
            AllowUnmatched = true
        });

        var exported = LyricExporter.Export(merged, new LyricExportOptions
        {
            ExportFormat = ExportFormat.Lrc,
            ExportMode = ExportMode.Interleaved,
            LineBreak = "\r\n",
            IncludeKinds = ImmutableHashSet.Create(LyricTrackKind.Translation, LyricTrackKind.Romanization)
        });
        var expected = TestResourceLocator.ReadAllText("song-3-combined.lrc");

        var exportedWithoutMeta = StripHeaderIfPresent(exported, "\r\n");
        NormalizeLineEndings(exportedWithoutMeta).Should().Be(NormalizeLineEndings(expected));
    }

    private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n");

    private static string StripHeaderIfPresent(string text, string lineBreak)
    {
        var normalized = NormalizeLineEndings(text);
        var normalizedBreak = NormalizeLineEndings(lineBreak);

        if (!normalized.StartsWith("[ti:")) return text;

        var lines = normalized.Split('\n').ToList();
        var headerKeys = new HashSet<string> { "ti", "ar", "al", "au", "by", "offset", "ve" };
        var index = 0;
        while (index < lines.Count)
        {
            var line = lines[index];
            if (!line.StartsWith("[") || !line.Contains(':') || !line.EndsWith("]")) break;
            var key = line[1..line.IndexOf(':')];
            if (!headerKeys.Contains(key)) break;
            index++;
        }

        var stripped = string.Join("\n", lines.Skip(index));
        return normalizedBreak == "\n" ? stripped : stripped.Replace("\n", normalizedBreak);
    }

    private static LyricDocument CreateSampleDocument()
    {
        ILyricLine line1 = new LyricLine
        {
            StartTime = TimeSpan.FromSeconds(1),
            Text = "orig-1",
            Translation = new LyricLine { StartTime = TimeSpan.FromMilliseconds(1050), Text = "trans-1" },
            Romanization = new LyricLine { StartTime = TimeSpan.FromMilliseconds(1080), Text = "roma-1" }
        };
        ILyricLine line2 = new LyricLine
        {
            StartTime = TimeSpan.FromSeconds(2),
            Text = "orig-2",
            Translation = new LyricLine { StartTime = TimeSpan.FromMilliseconds(2030), Text = "trans-2" },
            Romanization = new LyricLine { StartTime = TimeSpan.FromMilliseconds(2060), Text = "roma-2" }
        };

        return new LyricDocument
        {
            Lines = ImmutableArray.Create(line1, line2),
            TrackKind = LyricTrackKind.Original
        };
    }
}
