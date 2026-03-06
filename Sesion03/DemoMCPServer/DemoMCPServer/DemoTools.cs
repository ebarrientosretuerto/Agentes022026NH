using Microsoft.Extensions.Logging;
using System.Collections;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
namespace MCPServerDemo.Tools;

public class DemoTools
{
    private readonly ILogger<DemoTools> _logger;
    public DemoTools(ILogger<DemoTools> logger)
    {
        _logger = logger;
    }

    [Description("Obtiene la fecha y hora actual del sistema")]
    public string GetCurrentDateTime()
    {
        var result = $"Fecha y hora actual: {DateTime.UtcNow:ddd, dd ' de ' MMM ' de ' yyyy, HH:mm:ss} UTC";
        _logger.LogInformation("GetCurrentDateTime called -> {Result}", result);
        return result;
    }
    
    [Description("Realiza una operación matemática básica entre dos números")]
    public double Calculate(
        [Description("Primer operando")] double a,
        [Description("Operación add, substract, multiply, divide")] string operation,
        [Description("Segundo operando")] double b)
    {
        _logger.LogInformation("Calculate called with a={A}, operation= { Op}, b ={ B}", a, operation, b);
        try
        {
            var result = operation switch
            {
                "add" => a + b,
                "substract" => a - b,
                "multiply" => a * b,
                "divide" => b == 0 ? throw new DivideByZeroException("No se puede dividir por cero") : a / b,_ => throw new ArgumentException($"Operación desconocida: { operation }")
            };
            _logger.LogInformation("Calculate result -> {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Calculate");
            throw;
        }
    }
}