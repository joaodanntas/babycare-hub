// ─────────────────────────────────────────────────────────────
// TRECHO ATUALIZADO de script.js
// Substitui o bloco antigo que chamava o Gemini direto do navegador.
// ─────────────────────────────────────────────────────────────

// 1. REMOVA esta linha do seu script.js (a chave não fica mais aqui):
// const ai = new GoogleGenAI({ apiKey: "AQ.Ab8RN6K..." });

// 2. Defina a URL do seu backend (troque quando fizer o deploy)
const API_BASE_URL = "http://localhost:5000"; // depois: "https://sua-api.onrender.com"

// 3. Substitua a função sendMessage() inteira por esta versão:
async function sendMessage() {
  const input = document.getElementById('chatInput');
  const btnEnviar = document.getElementById('btnSendChat');

  if (!input) return;

  const text = input.value.trim();
  if (!text) return;

  input.value = '';
  if (btnEnviar) btnEnviar.disabled = true;

  addChatMessage('user', text);
  addTypingIndicator();

  const semanasGestacao = (typeof state !== 'undefined' && state.user && state.user.semanas)
    ? state.user.semanas
    : null;

  try {
    // Agora chamamos NOSSO backend, não o Gemini direto.
    const response = await fetch(`${API_BASE_URL}/api/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        message: text,
        semanasGestacao: semanasGestacao
      })
    });

    if (!response.ok) {
      throw new Error('Erro na resposta do servidor');
    }

    const data = await response.json();

    removeTypingIndicator();
    addChatMessage('assistant', data.reply || 'Desculpe, não consegui processar a resposta. Tente novamente!');

  } catch (error) {
    console.error("Erro ao chamar o backend:", error);
    removeTypingIndicator();
    addChatMessage('assistant', 'Desculpe, a Baby IA encontrou uma instabilidade. Pode tentar reenviar? 💙');
  }

  if (btnEnviar) btnEnviar.disabled = false;
}
