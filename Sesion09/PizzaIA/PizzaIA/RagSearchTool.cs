using System.ComponentModel;
using System.Text;

namespace DemoAPP.PDFVector;

/// <summary>
/// Herramienta RAG para el agente: busca información relevante en los documentos PDF indexados.
/// El agente la invoca automáticamente cuando necesita contexto adicional.
/// </summary>
public class RagSearchTool
{
    private readonly EmbeddingService _embeddingService;
    private readonly VectorStore _vectorStore;

    public RagSearchTool(EmbeddingService embeddingService, VectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    /// <summary>
    /// Busca información relevante en los documentos PDF indexados de Mi Pizza.
    /// Usa búsqueda semántica por similitud de vectores para encontrar los fragmentos más relevantes.
    /// </summary>
    /// <param name="consulta">La pregunta o tema a buscar en los documentos.</param>
    [Description("Busca información relevante en los documentos PDF de Mi Pizza (menú, políticas, recetas, procedimientos, etc.) " +
                 "usando búsqueda semántica. Úsala cuando necesites contexto adicional que no está en la base de datos SQL.")]
    public async Task<string> BuscarEnDocumentos(string consulta)
    {
        try
        {
            // 1. Generar embedding de la consulta
            var queryEmbedding = await _embeddingService.GenerarEmbeddingAsync(consulta);

            // 2. Buscar los 5 chunks más similares
            var resultados = await _vectorStore.BuscarSimilaresAsync(queryEmbedding, topK: 5);

            if (resultados.Count == 0)
                return "No se encontró información relevante en los documentos.";

            // 3. Formatear resultados como contexto para el modelo
            var sb = new StringBuilder();
            sb.AppendLine($"Se encontraron {resultados.Count} fragmentos relevantes:\n");

            foreach (var r in resultados)
            {
                sb.AppendLine($"--- Fuente: {r.Origen}, Página {r.Pagina} (similitud: {r.Similitud:P0}) ---");
                sb.AppendLine(r.Contenido);
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[RagSearchTool] Error: {ex.Message}");
            Console.ResetColor();
            return "Error al buscar en los documentos. Intenta de nuevo.";
        }
    }
}
