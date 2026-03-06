using System.Text;
using System.Text.Json;
using MCPServerDemo.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.ComponentModel;
using System.Reflection;

namespace MCPServerDemo
{
    public class Function
    {
        private readonly DemoTools _tools;
        private readonly ILogger<Function> _logger;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public Function(DemoTools tools, ILogger<Function> logger)
        {
            _tools = tools;
            _logger = logger;
        }

        [Function("mcp")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mcp")]
            HttpRequestData req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("Incoming request body: {Body}", body);

            JsonDocument doc;
            try { doc = JsonDocument.Parse(body); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse request body");
                return await ErrorResponse(req, -32700, "Parse Error", null);
            }

            var root = doc.RootElement;
            var method = root.TryGetProperty("method", out var m) ? m.GetString() : null;

            object? id = null;
            if (root.TryGetProperty("id", out var i)) id = i;

            _logger.LogInformation("MCP method: {Method}, Id: {Id}", method, id);

            return method switch
            {
                "initialize" => await HandleInitialize(req, id),
                "tools/list" => await HandleToolsList(req, id),
                "tools/call" => await HandleToolsCall(req, root, id),
                _ => await ErrorResponse(req, -32601, $"Method not found: {method}", id),
            };
        }

        private async Task<HttpResponseData> HandleInitialize(HttpRequestData req, object? id)
        {
            var result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new { name = "MCPServerDemo", version = "1.0.0" }
            };

            return await SseResponse(req, id, result);
        }

        private async Task<HttpResponseData> HandleToolsList(HttpRequestData req, object? id)
        {
            var toolList = typeof(DemoTools)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method =>
                {
                    var desc = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? method.Name;

                    var parameters = method.GetParameters().Select(p => new
                    {
                        name = p.Name,
                        description = p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? p.Name,
                        type = MapType(p.ParameterType)
                    }).ToArray();

                    return new
                    {
                        name = method.Name,
                        description = desc,
                        inputSchema = new
                        {
                            type = "object",
                            properties = parameters.ToDictionary(
                                p => p.name,
                                p => new { type = p.type, description = p.description }
                            ),
                            required = parameters.Select(p => p.name).ToArray()
                        }
                    };
                })
                .ToArray();

            _logger.LogInformation("Returning tools list with {count} tools", toolList.Length);
            return await SseResponse(req, id, new { tools = toolList });
        }

        private async Task<HttpResponseData> HandleToolsCall(HttpRequestData req, JsonElement root, object? id)
        {
            if (!root.TryGetProperty("params", out var @params))
                return await ErrorResponse(req, -32602, "Invalid params", id);

            var toolName = @params.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            var hasArgs = @params.TryGetProperty("arguments", out var arguments);

            _logger.LogInformation("Tool call requested: {ToolName}, hasArgs={HasArgs}", toolName, hasArgs);
            if (hasArgs) _logger.LogInformation("Arguments JSON: {Args}", arguments.GetRawText());

            try
            {
                var method = typeof(DemoTools).GetMethod(toolName ?? "")
                    ?? throw new ArgumentException($"Tool not found: {toolName}");

                var parameters = method.GetParameters();

                var invokeArgs = parameters.Select(p =>
                {
                    if (!hasArgs) return Type.Missing;
                    if (!arguments.TryGetProperty(p.Name!, out var val)) return Type.Missing;
                    return JsonSerializer.Deserialize(val.GetRawText(), p.ParameterType, JsonOpts);
                }).ToArray();

                var result = method.Invoke(_tools, invokeArgs);
                _logger.LogInformation("Tool {Tool} invoked, result type: {Type}",
                    toolName, result?.GetType().Name ?? "null");

                var text = result?.ToString() ?? string.Empty;

                _logger.LogInformation("Returning result for {Tool}: {Text}", toolName, text);

                return await SseResponse(req, id, new
                {
                    content = new[] { new { type = "text", text } }
                });
            }
            catch (TargetInvocationException tie)
            {
                return await ErrorResponse(req, -32000, tie.InnerException?.Message ?? tie.Message, id);
            }
            catch (Exception ex)
            {
                return await ErrorResponse(req, -32000, ex.Message, id);
            }
        }

        private static async Task<HttpResponseData> ErrorResponse(HttpRequestData req, int code, string message, object? id)
        {
            var payload = JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code, message } }, JsonOpts);
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(payload, Encoding.UTF8);
            return response;
        }

        private static async Task<HttpResponseData> SseResponse(HttpRequestData req, object? id, object result)
        {
            var payload = JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result }, JsonOpts);
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "text/event-stream");
            response.Headers.Add("Cache-Control", "no-cache");
            await response.WriteStringAsync($"event: message\ndata: {payload}\n\n", Encoding.UTF8);
            return response;
        }

        private static string MapType(Type t) =>
            t == typeof(double) || t == typeof(int) || t == typeof(float) ? "number" :
            t == typeof(bool) ? "boolean" : "string";
    }
}