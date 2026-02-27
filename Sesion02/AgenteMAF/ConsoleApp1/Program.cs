using DemoAPP;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

IChatService chatService = new OpenAICliente();

try
{
    chatService.Initialize(configuration);
    Console.WriteLine($"Hola soy tu agente Azure Open AI");
}catch(Exception ex)
{
    Console.WriteLine($"Error fatal dureante la inicializacion: {ex.Message}");
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