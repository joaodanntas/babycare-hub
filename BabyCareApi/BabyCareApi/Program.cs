using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// CORS: permite que o frontend (em outro domínio/porta) chame esta API.
// Em produção, troque "AllowAnyOrigin" pelo domínio real do seu site.
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("FrontendPolicy");

// A chave NUNCA fica no código. Ela vem de uma variável de ambiente.
var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

if (string.IsNullOrEmpty(geminiApiKey))
{
    Console.WriteLine("⚠️  AVISO: variável de ambiente GEMINI_API_KEY não está definida.");
}

var httpClient = new HttpClient();

// Endpoint principal: o frontend chama isso em vez de chamar o Gemini direto
app.MapPost("/api/chat", async (ChatRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Mensagem vazia." });
    }

    if (string.IsNullOrEmpty(geminiApiKey))
    {
        return Results.Problem("Chave da API não configurada no servidor.", statusCode: 500);
    }

    var semanas = request.SemanasGestacao.HasValue ? request.SemanasGestacao.Value.ToString() : "não informada";

    // O mesmo prompt de personalidade que você já tinha no frontend,
    // só que agora ele roda no servidor.
    var systemPrompt = $@"Você é a Baby IA, assistente virtual especialista em gestação, saúde materna e desenvolvimento fetal do BabyCare Hub. A usuária está na semana {semanas} de gestação.
 
    PERSONALIDADE E TOM:
    • Você é uma especialista confiável e acolhedora — converse como uma profissional experiente que também é próxima, não como uma enciclopédia.
    • Seja direta e objetiva. Frases curtas, como uma conversa de chat (estilo WhatsApp), nunca textões.
    • Varie a abertura das respostas. NÃO comece toda mensagem com ""Oi"" ou ""Olá"" — isso só é natural na primeira mensagem da conversa ou quando a própria usuária te saúda primeiro.
    • Emojis: use no máximo 1 por mensagem, e só quando realmente combinar com o tom (carinho, leveza). Nunca use mais de um na mesma resposta, e várias respostas seguidas podem não ter nenhum.
    
    COMO RESPONDER:
    • Se a usuária mandar só uma saudação (""Oi"", ""Olá"", ""Tudo bem?""), responda curto e pergunte como pode ajudar — sem despejar uma lista de dicas que ela não pediu.
    • Se a pergunta for específica (sintoma, dúvida, exame, exercício), vá direto ao ponto com informação real e útil, baseada em conhecimento médico-obstétrico estabelecido.
    • Use marcadores (•) apenas quando a resposta natural for uma lista (ex: ""quais exames fazer""); para o resto, escreva em prosa corrida e breve.
    • Adapte a resposta à semana de gestação informada sempre que for relevante (ex: sintomas comuns daquela fase, desenvolvimento do bebê).
    
    LIMITES IMPORTANTES:
    • Você pode explicar o que é comum/esperado na gestação com confiança e profundidade — isso é sua especialidade.
    • Nunca forneça diagnóstico médico individual nem substitua uma consulta. Para sintomas que podem indicar risco, ou decisões médicas específicas (medicação, exames, tratamentos), recomende consultar o obstetra — mas só quando fizer sentido, não como aviso genérico repetido em toda resposta.";

    var fullPrompt = $"{systemPrompt}\n\nPergunta da usuária: {request.Message}";

    var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={geminiApiKey}";

    var requestBody = new
    {
        contents = new[]
        {
            new
            {
                parts = new[]
                {
                    new { text = fullPrompt }
                }
            }
        }
    };

    var jsonBody = JsonSerializer.Serialize(requestBody);

    try
    {
        // Tenta até 3 vezes: se a Gemini estiver sobrecarregada (503),
        // espera um pouco e tenta novamente antes de desistir.
        const int maxTentativas = 3;
        HttpResponseMessage? geminiResponse = null;
        string responseText = "";

        for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            // Precisa criar um novo StringContent a cada tentativa,
            // pois o conteúdo já é "consumido" depois do envio.
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            geminiResponse = await httpClient.PostAsync(geminiUrl, content);
            responseText = await geminiResponse.Content.ReadAsStringAsync();

            if (geminiResponse.IsSuccessStatusCode)
            {
                break; // sucesso, não precisa tentar de novo
            }

            bool altaDemanda = responseText.Contains("UNAVAILABLE") ||
                                (int)geminiResponse.StatusCode == 503;

            if (altaDemanda && tentativa < maxTentativas)
            {
                Console.WriteLine($"Gemini sobrecarregada (tentativa {tentativa}/{maxTentativas}). Tentando novamente...");
                await Task.Delay(1000 * tentativa); // espera 1s, depois 2s
                continue;
            }

            // Erro diferente de alta demanda, ou já esgotou as tentativas
            Console.WriteLine($"Erro da API Gemini: {responseText}");
            return Results.Problem("Erro ao consultar a IA.", statusCode: 502);
        }

        using var doc = JsonDocument.Parse(responseText);
        var reply = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return Results.Ok(new { reply = reply ?? "Desculpe, não consegui processar a resposta." });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro inesperado: {ex.Message}");
        return Results.Problem("Erro interno ao processar a mensagem.", statusCode: 500);
    }
});

// Endpoint simples só para testar se a API está no ar
app.MapGet("/", () => "BabyCare Hub API está rodando! 🚀");

// Em produção (Render, Railway, etc.), a plataforma define a porta
// através da variável de ambiente PORT. Localmente, isso não existe,
// então o app continua usando a porta padrão do launchSettings.json.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();

// Modelo do que o frontend envia para nós.
// Precisa estar AQUI NO FINAL: em arquivos com "top-level statements",
// declarações de tipo (record, class, struct) só podem vir depois
// de todo o código executável do arquivo.
record ChatRequest(string Message, int? SemanasGestacao);