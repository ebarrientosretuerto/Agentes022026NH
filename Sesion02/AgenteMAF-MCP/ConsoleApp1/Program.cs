using DemoAPP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;

//Si se pasa con --with-mcp se levanta el servidor MCP (stdio) en lugas del chat
if (args.Contains("--with-mcp"))
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();
    await builder.Build().RunAsync();
    return;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

IChatService chatService = new OpenAICliente();

try
{
    chatService.Initialize(configuration);
    Console.WriteLine($"Hola soy tu agente Azure Open AI");

}
catch(Exception ex)
{
    Console.WriteLine($"Error fatal durante la inicializacion: {ex.Message}");
    return;
}

Console.WriteLine("Chat iniciado (escriba 'exit' para salir):");

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Usuario: ");
    var input = Console.ReadLine();
    Console.ResetColor();

    
    if(string.IsNullOrWhiteSpace(input) || 
        input.Equals("exit",StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    var response = await chatService.SendMessageAsync(input);

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"Agente: {response}");
    Console.ResetColor();
}