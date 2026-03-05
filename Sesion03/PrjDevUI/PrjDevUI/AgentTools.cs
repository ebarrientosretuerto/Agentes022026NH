using System.ComponentModel;

namespace DemoAPP;

public class AgentTools
{
    [Description("Obtiene la fecha y hora actual del sistema.")]
    public string GetCurrentDateTime() => $"Fecha y hora actual: " +
        $"{DateTime.Now:ddd, dd ' de ' MMMM ' de ' yyyy, HH:mm:ss}";
}
