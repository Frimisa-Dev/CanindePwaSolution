using CanindePwaShared.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace CanindePwaShared.Models.Tbl
{
    // Entidades dos perfis de pessoa.
    // REGRA: nome da classe == nome da tabela SQL (com Cd quando o SQL tem).
    // PK/FK nunca tem Cd: ID_Pessoa, ID_PessoaEmbarcador, etc.
    // Só chaves escalares: sem navegação para TblPessoa/TblUsuario/MbUsuario.

    public class TblPessoa
    {
        public int ID_Pessoa { get; set; }
        public int ID_Cep { get; set; } = 0;
        public int CodigoPessoaTipo { get; set; } = 0;
        public string? Numero { get; set; } = string.Empty;
        public string? ComplementoNumero { get; set; } = string.Empty;
        public string? DddFixo { get; set; } = string.Empty;
        public string? Fixo { get; set; } = string.Empty;
        public string? DddCelular { get; set; } = string.Empty;
        public string? Celular { get; set; } = string.Empty;
        public string? Contato { get; set; } = string.Empty;
        public EstatusEnum Estatus { get; set; }
    }

    public class TblPessoaEmail
    {
        public int ID_PessoaEmail { get; set; }
        public int ID_Pessoa { get; set; }
        [Column("Setor")]
        public SetorEnum Setor { get; set; }
        public string? Email { get; set; }
        public SimNaoEnum SelecionadoParaImprimir { get; set; } = SimNaoEnum.Nao;
    }

    public class TblPessoaFisica
    {
        public int ID_PessoaFisica { get; set; }
        public int ID_Pessoa { get; set; } = 0;
        public string? Nome { get; set; } = string.Empty;
        public string? Cpf { get; set; } = string.Empty;
        public string? Rg { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
    }

    public class TblPessoaJuridica
    {
        public int ID_PessoaJuridica { get; set; }
        public int ID_Pessoa { get; set; } = 0;
        public string? RazaoSocial { get; set; } = string.Empty;
        public string? Cnpj { get; set; } = string.Empty;
        public string? InscricaoEstadual { get; set; } = string.Empty;
        public DateTime DataFundacao { get; set; }
    }

    // ========================================================================
    // Perfis de pessoa (tabelas tblCdPessoa*)
    // ========================================================================

    public class TblCdPessoaParceiroFrete
    {
        public uint ID_PessoaParceiroFrete { get; set; }
        public uint ID_Pessoa { get; set; }
        public uint ID_Usuario { get; set; }
        public uint ID_MobileUsuario { get; set; }
    }

    public class TblCdPessoaCliente
    {
        public uint ID_PessoaCliente { get; set; }
        public uint ID_Pessoa { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TblCdPessoaEmbarcador
    {
        public uint ID_PessoaEmbarcador { get; set; }
        public uint ID_Pessoa { get; set; }
        public uint ID_Usuario { get; set; }
        public uint ID_MobileUsuario { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TblCdPessoaCorretor
    {
        public uint ID_PessoaCorretor { get; set; }
        public uint ID_Pessoa { get; set; }
        public uint ID_Usuario { get; set; }
        public uint ID_MobileUsuario { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TblCdPessoaAgenciador
    {
        public uint ID_PessoaAgenciador { get; set; }
        public uint ID_Pessoa { get; set; }
        public uint ID_Usuario { get; set; }
        public uint ID_MobileUsuario { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TblCdPessoaMotorista
    {
        public uint ID_PessoaMotorista { get; set; }
        public uint ID_Pessoa { get; set; }
        public uint ID_PessoaAgenciador { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TblCdPessoaFornecedor
    {
        public uint ID_PessoaFornecedor { get; set; }
        public uint ID_Pessoa { get; set; }
        public string NomeFantasia { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TblCdPessoaProdutorRural
    {
        public uint ID_PessoaProdutorRural { get; set; }
        public uint ID_Pessoa { get; set; }
        public string? Pis { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TblCdPessoaProdutorRuralContaBancaria
    {
        public uint ID_PessoaProdutorRuralContaBancaria { get; set; }
        public uint ID_PessoaProdutorRural { get; set; }
        public uint ID_Banco { get; set; }
        public uint ID_CidadeEstado { get; set; }
        public string Agencia { get; set; } = string.Empty;
        public string Conta { get; set; } = string.Empty;
        public string Compensacao { get; set; } = string.Empty;
        public string TipoConta { get; set; } = "Corrente";   // 'Corrente' | 'Poupança'
        public string Estatus { get; set; } = "Ativo";        // 'Ativo' | 'Inativo'
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TblCdPessoaProdutorRuralPropriedade
    {
        public uint ID_PessoaProdutorRuralPropriedade { get; set; }
        public uint ID_PessoaProdutorRural { get; set; }
        public uint ID_Propriedade { get; set; }
        public string Estatus { get; set; } = "Ativo";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TblCdPropriedade
    {
        public uint ID_Propriedade { get; set; }
        public uint ID_CidadeEstado { get; set; }
        public string Ipr { get; set; } = string.Empty;
        public string NomePropriedade { get; set; } = string.Empty;
        public string EnderecoPropriedade { get; set; } = string.Empty;
        public string Estatus { get; set; } = "Ativo";
    }
}