using Microsoft.AspNetCore.Mvc;
using FalcaoWebServer.Services;
using FalcaoPwaShared.Models.Dtos.TableDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.JSInterop.Infrastructure;

namespace FalcaoWebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // leitura/gravação requer usuário autenticado
    public class PessoaController : ControllerBase
    {
        private readonly IPessoaService _pessoaService;
        private readonly ILogger<PessoaController> _logger;

        public PessoaController(IPessoaService pessoaService, ILogger<PessoaController> logger)
        {
            _pessoaService = pessoaService;
            _logger = logger;
        }
        // GET /api/pessoas?page=1&pageSize=50&search=xyz
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
        {
            var (items, total) = await _pessoaService.GetAllAsync(page, pageSize, search);
            return Ok(new { total, page, pageSize, items });
        }

        // GET /api/produtos/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _pessoaService.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>
        /// Cria uma nova pessoa (física ou jurídica)
        /// </summary>
        /// <param name="dto">Dados da pessoa</param>
        /// <returns>ID da pessoa criada</returns>
        [HttpPost]
        public async Task<IActionResult> CreatePessoaAsync([FromBody] TblPessoaDto dto)
        {
            try
            {
                // 🔹 Validação básica do modelo
                if (!ModelState.IsValid)
                {
                    var erros = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Validação falhou ao criar pessoa: {Erros}", string.Join("; ", erros));
                    return BadRequest(new { Message = "Erro de validação", Erros = erros });
                }

                // DEBUG helper - log antes de salvar
                _logger.LogInformation("Recebida requisição para criar pessoa: {Nome}", dto.Nome);
                _logger.LogInformation("Recebida requisição para criar pessoa: {Estatus}", dto.Estatus);

                // 🔹 Chama o serviço de persistência
                int idPessoa = await _pessoaService.CreatePessoaAsync(dto);

                _logger.LogInformation("Pessoa criada com sucesso. ID: {Id}", idPessoa);

                // 🔹 Retorna sucesso com ID
                return Ok(new
                {
                    Message = "Pessoa criada com sucesso.",
                    ID_Pessoa = idPessoa
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação de argumento: {Msg}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erro de operação inválida: {Msg}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Erro inesperado ao criar pessoa. Inner: {Inner}", inner);
                return StatusCode(500, new { Message = "Erro interno do servidor.", Detalhe = inner });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePessoaAsync(int id, [FromBody] TblPessoaDto dto)
        {
            try
            {
                int idPessoa = await _pessoaService.UpdatePessoaAsync(id, dto);
                return Ok(new { Message = "Pessoa atualizada com sucesso.", ID_Pessoa = idPessoa });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar pessoa ID {Id}.", id);
                return StatusCode(500, new { Message = "Erro interno ao atualizar pessoa.", Detalhe = ex.Message });
            }
        }
    }
}

