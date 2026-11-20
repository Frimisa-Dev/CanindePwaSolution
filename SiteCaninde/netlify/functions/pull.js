// netlify/functions/pull.js (Functions v2)
import { getStore } from '@netlify/blobs';
import { STORE_PENDENTES, autorizarFalcao } from './_lib/helpers.js';

export default async (req, context) => {
  // 1. Validação de método
  if (req.method !== 'GET') {
    return new Response(JSON.stringify({ erro: 'Método não permitido' }), {
      status: 405,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  // 2. Validação do Token do Falcao
  const token = req.headers.get('x-falcao-token') || '';
  const esperado = process.env.FALCAO_PULL_TOKEN || '';
  
  // Comparação segura (simulada)
  if (!esperado || token !== esperado) {
    return new Response(JSON.stringify({ erro: 'Acesso negado' }), {
      status: 403,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  try {
    // 3. Conexão com o Blobs (v2 já injeta o contexto automaticamente)
    const store = getStore({
      name: STORE_PENDENTES,
      consistency: 'strong'
    });

    const { blobs } = await store.list();
    const items = [];

    for (const b of blobs) {
      const item = await store.get(b.key, { type: 'json' });
      if (item) items.push(item);
    }

    return new Response(JSON.stringify({ items, total: items.length }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });

  } catch (err) {
    console.error('ERRO pull v2:', err);
    return new Response(JSON.stringify({
      erro: 'Function crash',
      mensagem: err?.message || String(err)
    }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' }
    });
  }
};