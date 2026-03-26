using System.ComponentModel;
using System.Text.Json;
using Npgsql;
using PizzaIA.Guardrails;

namespace PizzaIA;

public class PizzaDbQueryTool
{
    private readonly string _connectionString;

    public PizzaDbQueryTool(IConfiguration configuration)
    {
        var host = configuration["Postgres:Host"] ?? throw new InvalidOperationException("Falta Postgres:Host");
        var port = configuration["Postgres:Port"] ?? "5432";
        var db = configuration["Postgres:Database"] ?? throw new InvalidOperationException("Falta Postgres:Database");
        var user = configuration["Postgres:Username"] ?? throw new InvalidOperationException("Falta Postgres:Username");
        var pass = configuration["Postgres:Password"] ?? throw new InvalidOperationException("Falta Postgres:Password");
        var ssl = configuration["Postgres:SslMode"] ?? "Require";

        _connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode={ssl};Trust Server Certificate=true";
    }

    /// <summary>
    /// Ejecuta una consulta SQL SELECT contra la base de datos de Mi Pizza y devuelve los resultados en 
    /// formato JSON
    /// </summary>
    /// <param name=="sql">La consuilta SQL SELEC a ejecutar. Debe usar el prefijo schema pizza</param>
    /// 

    [Description("Ejecuta una consulta SQL SELECT de solo lectura contra l a base daots PostgreSQL de Mi Pizza" +
        "Solo se perminten SELECT. USa siempre el prefijo schema pizza en las tablas.")]
    public async  Task<string> EjecutarConsulta(string sql)
    {
        var (isValid, errorMessage) = SqlGuardrail.Validate(sql);
        if (!isValid)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Sqlguardrail] Consulta bloqueada: {errorMessage}");
            Console.WriteLine($"[Sqlguardrail] SQL: {sql[..Math.Min(200, sql.Length)]}");
            Console.ResetColor();
            return $"Error: {errorMessage}";
        }

        var trimmed = sql.TrimStart().ToUpperInvariant();

        if (!trimmed.StartsWith("SELECT"))
            return "Error: Solo se permiten consultas SELECT de solo lectura";
        string[] forbidden = ["INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE",
                            "CREATE", "EXEC", "EXECUTE"];

        foreach(var word in forbidden)
        {
            if (trimmed.Contains(word))
                return $"Error: La consutla contiene una operación no permitida {sql}. solo SELECT";
        }

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 15;

            await using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i= 0; i< reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                results.Add(row);

                if (results.Count >= 100)
                    break;
            }

            if (results.Count == 0)
                return "La consulta no devolvió resultados";

            var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return $"Resultado ({results.Count} filas:\n{json}";
        }
        catch(Exception ex)
        {
            return $"Error al ejecutar la consulta: {ex.Message}";
        }
    }

}

