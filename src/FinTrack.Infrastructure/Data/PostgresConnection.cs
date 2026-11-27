using Npgsql;

namespace FinTrack.Infrastructure.Data;

public static class PostgresConnection
{
    public static string FromUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("postgres" or "postgresql") ||
            string.IsNullOrEmpty(uri.Host) || uri.Port == 0 ||
            string.IsNullOrEmpty(uri.AbsolutePath.Trim('/')) || uri.Fragment.Length != 0)
            throw new InvalidOperationException("DATABASE_URL must be a PostgreSQL connection URL.");

        var separator = uri.UserInfo.IndexOf(':');
        if (separator <= 0 || separator == uri.UserInfo.Length - 1)
            throw new InvalidOperationException("DATABASE_URL must include PostgreSQL credentials.");

        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port < 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath[1..]),
            Username = Uri.UnescapeDataString(uri.UserInfo[..separator]),
            Password = Uri.UnescapeDataString(uri.UserInfo[(separator + 1)..]),
            SslMode = SslMode.VerifyFull
        };

        if (uri.Query.TrimStart('?').Split('&').Contains("channel_binding=require", StringComparer.OrdinalIgnoreCase))
            connection.ChannelBinding = ChannelBinding.Require;

        return connection.ConnectionString;
    }
}
