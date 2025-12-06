using Npgsql;
using System.Diagnostics;

namespace StorhaugenEats.API;

public static class ConnectionTester
{
    public static async Task TestAllConnectionsAsync(IConfiguration configuration)
    {
        var connectionStrings = new Dictionary<string, string>
        {
            { "Transaction Pooler (Port 6543)", configuration.GetConnectionString("DefaultConnection")! },
            { "Session Pooler (Port 5432)", configuration.GetConnectionString("SessionPooler")! },
            { "Direct Connection", configuration.GetConnectionString("DirectConnection")! }
        };

        Console.WriteLine("=== Testing Database Connections ===\n");

        foreach (var (name, connString) in connectionStrings)
        {
            Console.WriteLine($"Testing: {name}");
            Console.WriteLine($"Connection string: {MaskPassword(connString)}");

            var sw = Stopwatch.StartNew();

            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                // Test a simple query
                await using var cmd = new NpgsqlCommand("SELECT version()", conn);
                var version = await cmd.ExecuteScalarAsync();

                sw.Stop();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ SUCCESS ({sw.ElapsedMilliseconds}ms)");
                Console.WriteLine($"  PostgreSQL Version: {version?.ToString()?.Substring(0, Math.Min(50, version?.ToString()?.Length ?? 0))}...");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                sw.Stop();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ FAILED ({sw.ElapsedMilliseconds}ms)");
                Console.WriteLine($"  Error: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  Inner: {ex.InnerException.Message}");
                }

                Console.ResetColor();
            }

            Console.WriteLine();
        }

        Console.WriteLine("=== Connection Test Complete ===\n");
    }

    private static string MaskPassword(string connString)
    {
        if (string.IsNullOrEmpty(connString)) return connString;

        var builder = new NpgsqlConnectionStringBuilder(connString);
        builder.Password = "***";
        return builder.ToString();
    }
}
