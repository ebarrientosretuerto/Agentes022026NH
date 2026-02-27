using Microsoft.Extensions.Configuration;

namespace DemoAPP;

public interface IChatService
{
    void Initialize(IConfiguration configuration);

    Task<string> SendMessageAsync(string message);
}