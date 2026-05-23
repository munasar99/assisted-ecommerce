namespace AssistedEcommerce.Api.Infrastructure;

/// <summary>Reads production secrets from env vars (Railway/Vercel). appsettings.Local.json is not deployed to Docker.</summary>
public static class ConfigEnvironment
{
    public static string? GetMongoConnectionString(IConfiguration config)
    {
        string?[] candidates =
        [
            Environment.GetEnvironmentVariable("MONGODB_URI"),
            Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("MongoDb__ConnectionString"),
            config["MongoDb:ConnectionString"],
            config["ConnectionStrings:MongoDb"]
        ];

        foreach (var raw in candidates)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var value = raw.Trim();
            if (IsLocalDefault(value))
                continue;
            return value;
        }

        return null;
    }

    public static string ResolveConnectionString(MongoDbSettings settings, IConfiguration config)
    {
        var fromConfig = GetMongoConnectionString(config);
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig;

        var conn = settings.ConnectionString?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(conn) && !IsLocalDefault(conn) &&
            !conn.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            return conn;

        return "";
    }

    public static bool HasMongoEnvVar() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MONGODB_URI"))
        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING"))
        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MongoDb__ConnectionString"));

    public static string DescribeMongoTarget(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "not-set";
        if (connectionString.Contains("mongodb.net", StringComparison.OrdinalIgnoreCase))
            return "atlas";
        if (connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            return "localhost-default";
        return "custom";
    }

    private static bool IsLocalDefault(string value) =>
        value.Equals("mongodb://localhost:27017", StringComparison.OrdinalIgnoreCase);
}
