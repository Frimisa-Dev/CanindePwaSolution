import { getStore } from '@netlify/blobs';

export const STORE_PENDENTES   = 'solicitacoes-pendentes';
export const STORE_IMPORTADOS  = 'solicitacoes-importadas';
export const STORE_REJEITADOS  = 'solicitacoes-rejeitadas';
export const STORE_RATELIMIT   = 'ratelimit';
export const VERSAO_POLITICA_CORRENTE = '2026-11-13';
export const MAX_PAYLOAD_BYTES = 32 * 1024;
export const RATE_LIMIT_MAX    = 5;
export const RATE_LIMIT_WINDOW = 600; // 10 min

export function validarCpf(cpf) {
  if (!/^\d{11}$/.test(cpf)) return false;
  if (/^(\d)\1{10}$/.test(cpf)) return false;
  const calc = (n) => {
    let sum = 0;
    for (let i = 0; i < n; i++) sum += parseInt(cpf[i]) * (n + 1 - i);
    const r = (sum * 10) % 11;
    return r === 10 ? 0 : r;
  };
  return calc(9) === parseInt(cpf[9]) && calc(10) === parseInt(cpf[10]);
}

export function normalizarNome(s) {
  return (s || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '')
    .toLowerCase().replace(/\s+/g, ' ').trim();
}

export function calcularIdade(dataNascimento) {
  const nasc = new Date(dataNascimento + 'T00:00:00Z');
  if (isNaN(nasc.getTime())) return null;
  const hoje = new Date();
  let idade = hoje.getUTCFullYear() - nasc.getUTCFullYear();
  const m = hoje.getUTCMonth() - nasc.getUTCMonth();
  if (m < 0 || (m === 0 && hoje.getUTCDate() < nasc.getUTCDate())) idade--;
  return idade;
}

export async function sha256Hex(str) {
  const buf = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(str));
  return Array.from(new Uint8Array(buf)).map(b => b.toString(16).padStart(2, '0')).join('');
}

export function safeEqual(a, b) {
  if (typeof a !== 'string' || typeof b !== 'string' || a.length !== b.length) return false;
  let d = 0;
  for (let i = 0; i < a.length; i++) d |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return d === 0;
}

export function normalizarEValidar(p) {
  const e = {};
  if (!p.nome || p.nome.trim().length < 3) e.nome = 'Nome obrigatório (mín. 3)';
  if (!p.email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(p.email)) e.email = 'E-mail inválido';
  if (!p.dataNascimento || !/^\d{4}-\d{2}-\d{2}$/.test(p.dataNascimento)) e.dataNascimento = 'Data inválida';
  if (!p.cpf || !validarCpf(p.cpf)) e.cpf = 'CPF inválido';
  if (!p.aceiteDeclaracao) e.aceiteDeclaracao = 'Aceite da declaração obrigatório';
  if (!p.aceiteLgpd) e.aceiteLgpd = 'Aceite LGPD obrigatório';
  if (p.versaoPolitica !== VERSAO_POLITICA_CORRENTE) e.versaoPolitica = 'Versão da política desatualizada';
  if (!p.assinatura || normalizarNome(p.assinatura) !== normalizarNome(p.nome))
    e.assinatura = 'Assinatura deve coincidir com o nome';

  if (!e.dataNascimento) {
    const idade = calcularIdade(p.dataNascimento);
    if (idade === null) e.dataNascimento = 'Data inválida';
    else if (idade < 18) {
      if (!p.nomeResponsavel) e.nomeResponsavel = 'Nome do responsável obrigatório';
      if (!p.cpfResponsavel || !validarCpf(p.cpfResponsavel)) e.cpfResponsavel = 'CPF do responsável inválido';
      if (!p.aceiteResponsavelLgpd) e.aceiteResponsavelLgpd = 'Aceite do responsável obrigatório';
    }
  }

  const proibido = /<\s*script|javascript:|on\w+\s*=/i;
  for (const [k, v] of Object.entries(p)) {
    if (typeof v === 'string' && proibido.test(v)) e[k] = 'Conteúdo não permitido';
  }
  return e;
}

export function sanitizarPayload(p) {
  const maxLen = {
    nome: 200, apelido: 100, nacionalidade: 60, estadoCivil: 40, profissao: 100,
    vinculoHistorico: 2000, rg: 40, rua: 200, numero: 20, bairro: 100, cidade: 100,
    email: 200, assinatura: 200, nomeResponsavel: 200, versaoPolitica: 20,
    cpf: 11, cep: 8, telefone: 20, uf: 2, dataNascimento: 10, cpfResponsavel: 11,
  };
  const out = {};
  for (const [k, v] of Object.entries(p)) {
    if (typeof v === 'string') out[k] = v.slice(0, maxLen[k] || 500).trim();
    else if (typeof v === 'boolean' || typeof v === 'number') out[k] = v;
    else if (Array.isArray(v)) out[k] = v.filter(x => typeof x === 'string').slice(0, 10);
  }
  return out;
}

export function autorizarFalcao(event) {
  const token = event.headers['x-falcao-token'] || '';
  const esperado = process.env.FALCAO_PULL_TOKEN || '';
  if (!esperado) return false; // Fail-closed
  return safeEqual(token, esperado);
}

export async function validarTurnstile(token, ip) {
  if (!token || !process.env.TURNSTILE_SECRET) return !process.env.TURNSTILE_SECRET;
  try {
    const res = await fetch('https://challenges.cloudflare.com/turnstile/v0/siteverify', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ secret: process.env.TURNSTILE_SECRET, response: token, remoteip: ip }),
    });
    const data = await res.json();
    return data.success === true;
  } catch { return false; }
}

export async function checarRateLimit(ip) {
  const store = getStore({ name: STORE_RATELIMIT, consistency: 'strong' });
  const key = `ip-${ip}`;
  const now = Math.floor(Date.now() / 1000);
  let state = await store.get(key, { type: 'json' });
  if (!state || now - state.windowStart > RATE_LIMIT_WINDOW) state = { windowStart: now, count: 0 };
  state.count++;
  await store.setJSON(key, state);
  return { excedido: state.count > RATE_LIMIT_MAX, reset: state.windowStart + RATE_LIMIT_WINDOW };
}

export function jsonResponse(statusCode, body, extraHeaders = {}) {
  return {
    statusCode,
    headers: { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store', ...extraHeaders },
    body: JSON.stringify(body),
  };
}