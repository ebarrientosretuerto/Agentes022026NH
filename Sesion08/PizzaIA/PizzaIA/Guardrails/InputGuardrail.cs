using System.Text.RegularExpressions;

namespace PizzaIA.Guardrails;

public static partial class InputGuardrail
{
    // Longitud maxima permitida por mensaje de usuario
    private const int MaxInputLength = 2000;

    //Patrones de prompt injection comunes
    private static readonly string[] InjectionPatterns =
    [
        "ignora las instrucciones previas",
        "ignora todas las instrucciones",
        "ignora tus instrucciones", 
        "olvida tus instrucciones",
        "ignora",
        "instrucciones previas",
        "instrucciones anteriores",
        "prompt del sistema",
        "reiniciar",
        "descartar",
        "olvida",
        "DAN",
        "modo desarrollador",
        "sin restricciones",
        "sudo",
        "jailbreak",
        "evadir",
        "revelar",
        "texto oculto",
        "imprime el prompt",
        "base64",
        "traduce",
        "formato de salida",
        "---",
        "###",
        "fin del mensaje",
        "nueva instrucción"
    ];

    private static readonly string[] DangerousSqlPatterns =
    [
        "DROP TABLE",
        "DROP DATABASE",
        "DROP SCHEMA",
        "TRUNCATE",
        "DELETE FROM",
        "ALTER TABLE",
        "CREATE USER",
        "REVOKE",
        "GRANT",
        "pg_sleep",
        "information_schema",
        "pg_catalog",
        "pg_tables",
        "COPY ",
        "\\copy",
        "pg_read_file",
        "pg_ls_dir",
        "lo_import",
        "lo_export"
    ];

    public static (bool IsValid, String? ErrorMessage) Validate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (true, null);

        if (input.Length > MaxInputLength)
            return (false, $"El mensaje es demasiado largo. Maximo{MaxInputLength} caracteres.");

        //Detectar el prompt injection
        var lowerInput = input.ToLowerInvariant();
        foreach(var pattern in InjectionPatterns)
        {
            if (lowerInput.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return (false, "Lo siento, solo puedo responder preguntas relacionadas con Mi Pizza");
        }

        //Detectar SQL injection directa en el mensaje del usuario
        var upperInput = input.ToUpperInvariant();
        foreach(var pattern in DangerousSqlPatterns)
        {
            if(upperInput.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return (false, "Lo siento, solo puedo responder preguntas relacionadas con Mi Pizza");
        }

        //Detectar el exceso de caracteres especiales
        if (ExcessiveSpecialCharRegex().IsMatch(input))
            return (false, "El mensaje contiene caracteres no válidos");

        return (true, null);
    }

    [GeneratedRegex(@"[^\w\s\.,;:¿\?¡!áéíóúñÁÉÍÓÚÑüÜ\-\(\)""'@#\$%&/=\+\*]{10,}", RegexOptions.Compiled)]
    private static partial Regex ExcessiveSpecialCharRegex();

}
