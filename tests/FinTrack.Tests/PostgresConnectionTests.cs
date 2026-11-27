using FinTrack.Infrastructure.Data;
using Npgsql;

namespace FinTrack.Tests;

public class PostgresConnectionTests
{
    [Fact]
    public void ConvertsNeonUrlAndRequiresVerifiedTls()
    {
        var result = new NpgsqlConnectionStringBuilder(PostgresConnection.FromUrl(
            "postgresql://role%40example:pass%3B%40%3Aword@ep-test.neon.tech:5432/fintrack%2Dpilot?sslmode=require&channel_binding=require"));

        Assert.Equal("ep-test.neon.tech", result.Host);
        Assert.Equal("fintrack-pilot", result.Database);
        Assert.Equal("role@example", result.Username);
        Assert.Equal("pass;@:word", result.Password);
        Assert.Equal(SslMode.VerifyFull, result.SslMode);
        Assert.Equal(ChannelBinding.Require, result.ChannelBinding);
    }

    [Theory]
    [InlineData("postgresql://example.com")]
    [InlineData("https://role:password@example.com/db")]
    [InlineData("postgresql://role@example.com/db")]
    public void RejectsMalformedConnections(string url)
    {
        Assert.Throws<InvalidOperationException>(() => PostgresConnection.FromUrl(url));
    }
}
