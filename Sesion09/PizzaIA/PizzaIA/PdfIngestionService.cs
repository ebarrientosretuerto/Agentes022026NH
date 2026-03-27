namespace DemoAPP.PDFVector;

/// <summary>
/// Servicio de ingesta: orquesta el pipeline completo de PDF → chunks → embeddings → pgvector.
/// </summary>
public class PdfIngestionService
{
    private readonly EmbeddingService _embeddingService;
    private readonly VectorStore _vectorStore;

    public PdfIngestionService(EmbeddingService embeddingService, VectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    /// <summary>
    /// Procesa un archivo PDF: extrae texto, genera chunks, crea embeddings y los guarda en pgvector.
    /// </summary>
    /// <param name="pdfPath">Ruta al archivo PDF.</param>
    /// <param name="reindexar">Si es true, elimina chunks previos del mismo archivo antes de insertar.</param>
    public async Task<IngestionResult> ProcesarPdfAsync(string pdfPath, bool reindexar = true)
    {
        var nombreArchivo = Path.GetFileName(pdfPath);
        Console.WriteLine($"[Ingesta] Procesando: {nombreArchivo}");

        // 1. Extraer chunks del PDF
        var chunks = PdfChunker.ExtraerChunks(pdfPath);
        Console.WriteLine($"[Ingesta] {chunks.Count} chunks extraídos");

        if (chunks.Count == 0)
            return new IngestionResult { Archivo = nombreArchivo, ChunksProcesados = 0, Error = "No se extrajo texto del PDF" };

        // 2. Si reindexar, eliminar chunks previos
        if (reindexar)
        {
            await _vectorStore.EliminarPorOrigenAsync(nombreArchivo);
            Console.WriteLine($"[Ingesta] Chunks previos de '{nombreArchivo}' eliminados");
        }

        // 3. Generar embeddings en batches de 20
        var batchSize = 20;
        var totalInsertados = 0;

        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            var batch = chunks.Skip(i).Take(batchSize).ToList();
            var textos = batch.Select(c => c.Contenido).ToList();

            try
            {
                var embeddings = await _embeddingService.GenerarEmbeddingsBatchAsync(textos);

                var items = batch.Zip(embeddings, (chunk, emb) => (chunk, emb)).ToList();
                await _vectorStore.InsertarChunksBatchAsync(items);

                totalInsertados += batch.Count;
                Console.WriteLine($"[Ingesta] Batch {i / batchSize + 1}: {batch.Count} chunks insertados ({totalInsertados}/{chunks.Count})");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Ingesta] Error en batch {i / batchSize + 1}: {ex.Message}");
                Console.ResetColor();
            }

            // Pequeña pausa para no saturar el API de embeddings
            if (i + batchSize < chunks.Count)
                await Task.Delay(200);
        }

        Console.WriteLine($"[Ingesta] Completado: {totalInsertados} chunks indexados de '{nombreArchivo}'");

        return new IngestionResult
        {
            Archivo = nombreArchivo,
            ChunksProcesados = totalInsertados,
            TotalChunks = chunks.Count
        };
    }

    /// <summary>
    /// Procesa todos los PDFs en una carpeta.
    /// </summary>
    public async Task<List<IngestionResult>> ProcesarCarpetaAsync(string carpetaPath)
    {
        var resultados = new List<IngestionResult>();
        var archivos = Directory.GetFiles(carpetaPath, "*.pdf");

        Console.WriteLine($"[Ingesta] {archivos.Length} archivos PDF encontrados en '{carpetaPath}'");

        foreach (var archivo in archivos)
        {
            var resultado = await ProcesarPdfAsync(archivo);
            resultados.Add(resultado);
        }

        return resultados;
    }
}

public class IngestionResult
{
    public string Archivo { get; set; } = "";
    public int ChunksProcesados { get; set; }
    public int TotalChunks { get; set; }
    public string? Error { get; set; }
}
