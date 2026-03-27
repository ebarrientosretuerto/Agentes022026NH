using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace PizzaIA.RAG;




public static class PdfChunker
{
    public static List<DocumentChunk> ExtraerChunks(string pdfPath, int maxChunkChars = 1500, int overlapChars = 200)
    {
        var chunks = new List<DocumentChunk>();
        var nombreArchivo = Path.GetFileName(pdfPath);
        using var document = PdfDocument.Open(pdfPath);

        foreach(var page in document.GetPages())
        {
            var texto = ContentOrderTextExtractor.GetText(page);
            if (string.IsNullOrWhiteSpace(texto))
                continue;

            texto = NormalizarTexto(texto);

            if(texto.Length >= maxChunkChars)
            {
                chunks.Add(new DocumentChunk
                {
                    Contenido = texto,
                    Origen = nombreArchivo,
                    Pagina = page.Number,
                    ChunkIndex = 0,
                    TokensAprox = EstimarTokens(texto)
                });
            }
            else
            {
                var pageChunks = DividirEnChunks(texto, maxChunkChars, overlapChars);
                for(int i = 0; i < pageChunks.Count; i++)
                {
                    chunks.Add(new DocumentChunk
                    {
                        Contenido = pageChunks[i],
                        Origen = nombreArchivo,
                        Pagina = page.Number,
                        ChunkIndex = i,
                        TokensAprox = EstimarTokens(pageChunks[i])
                    });
                }
            }
        }
        return chunks;
    }

    private static List<string> DividirEnChunks(string texto, int maxChars, int overlap)
    {
        var chunks = new List<string>();
        var inicio = 0;

        while(inicio < texto.Length)
        {
            var fin = Math.Min(inicio + maxChars, texto.Length);
            // Intentar cortar en un punto natural (punto, salto de línea)
            if (fin < texto.Length)
            {
                var corteNatural = texto.LastIndexOfAny(['.', '\n', '!', '?'], fin, Math.Min(fin - inicio, 200));
                if (corteNatural > inicio)
                    fin = corteNatural + 1;
            }

            chunks.Add(texto[inicio..fin].Trim());
            inicio = fin - overlap;

            if (inicio >= texto.Length) break;
        }

        return chunks;
    }

    private static string NormalizarTexto(string texto)
    {
        // Reemplazar múltiples espacios/tabs por uno solo
        var normalized = System.Text.RegularExpressions.Regex.Replace(texto, @"[ \t]+", " ");
        // Reemplazar múltiples saltos de línea por uno solo
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\n{3,}", "\n\n");
        return normalized.Trim();
    }

    /// <summary>
    /// Estimación rápida de tokens (~4 chars por token en español).
    /// </summary>
    private static int EstimarTokens(string texto) => texto.Length / 4;
}



public class DocumentChunk
{
    public string Contenido { get; set; } = "";
    public string Origen { get; set; } = "";
    public int Pagina { get; set; }
    public int ChunkIndex { get; set; }
    public int TokensAprox { get; set; }
        
}