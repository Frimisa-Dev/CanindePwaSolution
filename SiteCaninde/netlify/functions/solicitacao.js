// netlify/functions/solicitacao.js (Functions v2)
import { getStore } from '@netlify/blobs';
import {
  STORE_PENDENTES, MAX_PAYLOAD_BYTES,
  validarTurnstile, normalizarEValidar, checarRateLimit,
  sha256Hex, sanitizarPayload
} from './_lib/helpers.js';

function jsonResponse(status, body) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' }
  });
}

export default async (req, context) => {
  if (req.method !== 'POST') return jsonResponse(405, { erro: 'Método não permitido' });

  const bodyStr = await req.text();
  if (bodyStr.length > MAX_PAYLOAD_BYTES) return jsonResponse(413, { erro: 'Payload muito grande' });

  let payload;
  try { payload = JSON.parse(bodyStr); }
  catch { return jsonResponse(400, { erro: 'JSON inválido' }); }

  const ip = req.headers.get('x-nf-client-connection-ip')
    || (req.headers.get('x-forwarded-for') || '').split(',')[0].trim()
    || 'desconhecido';

  const rl = await checarRateLimit(ip);
  if (rl.excedido) {
    return jsonResponse(429, { erro: 'Muitas tentativas. Aguarde alguns minutos.' });
  }

  const turnstileOk = await validarTurnstile(payload.turnstileToken, ip);
  if (!turnstileOk) return jsonResponse(400, { erro: 'Verificação anti-robô falhou' });

  const erros = normalizarEValidar(payload);
  if (Object.keys(erros).length) return jsonResponse(400, { erro: 'validacao', campos: erros });

  const idSolicitacaoSite = crypto.randomUUID();
  const hoje = new Date();
  const ymd = hoje.toISOString().slice(0, 10).replace(/-/g, '');
  const protocoloSite = `SITE-${ymd}-${Math.random().toString(36).substring(2, 6).toUpperCase()}`;
  const payloadOriginalHash = await sha256Hex(bodyStr);
  const payloadLimpo = sanitizarPayload(payload);

  try {
    const store = getStore({ name: STORE_PENDENTES, consistency: 'strong' });
    await store.setJSON(idSolicitacaoSite, {
      idSolicitacaoSite,
      protocoloSite,
      criadoEmUtc: hoje.toISOString(),
      ipOrigem: ip,
      userAgent: (req.headers.get('user-agent') || '').slice(0, 500),
      payloadOriginalHash,
      payload: payloadLimpo,
    });

    return jsonResponse(201, {
      protocoloSite,
      mensagem: 'Recebemos sua ficha. Entraremos em contato pelo canal escolhido.',
    });
  } catch (err) {
    console.error('ERRO solicitacao:', err);
    return jsonResponse(500, {
      erro: 'Falha ao gravar',
      mensagem: err?.message || String(err)
    });
  }
};