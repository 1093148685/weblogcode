using System.Text;

namespace Weblog.Core.Service.AI.Rag;

public static class RagTextProcessor
{
    public static List<string> SplitText(string text, int chunkSize, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var normalizedChunkSize = Math.Max(chunkSize, 100);
        var normalizedOverlap = Math.Clamp(overlap, 0, normalizedChunkSize - 1);
        var step = Math.Max(normalizedChunkSize - normalizedOverlap, 1);

        var paragraphs = text
            .Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(paragraph => paragraph.Trim())
            .Where(paragraph => paragraph.Length > 0)
            .ToList();

        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > normalizedChunkSize * 2)
            {
                FlushCurrent(chunks, current);
                SplitLongParagraph(chunks, paragraph, normalizedChunkSize, step);
                continue;
            }

            if (current.Length + paragraph.Length > normalizedChunkSize && current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                var overlapText = current.Length > normalizedOverlap
                    ? current.ToString()[^normalizedOverlap..]
                    : current.ToString();
                current.Clear();
                current.AppendLine(overlapText);
            }

            current.AppendLine(paragraph);
            current.AppendLine();
        }

        FlushCurrent(chunks, current);
        return chunks.Where(chunk => chunk.Length >= 10).ToList();
    }

    public static int EstimateTokens(string text)
    {
        return (int)(text.Length * 0.6);
    }

    public static List<string> ExtractKeywords(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<string>();

        var keywords = query
            .Split(new[] { ' ', ',', '，', '、', ';', '；', '?', '？', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(keyword => keyword.Trim())
            .Where(keyword => keyword.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (keywords.Count == 0)
            keywords.Add(query.Trim());

        return keywords;
    }

    public static float KeywordScore(string content, List<string> keywords)
    {
        if (keywords.Count == 0 || string.IsNullOrWhiteSpace(content))
            return 0f;

        return keywords.Sum(keyword =>
            content.Contains(keyword, StringComparison.OrdinalIgnoreCase) ? 1f : 0f) / keywords.Count;
    }

    public static float CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length != right.Length)
            return 0f;

        var dot = 0f;
        var leftNorm = 0f;
        var rightNorm = 0f;

        for (var i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            leftNorm += left[i] * left[i];
            rightNorm += right[i] * right[i];
        }

        var denominator = MathF.Sqrt(leftNorm) * MathF.Sqrt(rightNorm);
        return denominator < 1e-10f ? 0f : dot / denominator;
    }

    private static void SplitLongParagraph(List<string> chunks, string paragraph, int chunkSize, int step)
    {
        for (var index = 0; index < paragraph.Length; index += step)
        {
            var end = Math.Min(index + chunkSize, paragraph.Length);
            chunks.Add(paragraph[index..end].Trim());

            if (end == paragraph.Length)
                break;
        }
    }

    private static void FlushCurrent(List<string> chunks, StringBuilder current)
    {
        if (current.Length == 0)
            return;

        chunks.Add(current.ToString().Trim());
        current.Clear();
    }
}
