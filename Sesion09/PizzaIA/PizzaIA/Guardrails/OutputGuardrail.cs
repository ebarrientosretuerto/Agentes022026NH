namespace PizzaIA.Guardrails;

/// <summary>
/// Guardrail de salida: valida las respuestas del modelo antes de enviarlas al usuario.
/// Detecta fugas de información sensible y contenido inapropiado.
/// </summary>
public static class OutputGuardrail
{
    // Patrones que indican fuga de información sensible
    private static readonly string[] SensitivePatterns =
    [
        "api_key",
        "apikey",
        "password",
        "secret",
        "connection_string",
        "connectionstring",
        "Bearer ",
        "sk-",           // OpenAI API keys
        "AKIA",          // AWS access keys
        "-----BEGIN",    // Certificados/llaves privadas
    ];

    // Patrones que indican que el modelo reveló su system prompt
    private static readonly string[] SystemPromptLeakPatterns =
    [
        "mis instrucciones son",
        "mi prompt es",
        "mi system prompt",
        "fui instruido para",
        "mis reglas",
        "my instructions are",
        "my system prompt",
    ];

    /// <summary>
    /// Sanitiza la respuesta del modelo. Retorna el texto limpio o un mensaje seguro.
    /// </summary>
    public static string Sanitize(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return string.Empty;

        // Detectar fuga de system prompt
        var lowerOutput = output.ToLowerInvariant();
        foreach (var pattern in SystemPromptLeakPatterns)
        {
            if (lowerOutput.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[OutputGuardrail] Detectada posible fuga de system prompt. Respuesta bloqueada.");
                Console.ResetColor();
                return "Lo siento, solo puedo responder preguntas relacionadas con Mi Pizza 🍕";
            }
        }

        // Detectar fuga de credenciales/secretos
        foreach (var pattern in SensitivePatterns)
        {
            if (output.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[OutputGuardrail] Detectada posible fuga de información sensible ({pattern}). Respuesta sanitizada.");
                Console.ResetColor();
                return RedactSensitiveInfo(output, pattern);
            }
        }

        return output;
    }

    /// <summary>
    /// Redacta información sensible reemplazándola con [REDACTED].
    /// </summary>
    private static string RedactSensitiveInfo(string text, string pattern)
    {
        // Reemplazar el patrón y lo que le sigue (hasta el siguiente espacio o fin de línea)
        var idx = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        while (idx >= 0)
        {
            var endIdx = text.IndexOfAny([' ', '\n', '\r', ',', ';', '"', '\''], idx + pattern.Length);
            if (endIdx < 0) endIdx = text.Length;

            text = string.Concat(text.AsSpan(0, idx), "[REDACTED]", text.AsSpan(endIdx));
            idx = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        }
        return text;
    }
}
