using System.Text.Json;
using System;
using System.Text;
using Azure.Identity;

string endpoint = "URL";
string apiKey = "API KEY";
string deploymentName = "gpt-5-nano";

var client = new HttpClient();
client.DefaultRequestHeaders.Add("api-key", apiKey);

var requestBody = JsonSerializer.Serialize(new
{
    messages = new[] { new { 
        role = "user", 
        content="¿Cual es la capital de Japon?" 
    } },
    max_completion_tokens = 1000
});

var content = new StringContent(
    requestBody, 
    Encoding.UTF8, 
    "application/json"
);

var url = $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version=2024-02-15-preview";

var response = await client.PostAsync(url, content);
var responseContent = await response.Content.ReadAsStringAsync();

using JsonDocument doc = JsonDocument.Parse(responseContent);
Console.WriteLine("Respuesta del modelo:" + doc);
var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
Console.WriteLine(message);