// netlify/functions/mark-imported.js (Functions v2)
import { getStore } from '@netlify/blobs';
import {
  STORE_PENDENTES, STORE_IMPORTADOS, STORE_REJEITADOS,
  autorizarFalcao
} from './_lib/helpers.js';

function jsonResponse(status, body) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' }
  });
}

export default async (req, context) => {
  if (req.method !== 'POST') return jsonResponse(405, { erro: 'Método não permitido' });

  // Cria um evento fake para reutilizar o autorizarFalcao existente
  const token = req.headers.get('x-falcao-token') || '';
  const esperado = process.env.FALCAO_PULL_TOKEN || '';
  if (!esperado || token !== esperado) return jsonResponse(403, { erro: 'Acesso negado' });

  let body;
  try { body = await req.json(); }
  catch { return jsonResponse(400, { erro: 'JSON inválido' }); }

  const { importados = [], rejeitados = [], rejeitadosMotivos = {} } = body;
  const pendentes = getStore({ name: STORE_PENDENTES, consistency: 'strong' });
  const impStore   = getStore({ name: STORE_IMPORTADOS, consistency: 'strong' });
  const rejStore   = getStore({ name: STORE_REJEITADOS, consistency: 'strong' });
  const agora = new Date().toISOString();
  let processados = 0;

  try {
    for (const id of importados) {
      const item = await pendentes.get(id, { type: 'json' });
      if (!item) continue;
      await impStore.setJSON(id, { ...item, importadoEmUtc: agora });
      await pendentes.delete(id);
      processados++;
    }
    for (const id of rejeitados) {
      const item = await pendentes.get(id, { type: 'json' });
      if (!item) continue;
      await rejStore.setJSON(id, {
        ...item, rejeitadoEmUtc: agora,
        motivoRejeicao: rejeitadosMotivos[id] || 'Não especificado',
      });
      await pendentes.delete(id);
      processados++;
    }
    return jsonResponse(200, { ok: true, processados });
  } catch (err) {
    console.error('ERRO mark-imported:', err);
    return jsonResponse(500, { erro: 'Function crash', mensagem: err?.message || String(err) });
  }
};