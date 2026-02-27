using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DemoAPP;

[McpServerToolType]
public class McpTools
{
    [McpServerTool, Description("Obtiene la fecha y hora actual del sistema.")]
    public static string GetCurrentDateTime() =>
        $"Fecha y hora actual: {DateTime.Now:dddd, 'de' MMMM 'de' yyyy, HH:mm:ss}";
}