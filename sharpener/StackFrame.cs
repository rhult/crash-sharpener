using System.Text.RegularExpressions;

namespace Sharpener;

public partial struct StackFrame
{
    public string Symbol { get; private init; }
    public int Offset { get; private init; }
    public int TokenType { get; private init; }
    public int TokenValue { get; private init; }
    public string PartialLine { get; private init; }

    public static StackFrame? FromString(string line)
    {
        string symbol;
        int offset;
        int tokenType;
        int tokenValue;
        string partialLine;

        // Parses lines from a stack trace that has been decorated with IL offsets and
        // method tokens.
        //
        // For example:
        // at Car.Start(Action`1 onStop) IL_0123 T_06001234
        //
        // The token consists of one byte for the type (only methods, 0x06, is
        // supported for now) and three bytes for the token value.

        var match = LineRegex().Match(line);

        if (match is { Success: true, Groups.Count: 5 })
        {
            symbol = match.Groups[2].Value;
            offset = Convert.ToInt32(match.Groups[3].Value, 16);

            var tokenString = match.Groups[4].Value;
            tokenType = Convert.ToInt32(tokenString[..2], 16);
            tokenValue = Convert.ToInt32(tokenString[2..], 16);

            partialLine = "    " + match.Groups[1].Value;
        }
        else
        {
            return null;
        }

        return new StackFrame
        {
            Symbol = symbol,
            Offset = offset,
            TokenType = tokenType,
            TokenValue = tokenValue,
            PartialLine = partialLine
        };
    }

    [GeneratedRegex("^\\s*(at (.*)) IL_([A-Fa-f0-9]*) T_([A-Fa-f0-9]*)")]
    private static partial Regex LineRegex();
}
