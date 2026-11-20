using CanindePwaShared.Models.Dto.TableDto;
using CanindeWebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanindeWebServer.Controllers
{
    /// <summary>
    /// Pessoas e seus perfis. Acesso: usuário ativo com papel Ceo, Administrador ou Operador
    /// (policy "DashboardAccess" do Program.cs).
    ///
    /// Erros: 400 dado inválido | 404 não encontrada | 409 conflito (ex.: CPF já cadastrado) |
    ///        422 regra de negócio | 500 erro interno (mensagem genérica + código de correlação;
    ///        o detalhe técnico vai só para o log).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAccess")]
    public class PessoaController : ControllerBase
    {
        private readonly IPessoaService _pessoaService;
        private readonly ILogger<PessoaController> _logger;

        public PessoaController(IPessoaService pessoaService, ILogger<PessoaController> logger)
        {
            _pessoaService = pessoaService;
            _logger = logger;
        }

        /// <summary>Lista paginada. GET /api/Pessoa?page=1&amp;pageSize=50&amp;search=maria&amp;perfil=Embarcador</summary>
        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
                                          [FromQuery] string? search = null, [FromQuery] string? perfil = null,
                                          CancellationToken ct = default)
            => Executar(async () =>
            {
                var (items, total) = await _pessoaService.GetAllAsync(page, pageSize, search, perfil, ct);
                return Ok(new { total, page, pageSize, items });
            });

        /// <summary>Detalhe da pessoa com seus perfis.</summary>
        [HttpGet("{id:int}")]
        public Task<IActionResult> Get(int id, CancellationToken ct)
            => Executar(async () =>
            {
                var item = await _pessoaService.GetByIdAsync(id, ct);
                return item == null ? NotFound(new { Message = "Pessoa não encontrada." }) : Ok(item);
            });

        /// <summary>Cria pessoa (física ou jurídica) com e-mail e perfis, em uma transação.</summary>
        [HttpPost]
        public Task<IActionResult> Create([FromBody] TblPessoaRequestDto dto, CancellationToken ct)
            => Executar(async () =>
            {
                var id = await _pessoaService.CreatePessoaAsync(dto, ct);
                return CreatedAtAction(nameof(Get), new { id }, new { Message = "Pessoa criada com sucesso.", ID_Pessoa = id });
            });

        /// <summary>Atualiza dados básicos (tipo e CPF/CNPJ não mudam por aqui).</summary>
        [HttpPut("{id:int}")]
        public Task<IActionResult> Update(int id, [FromBody] TblPessoaRequestDto dto, CancellationToken ct)
            => Executar(async () =>
            {
                await _pessoaService.UpdatePessoaAsync(id, dto, ct);
                return Ok(new { Message = "Pessoa atualizada com sucesso.", ID_Pessoa = id });
            });

        /// <summary>Inativa a pessoa (nunca apaga).</summary>
        [HttpDelete("{id:int}")]
        public Task<IActionResult> Inativar(int id, CancellationToken ct)
            => Executar(async () =>
            {
                await _pessoaService.InativarAsync(id, ct);
                return Ok(new { Message = "Pessoa inativada.", ID_Pessoa = id });
            });

        [HttpPost("{id:int}/reativar")]
        public Task<IActionResult> Reativar(int id, CancellationToken ct)
            => Executar(async () =>
            {
                await _pessoaService.ReativarAsync(id, ct);
                return Ok(new { Message = "Pessoa reativada.", ID_Pessoa = id });
            });

        /// <summary>Perfis da pessoa (lista vazia = pessoa sem perfil).</summary>
        [HttpGet("{id:int}/perfis")]
        public Task<IActionResult> GetPerfis(int id, CancellationToken ct)
            => Executar(async () => Ok(await _pessoaService.GetPerfisAsync(id, ct)));

        /// <summary>Adiciona um perfil (respeita a regra: no máximo um, exceto Embarcador + Agenciador).</summary>
        [HttpPost("{id:int}/perfis/{perfil}")]
        public Task<IActionResult> AdicionarPerfil(int id, string perfil, [FromBody] PessoaPerfilRequestDto? dados, CancellationToken ct)
            => Executar(async () =>
            {
                await _pessoaService.AdicionarPerfilAsync(id, perfil, dados, ct);
                return Ok(new { Message = "Perfil adicionado.", ID_Pessoa = id, Perfil = perfil });
            });

        // ------------------------------------------------------------------
        // Tradução de exceções em respostas HTTP (sem vazar detalhes internos)
        // ------------------------------------------------------------------
        private async Task<IActionResult> Executar(Func<Task<IActionResult>> acao)
        {
            try
            {
                return await acao();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ConflitoServiceException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (RegraNegocioServiceException ex)
            {
                return UnprocessableEntity(new { Message = ex.Message });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                var codigo = HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Erro inesperado em Pessoa. Código {Codigo}", codigo);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "Erro interno do servidor.", Codigo = codigo });
            }
        }
    }
}
