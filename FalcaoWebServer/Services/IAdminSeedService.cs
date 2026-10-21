using System.IO.Pipelines;
using FalcaoPwaShared.Models.Dtos.AuthDto;

/// <summary>
/// ?? ATENCAO - SERVICO ESPECIALIZADO PARA BOOTSTRAP DO SISTEMA ??
/// 
/// MOTIVACAO ARQUITETURAL:
/// =======================
/// O sistema Falcao possui DOIS fluxos distintos de criacao de usuario:
/// 
/// 1. BOOTSTRAP INICIAL (Seed)
///    - Contexto: Sistema vazio, primeira execucao
///    - Requisitos: Criar usuario mestre SEM PIN, SEM confirmacao, ATIVO imediatamente
///    - Responsavel: Program.cs (infraestrutura)
///    
/// 2. FLUXO NORMAL DE NEGOCIO
///    - Contexto: Sistema operacional, admin autenticado
///    - Requisitos: PIN, confirmacao de e-mail, auditoria completa
///    - Responsavel: AuthController (dominio)
///    
/// POR QUE NAO USAR IAuthManager PARA AMBOS?
/// =========================================
/// ? VIOLACAO DO PRINCIPIO DA RESPONSABILIDADE UNICA
///    - IAuthManager teria que saber "quando" esta sendo executado
///    - Adicionaria logica condicional complexa (if isSeed, if isAdmin, etc.)
///    
/// ? ACOPLAMENTO TEMPORAL PERIGOSO
///    - O mesmo servico teria comportamentos diferentes baseados no "momento"
///    - Dificulta testes e manutencao
///    
/// ? CONFUSAO CONCEITUAL
///    - Bootstrap NAO e um caso de uso de negocio
///    - E uma necessidade tecnica/infraestrutural
///    
/// ANALOGIA:
/// =========
/// IAdminSeedService   Construtor que instala a primeira fechadura
/// IAuthManager        Chaveiro que cria novas chaves para moradores
/// 
/// O construtor NAO e um chaveiro, mesmo que ambos trabalhem com fechaduras.
/// </summary>
public interface IAdminSeedService
{
    /// <summary>
    /// ?? METODO DE INFRAESTRUTURA - NAO E UM CASO DE USO DE NEGOCIO ??
    /// 
    /// OBJETIVO EXCLUSIVO:
    /// Garantir que o sistema tenha um usuario administrativo na PRIMEIRA execucao.
    /// 
    /// DIFERENCAS CRITICAS DO IAuthManager.CadastroUsuarioUserAsync():
    /// ---------------------------------------------------------
    /// | CARACTERISTICA       | IAdminSeedService        | IAuthManager             |
    /// |----------------------|--------------------------|--------------------------|
    /// | Contexto            | Sistema vazio           | Sistema operacional     |
    /// | Validacao de PIN    | NAO aplica              | OBRIGATORIA             |
    /// | Ativacao automatica | SIM (IsActive = true)   | Aguarda confirmacao     |
    /// | Auditoria           | Log tecnico             | Log completo de negocio |
    /// | Chamador            | Program.cs (infra)      | AuthController (dominio)|
    /// | Frequencia          | UMA vez na vida do sist.| Multiplas vezes         |
    /// 
    /// REGRA DE USO:
    /// -------------
    /// 1. Chamado APENAS por Program.cs durante inicializacao
    /// 2. Chamado APENAS se Users table estiver VAZIA
    /// 3. NUNCA chamado apos o sistema estar populado
    /// 
    /// EXCECAO JUSTIFICADA:
    /// O bypass das regras de negocio (PIN, ativacao) e ACEITAVEL porque:
    /// 1. E um caso tecnico de infraestrutura
    /// 2. Sem ele, o sistema fica inacessivel
    /// 3. O risco e controlado (apenas na primeira execucao)
    /// </summary>
    Task<SeedResultDto> SeedAdministrativeUserAsync(SeedAdminDto dto);
}