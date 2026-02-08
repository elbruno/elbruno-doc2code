// elbruno.Doc2Code — utility for repairing common JSON malformations produced by LLMs.
namespace elbruno.Doc2Code.Agents;

/// <summary>
/// Attempts to fix common structural JSON errors produced by LLMs,
/// such as missing commas, trailing commas, duplicate commas, and truncated output.
/// All repairs are string-aware — content inside JSON string values is never modified.
/// </summary>
internal static class JsonRepairHelper
{
    /// <summary>
    /// Attempts to repair malformed JSON. Returns the repaired string and a list of
    /// human-readable descriptions of fixes that were applied.
    /// </summary>
    public static (string RepairedJson, List<string> AppliedFixes) TryRepair(string json)
    {
        var fixes = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
            return (json, fixes);

        // Phase 1: character-level repairs (string-aware)
        var repaired = RepairStructural(json, fixes);

        // Phase 2: close unclosed braces/brackets (truncation repair)
        repaired = CloseTruncated(repaired, fixes);

        return (repaired, fixes);
    }

    /// <summary>
    /// Walks the JSON character-by-character, tracking whether we are inside a string,
    /// and applies structural repairs only outside of strings.
    /// </summary>
    private static string RepairStructural(string json, List<string> fixes)
    {
        var sb = new System.Text.StringBuilder(json.Length + 64);
        var inString = false;
        var escape = false;

        int missingCommaCount = 0;
        int trailingCommaCount = 0;
        int doubleCommaCount = 0;

        for (int i = 0; i < json.Length; i++)
        {
            var ch = json[i];

            // --- String tracking ---
            if (inString)
            {
                sb.Append(ch);
                if (escape)
                {
                    escape = false;
                    continue;
                }
                if (ch == '\\')
                {
                    escape = true;
                    continue;
                }
                if (ch == '"')
                    inString = false;
                continue;
            }

            // Outside of string
            if (ch == '"')
            {
                // Check if we need a missing comma before this quote
                if (sb.Length > 0)
                {
                    var prev = LastNonWhitespace(sb);
                    if (NeedsCommaAfter(sb, prev))
                    {
                        InsertCommaAfterLastValue(sb);
                        missingCommaCount++;
                    }
                }
                inString = true;
                sb.Append(ch);
                continue;
            }

            // --- Missing comma: } { or ] [ ---
            if ((ch == '{' || ch == '[') && sb.Length > 0)
            {
                var prev = LastNonWhitespace(sb);
                if (NeedsCommaAfter(sb, prev))
                {
                    InsertCommaAfterLastValue(sb);
                    missingCommaCount++;
                }
            }

            // --- Trailing comma before } or ] ---
            if (ch == '}' || ch == ']')
            {
                RemoveTrailingComma(sb, ref trailingCommaCount);
                sb.Append(ch);
                continue;
            }

            // --- Double (or more) commas ---
            if (ch == ',')
            {
                // Skip if previous non-whitespace is already a comma or opening bracket
                var prev = LastNonWhitespace(sb);
                if (prev == ',' || prev == '[' || prev == '{')
                {
                    doubleCommaCount++;
                    continue; // skip this comma
                }
                sb.Append(ch);
                continue;
            }

            sb.Append(ch);
        }

        if (missingCommaCount > 0)
            fixes.Add($"Inserted {missingCommaCount} missing comma(s) between values");
        if (trailingCommaCount > 0)
            fixes.Add($"Removed {trailingCommaCount} trailing comma(s)");
        if (doubleCommaCount > 0)
            fixes.Add($"Removed {doubleCommaCount} duplicate comma(s)");

        return sb.ToString();
    }

    /// <summary>
    /// Returns true if the previous non-whitespace character indicates the end of a JSON value
    /// that should be followed by a comma before a new value starts.
    /// </summary>
    private static bool NeedsCommaAfter(System.Text.StringBuilder sb, char prev)
    {
        return prev == '}' || prev == ']' || prev == '"' ||
               char.IsDigit(prev) ||
               EndsWithLiteral(sb, "true") ||
               EndsWithLiteral(sb, "false") ||
               EndsWithLiteral(sb, "null");
    }

    /// <summary>
    /// Inserts a comma right after the last non-whitespace character in the StringBuilder,
    /// preserving any trailing whitespace after the comma.
    /// </summary>
    private static void InsertCommaAfterLastValue(System.Text.StringBuilder sb)
    {
        // Find the position right after the last non-whitespace character
        int insertPos = sb.Length;
        for (int i = sb.Length - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(sb[i]))
            {
                insertPos = i + 1;
                break;
            }
        }
        sb.Insert(insertPos, ',');
    }

    /// <summary>
    /// Detects unclosed braces/brackets and appends closing characters.
    /// Tracks nesting outside of string literals.
    /// </summary>
    private static string CloseTruncated(string json, List<string> fixes)
    {
        var stack = new Stack<char>();
        var inString = false;
        var escape = false;

        foreach (var ch in json)
        {
            if (inString)
            {
                if (escape) { escape = false; continue; }
                if (ch == '\\') { escape = true; continue; }
                if (ch == '"') inString = false;
                continue;
            }

            switch (ch)
            {
                case '"': inString = true; break;
                case '{': stack.Push('}'); break;
                case '[': stack.Push(']'); break;
                case '}' or ']':
                    if (stack.Count > 0 && stack.Peek() == ch) stack.Pop();
                    break;
            }
        }

        if (stack.Count == 0)
            return json;

        // Remove any trailing comma before we close
        var trimmed = json.TrimEnd();
        if (trimmed.Length > 0 && trimmed[^1] == ',')
            trimmed = trimmed[..^1];

        var closers = new string(stack.ToArray());
        fixes.Add($"Appended {stack.Count} closing character(s) for truncated JSON: {closers}");
        return trimmed + closers;
    }

    private static char LastNonWhitespace(System.Text.StringBuilder sb)
    {
        for (int i = sb.Length - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(sb[i]))
                return sb[i];
        }
        return '\0';
    }

    private static bool EndsWithLiteral(System.Text.StringBuilder sb, string literal)
    {
        if (sb.Length < literal.Length)
            return false;

        int start = sb.Length - literal.Length;
        // The character before the literal (if any) must not be a letter (word boundary)
        if (start > 0 && char.IsLetter(sb[start - 1]))
            return false;

        for (int i = 0; i < literal.Length; i++)
        {
            if (sb[start + i] != literal[i])
                return false;
        }
        return true;
    }

    private static void RemoveTrailingComma(System.Text.StringBuilder sb, ref int count)
    {
        for (int i = sb.Length - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(sb[i]))
                continue;
            if (sb[i] == ',')
            {
                sb.Remove(i, 1);
                count++;
            }
            return;
        }
    }
}
