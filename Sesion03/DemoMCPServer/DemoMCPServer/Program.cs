using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MCPServerDemo.Tools;

var host = new HostBuilder()
.ConfigureFunctionsWorkerDefaults()
.ConfigureServices(services =>
{
    services.AddSingleton<DemoTools>();
}).
Build();
host.Run();