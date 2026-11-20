using System.ComponentModel.DataAnnotations;

namespace CanindePwaShared.Models.Dto.TableDto
{
    /// <summary>
    /// Dados específicos ao ADICIONAR um perfil a uma pessoa já cadastrada
    /// (POST /api/Pessoa/{id}/perfis/{perfil}). A maioria dos perfis não precisa de nenhum dado.
    /// </summary>
    public class PessoaPerfilRequestDto
    {
        [StringLength(45)] public string? NomeFantasia { get; set; }   // Fornecedor (obrigatório)
        [StringLength(20)] public string? Pis { get; set; }            // ProdutorRural (opcional)
        public int? ID_Usuario { get; set; }                           // Embarcador/Agenciador/Corretor/ParceiroFrete (obrigatório)
    }
}
