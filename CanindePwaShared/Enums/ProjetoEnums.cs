/*
===============================================================================
CanindePwaSolution — Enums consolidados (arquivo único)
===============================================================================
*/

namespace CanindePwaShared.Enums
{
    // ═══════════════════════════════════════════════════════════════════════
    //  1. AUTENTICAÇÃO / USUÁRIO
    // ═══════════════════════════════════════════════════════════════════════

    public enum AuthModeEnum
    {
        None,
        Cookie
    }

    public enum UsuarioEnum
    {
        Ceo,
        Administrador,
        Financeiro,
        Site,
        Operador
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  2. PERFIL / SÓCIO / CLASSIFICAÇÃO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Perfis de pessoa aceitos pelo sistema.
    /// O DTO trafega como string (compatibilidade WinForms/PWA);
    /// o Service converte para este enum e usa type-safety.
    /// </summary>
    public enum PerfilEnum
    {
        Embarcador,
        Agenciador,
        Motorista,
        Corretor,
        Cliente,
        Fornecedor,
        ProdutorRural,
        ParceiroFrete
    }

    public static class PerfilEnumExtensions
    {
        /// <summary>Converte string do DTO/WinForms para o enum (ignora caixa, acento e espaços).</summary>
        public static PerfilEnum? FromString(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var chave = valor.Trim()
                             .ToLowerInvariant()
                             .Replace("á", "a").Replace("ã", "a").Replace("â", "a")
                             .Replace("é", "e").Replace("ê", "e")
                             .Replace("í", "i")
                             .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
                             .Replace("ú", "u").Replace("ç", "c")
                             .Replace(" ", "").Replace("_", "").Replace("-", "");

            return chave switch
            {
                "embarcador" => PerfilEnum.Embarcador,
                "agenciador" => PerfilEnum.Agenciador,
                "motorista" => PerfilEnum.Motorista,
                "corretor" => PerfilEnum.Corretor,
                "cliente" => PerfilEnum.Cliente,
                "fornecedor" => PerfilEnum.Fornecedor,
                "produtorrural" => PerfilEnum.ProdutorRural,
                "parceirofrete" => PerfilEnum.ParceiroFrete,
                _ => null
            };
        }

        /// <summary>Converte o enum para o formato persistido (minúsculo, sem acento).</summary>
        public static string ToStringDb(this PerfilEnum perfil) => perfil.ToString().ToLowerInvariant();
    }

    public enum EstatusEnum
    {
        Ativo,
        Inativo,
        Bloqueado
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  3. ENDEREÇO / CEP
    // ═══════════════════════════════════════════════════════════════════════

    public enum TipoCepEnum
    {
        Definitivo,
        Provisorio,
        Unico
    }

    public enum ResultadoEnum
    {
        OK,
        NaoEncontrado,
        ProblemaPesquisa,
        CorreioForaOperacao
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  4. COMUNICAÇÃO / SISTEMA
    // ═══════════════════════════════════════════════════════════════════════

    public enum SetorEnum
    {
        Cadastro,
        Administrativo,
        Financeiro
    }

    public enum TipoMensagemEnum
    {
        Sucesso,
        Erro,
        Aviso,
        Info
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  5. BOOLEANO
    // ═══════════════════════════════════════════════════════════════════════

    public enum SimNaoEnum
    {
        Sim,
        Nao
    }

    
}