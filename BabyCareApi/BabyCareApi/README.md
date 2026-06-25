# BabyCare Hub API — Backend C# (ASP.NET Minimal API)

Esse backend protege sua chave da Gemini. O frontend chama esse servidor,
e só o servidor conversa com o Google.

## 1. Instalar o .NET SDK (se ainda não tiver)
Baixe o .NET 8 SDK em: https://dotnet.microsoft.com/download
Depois de instalar, confirme no terminal:
```
dotnet --version
```
Deve mostrar algo como "8.0.x".

## 2. Rodar o projeto localmente

Abra o terminal dentro da pasta `BabyCareApi` (onde está o `Program.cs`)
e rode:

```
dotnet restore
```

Isso baixa as dependências (só uma vez).

### 2.1 Definir a chave da API como variável de ambiente

**NUNCA** coloque a chave dentro do `Program.cs`. Defina assim:

**No Windows (PowerShell):**
```
$env:GEMINI_API_KEY="sua_chave_aqui"
```

**No Linux/Mac (bash):**
```
export GEMINI_API_KEY="sua_chave_aqui"
```

⚠️ Use uma chave NOVA — gere outra no Google AI Studio e revogue a antiga
que estava exposta no script.js, ela já deve ser tratada como comprometida.

### 2.2 Rodar o servidor
```
dotnet run
```

Você vai ver algo como:
```
Now listening on: http://localhost:5000
```

## 3. Testar se está funcionando

Em outro terminal, teste com curl:
```bash
curl -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d "{\"message\":\"Oi, como você pode me ajudar?\",\"semanasGestacao\":\"20\"}"
```

Se voltar um JSON com `"reply": "..."`, está funcionando.

## 4. Conectar o frontend

No seu `script.js`, troque o bloco que chama o Gemini direto por uma
chamada ao seu backend. Veja o arquivo `script_atualizado.js` que enviei
junto — só o trecho da função `sendMessage()` mudou.

## 5. Quando for colocar no ar (deploy)

Para produção, algumas opções gratuitas/fáceis para hospedar uma API .NET:
- **Render.com** (free tier, suporta Docker ou build direto de .NET)
- **Railway.app** (free tier pequeno, muito simples de configurar)
- **Azure App Service** (free tier, já que é o ecossistema nativo do .NET)

Em qualquer uma delas, você define a variável de ambiente `GEMINI_API_KEY`
no painel da plataforma (nunca no código), e troca a URL no frontend de
`http://localhost:5000` para a URL pública que a plataforma te der.

Na seção de CORS do `Program.cs`, troque `AllowAnyOrigin()` pelo domínio
real do seu site quando for para produção — isso evita que outros sites
usem sua API de graça.
