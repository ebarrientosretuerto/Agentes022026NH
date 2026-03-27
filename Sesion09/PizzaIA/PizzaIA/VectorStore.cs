using Npgsql;
using Pgvector;
using Pgvector.Npgsql;

namespace PizzaIA.RAG;

/// <summary>
/// Almacén de vectores en PostgreSQL con pgvector.
/// Maneja la inserción de chunks con embeddings y la búsqueda por similitud.
/// </summary>
public class VectorStore
{
    private readonly string _connectionString;

    public VectorStore(IConfiguration configuration)
    {
        var host = configuration["Postgres:Host"] ?? throw new InvalidOperationException("Falta Postgres:Host");
        var port = configuration["Postgres:Port"] ?? "5432";
        var db = configuration["Postgres:Database"] ?? throw new InvalidOperationException("Falta Postgres:Database");
        var user = configuration["Postgres:Username"] ?? throw new InvalidOperationException("Falta Postgres:Username");
        var pass = configuration["Postgres:Password"] ?? throw new InvalidOperationException("Falta Postgres:Password");
        var ssl = configuration["Postgres:SslMode"] ?? "Require";

        _connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode={ssl};Trust Server Certificate=true";
    }

    /// <summary>
    /// Inserta un chunk con su embedding en la base de datos.
    /// </summary>
    public async Task InsertarChunkAsync(DocumentChunk chunk, float[] embedding)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
        dataSourceBuilder.UseVector();
        await using var dataSource = dataSourceBuilder.Build();
        await using var conn = await dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand("""
            INSERT INTO pizza.documentos_rag (contenido, origen, pagina, chunk_index, tokens_aprox, embedding)
            VALUES (@contenido, @origen, @pagina, @chunk_index, @tokens, @embedding)
            """, conn);

        cmd.Parameters.AddWithValue("contenido", chunk.Contenido);
        cmd.Parameters.AddWithValue("origen", chunk.Origen);
        cmd.Parameters.AddWithValue("pagina", chunk.Pagina);
        cmd.Parameters.AddWithValue("chunk_index", chunk.ChunkIndex);
        cmd.Parameters.AddWithValue("tokens", chunk.TokensAprox);
        cmd.Parameters.AddWithValue("embedding", new Vector(embedding));

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Inserta múltiples chunks en batch.
    /// </summary>
    public async Task InsertarChunksBatchAsync(List<(DocumentChunk Chunk, float[] Embedding)> items)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
        dataSourceBuilder.UseVector();
        await using var dataSource = dataSourceBuilder.Build();
        await using var conn = await dataSource.OpenConnectionAsync();

        foreach (var (chunk, embedding) in items)
        {
            await using var cmd = new NpgsqlCommand("""
                INSERT INTO pizza.documentos_rag (contenido, origen, pagina, chunk_index, tokens_aprox, embedding)
                VALUES (@contenido, @origen, @pagina, @chunk_index, @tokens, @embedding)
                """, conn);

            cmd.Parameters.AddWithValue("contenido", chunk.Contenido);
            cmd.Parameters.AddWithValue("origen", chunk.Origen);
            cmd.Parameters.AddWithValue("pagina", chunk.Pagina);
            cmd.Parameters.AddWithValue("chunk_index", chunk.ChunkIndex);
            cmd.Parameters.AddWithValue("tokens", chunk.TokensAprox);
            cmd.Parameters.AddWithValue("embedding", new Vector(embedding));

            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Busca los chunks más similares a un embedding dado usando distancia coseno.
    /// </summary>
    /// <param name="queryEmbedding">Embedding de la pregunta del usuario.</param>
    /// <param name="topK">Cantidad de resultados a retornar.</param>
    /// <param name="umbralSimilitud">Umbral mínimo de similitud (0.0 a 1.0). Default: 0.3</param>
    public async Task<List<SearchResult>> BuscarSimilaresAsync(float[] queryEmbedding, int topK = 5, double umbralSimilitud = 0.3)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
        dataSourceBuilder.UseVector();
        await using var dataSource = dataSourceBuilder.Build();
        await using var conn = await dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand("""
            SELECT id, contenido, origen, pagina, 
                   1 - (embedding <=> @embedding) AS similitud
            FROM pizza.documentos_rag
            WHERE 1 - (embedding <=> @embedding) >= @umbral
            ORDER BY embedding <=> @embedding
            LIMIT @topk
            """, conn);

        cmd.Parameters.AddWithValue("embedding", new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue("umbral", umbralSimilitud);
        cmd.Parameters.AddWithValue("topk", topK);

        var results = new List<SearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new SearchResult
            {
                Id = reader.GetInt32(0),
                Contenido = reader.GetString(1),
                Origen = reader.GetString(2),
                Pagina = reader.GetInt32(3),
                Similitud = reader.GetDouble(4)
            });
        }

        return results;
    }

    /// <summary>
    /// Elimina todos los chunks de un documento específico (para re-indexar).
    /// </summary>
    public async Task EliminarPorOrigenAsync(string origen)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
        dataSourceBuilder.UseVector();
        await using var dataSource = dataSourceBuilder.Build();
        await using var conn = await dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            "DELETE FROM pizza.documentos_rag WHERE origen = @origen", conn);
        cmd.Parameters.AddWithValue("origen", origen);
        await cmd.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Resultado de una búsqueda por similitud.
/// </summary>
public class SearchResult
{
    public int Id { get; set; }
    public string Contenido { get; set; } = "";
    public string Origen { get; set; } = "";
    public int Pagina { get; set; }
    public double Similitud { get; set; }
}
