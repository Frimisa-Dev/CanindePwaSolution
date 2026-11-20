namespace CanindePwaShared.Models.Dto.TableDto
{
    /// <summary>
    /// Linha do resultado da consulta "perfil × pessoa" (SQL sobre as 8 tabelas tblcdpessoa*).
    /// Não é tabela do banco: transporta o resultado para o PessoaService.
    /// </summary>
    public class PerfilLinhaResponseDto
    {
        public string Perfil { get; set; } = string.Empty;
        public long IdPessoa { get; set; }
    }
}
