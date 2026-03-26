using System.Text.RegularExpressions;

﻿namespace PizzaIA.Guardrails

/// <summary>
/// Guardrail SQL: validación robusta de consultas SQL antes de ejecutarlas.
/// Complementa la validación básica de PizzaDbQueryTool con análisis más profundo.
/// </summary>
public static partial class SqlGuardrail
{
    // Schemas permitidos
    private static readonly string[] AllowedSchemas = ["pizza."];

    // Comandos SQL permitidos
    private static readonly string[] AllowedCommands = ["SELECT", "WITH"];

    // Palabras clave peligrosas (case-insensitive)
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE",
        "CREATE", "EXEC", "EXECUTE", "GRANT", "REVOKE",
        "COPY", "\\COPY",
        "pg_sleep", "pg_read_file", "pg_ls_dir", "pg_stat",
        "lo_import", "lo_export", "lo_create", "lo_unlink",
        "dblink", "pg_execute_server_program",
        "SET ", "RESET ", "LOAD ",
    ];

    // Funciones peligrosas de PostgreSQL
    private static readonly string[] ForbiddenFunctions =
    [
        "pg_sleep",
        "pg_read_file",
        "pg_read_binary_file",
        "pg_ls_dir",
        "pg_stat_file",
        "pg_terminate_backend",
        "pg_cancel_backend",
        "pg_reload_conf",
        "current_setting",
        "set_config",
        "lo_import",
        "lo_export",
    ];

    /// <summary>
    /// Valida una consulta SQL. Retorna (esValida, mensajeError).
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) Validate(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return (false, "La consulta SQL está vacía.");

        var normalized = NormalizeQuery(sql);

        // 1. Verificar que empiece con un comando permitido
        var startsWithAllowed = false;
        foreach (var cmd in AllowedCommands)
        {
            if (normalized.StartsWith(cmd, StringComparison.OrdinalIgnoreCase))
            {
                startsWithAllowed = true;
                break;
            }
        }
        if (!startsWithAllowed)
            return (false, "Solo se permiten consultas SELECT.");

        // 2. Detectar múltiples statements (SQL injection con ;)
        if (ContainsMultipleStatements(sql))
            return (false, "No se permiten múltiples sentencias SQL.");

        // 3. Buscar palabras clave prohibidas
        foreach (var keyword in ForbiddenKeywords)
        {
            if (ContainsKeyword(normalized, keyword))
                return (false, $"Operación no permitida: {keyword.Trim()}");
        }

        // 4. Buscar funciones peligrosas
        foreach (var func in ForbiddenFunctions)
        {
            if (normalized.Contains(func, StringComparison.OrdinalIgnoreCase))
                return (false, $"Función no permitida: {func}");
        }

        // 5. Detectar acceso a schemas del sistema
        if (SystemSchemaRegex().IsMatch(normalized))
            return (false, "No se permite acceder a schemas del sistema.");

        // 6. Detectar comentarios SQL (pueden ocultar inyecciones)
        if (normalized.Contains("--") || normalized.Contains("/*"))
            return (false, "No se permiten comentarios SQL.");

        // 7. Detectar UNION-based injection
        if (UnionInjectionRegex().IsMatch(normalized))
        {
            // Permitir UNION solo si no accede a schemas del sistema
            if (SystemSchemaInUnionRegex().IsMatch(normalized))
                return (false, "UNION con acceso a schemas del sistema no está permitido.");
        }

        return (true, null);
    }

    /// <summary>
    /// Normaliza la consulta removiendo espacios extra y saltos de línea.
    /// </summary>
    private static string NormalizeQuery(string sql)
    {
        return WhitespaceRegex().Replace(sql.Trim(), " ");
    }

    /// <summary>
    /// Detecta si hay múltiples statements separados por punto y coma.
    /// Ignora punto y coma dentro de strings.
    /// </summary>
    private static bool ContainsMultipleStatements(string sql)
    {
        var inString = false;
        var quoteChar = ' ';

        foreach (var c in sql)
        {
            if (!inString && (c == '\'' || c == '"'))
            {
                inString = true;
                quoteChar = c;
            }
            else if (inString && c == quoteChar)
            {
                inString = false;
            }
            else if (!inString && c == ';')
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Busca una palabra clave como token completo (no como substring de otra palabra).
    /// </summary>
    private static bool ContainsKeyword(string text, string keyword)
    {
        var trimmedKeyword = keyword.Trim();
        var pattern = $@"\b{Regex.Escape(trimmedKeyword)}\b";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\b(information_schema|pg_catalog|pg_tables|pg_views|pg_roles|pg_user|pg_shadow|pg_authid)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SystemSchemaRegex();

    [GeneratedRegex(@"\bUNION\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UnionInjectionRegex();

    [GeneratedRegex(@"\bUNION\b.*\b(information_schema|pg_catalog|pg_tables)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SystemSchemaInUnionRegex();
}

