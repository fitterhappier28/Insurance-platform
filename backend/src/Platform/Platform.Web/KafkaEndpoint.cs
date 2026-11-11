namespace Platform.Web;

public static class KafkaEndpoint
{
    public static string Parse(string connectionString)
    {
        var value = connectionString;
        const string marker = "Endpoint=";
        var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            value = value[(index + marker.Length)..];
            var end = value.IndexOf(';', StringComparison.Ordinal);
            if (end >= 0)
            {
                value = value[..end];
            }
        }

        value = value.Replace("tcp://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        return value;
    }
}
