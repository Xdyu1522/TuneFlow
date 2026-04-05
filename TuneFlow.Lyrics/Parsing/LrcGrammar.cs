using Parlot;
using Parlot.Fluent;
using TuneFlow.Lyrics.Models;
using static Parlot.Fluent.Parsers;

namespace TuneFlow.Lyrics.Parsing;

public static class LrcGrammar
{
    // 基础数字解析
    private static readonly Parser<int> Number = Terms.Integer().Then(v => (int)v);

    public static readonly Parser<TimeSpan> Timestamp =
        // 1. 解析由冒号分隔的时间部分 (如 01:02 或 00:01:02)
        Separated(Literals.Char(':'), Number)
            .And(
                // 2. 可选的毫秒部分 (如 .12 或 .123)
                Literals.Char('.').And(Terms.Pattern(char.IsDigit, 1, 3)).Optional()
            )
            .Then(p =>
            {
                var timeParts = p.Item1;    // List<int>
                var millisMatch = p.Item2;   // Tuple<char, TextSpan>?

                int hours = 0, minutes = 0, seconds = 0, milliseconds = 0;

                // 根据分割后的部分数量分配时/分/秒
                if (timeParts.Count == 3)
                {
                    hours = timeParts[0];
                    minutes = timeParts[1];
                    seconds = timeParts[2];
                }
                else if (timeParts.Count == 2)
                {
                    minutes = timeParts[0];
                    seconds = timeParts[1];
                }
                else
                {
                    seconds = timeParts[0];
                }

                // 处理毫秒逻辑：根据位数自动补齐（.1 -> 100ms, .12 -> 120ms, .123 -> 123ms）
                if (millisMatch.HasValue)
                {
                    var msStr = millisMatch.Value.Item2.ToString();
                    milliseconds = int.Parse(msStr.PadRight(3, '0'));
                }

                return new TimeSpan(0, hours, minutes, seconds, milliseconds);
            });

    public static readonly Parser<TimeSpan> TimeTag =
        Between(Literals.Char('['), Timestamp, Literals.Char(']'));
    
    public static readonly Parser<char> NewLine =
        OneOf(
            Literals.Char('\n'),
            Literals.Char('\r')
        );
    
    // 元信息标签：[key:value]
    public static readonly Parser<(string Key, string Value)> MetaTag =
        Between(
            Literals.Char('['),
            Terms.Pattern(c => c != ':' && c != ']')
                .AndSkip(Literals.Char(':'))
                .And(AnyCharBefore(Literals.Char(']'))),
            Literals.Char(']')
        ).Then(p => (
            p.Item1.ToString().Trim().ToLowerInvariant(),  // 👈 key 规范化
            p.Item2.ToString().Trim()                      // 👈 value 去空格
        ));
    
    public static readonly Parser<(TimeSpan[] Times, string Text)> LyricLine =
        OneOrMany(LrcGrammar.TimeTag)
            .And(AnyCharBefore(NewLine, canBeEmpty: true))
            .Then(p => (p.Item1.ToArray(), p.Item2.ToString()));
}