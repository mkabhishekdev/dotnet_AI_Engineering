using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

public static class GeminiStreamReader
{
    public static async IAsyncEnumerable<JsonElement> ReadAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        var eventData = new StringBuilder();

        while (true)
        {
            string? line =
                await reader.ReadLineAsync(cancellationToken);

            // A blank line marks the end of an SSE message.
            // null means the connection has ended.
            if (line is null || line.Length == 0)
            {
                if (eventData.Length > 0)
                {
                    using var document =
                        JsonDocument.Parse(eventData.ToString());

                    yield return document.RootElement.Clone();

                    eventData.Clear();
                }

                if (line is null)
                {
                    yield break;
                }

                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                string data = line["data:".Length..];

                if (data.StartsWith(" ", StringComparison.Ordinal))
                {
                    data = data[1..];
                }

                eventData.AppendLine(data);
            }
        }
    }
}