using CanindePwaShared.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CanindeWebServer.Data
{
    /// <summary>
    /// Traduz os enums do C# para os valores REAIS gravados no MySQL, sem alterar nenhum enum existente.
    /// Use em OnModelCreating: .HasConversion(EnumConverters.Estatus) / .HasConversion(EnumConverters.TipoCep)
    /// </summary>
    public static class EnumConverters
    {
        // Banco: 'Ativo' | 'Desativado' | 'Bloqueado'   ↔   C#: Ativo | Inativo | Bloqueado
        public static readonly ValueConverter<EstatusEnum, string> Estatus = new(
            v => v == EstatusEnum.Inativo ? "Desativado" : v == EstatusEnum.Bloqueado ? "Bloqueado" : "Ativo",
            s => s == "Desativado" ? EstatusEnum.Inativo : s == "Bloqueado" ? EstatusEnum.Bloqueado : EstatusEnum.Ativo);

        // Banco: 'NaoCadastrado' | 'Provisorio' | 'PorCidade' | 'PorRua'   ↔   C#: Provisorio | Unico | Definitivo
        // 'NaoCadastrado' não tem equivalente no enum e hoje não existe nenhuma linha com ele (lê como Provisorio).
        public static readonly ValueConverter<TipoCepEnum, string> TipoCep = new(
            v => v == TipoCepEnum.Definitivo ? "PorRua" : v == TipoCepEnum.Unico ? "PorCidade" : "Provisorio",
            s => s == "PorRua" ? TipoCepEnum.Definitivo : s == "PorCidade" ? TipoCepEnum.Unico : TipoCepEnum.Provisorio);
    }
}
