using CanindePwaShared.Models.Dto.TableDto;
using CanindeWebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanindeWebServer.Controllers
{
    /// <summary>
    /// Consulta de CEP para preencher o endereço antes de cadastrar a pessoa.
    /// Procura no banco (tblcep) e, se não achar, no ViaCEP. NÃO grava nada: o CEP só é
    /// cadastrado quando uma pessoa é criada ou editada.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAccess")]
    public class CepController : ControllerBase
    {
        private readonly ICepService _cepService;
        private readonly ILogger<CepController> _logger;

        public CepController(ICepService cepService, ILogger<CepController> logger)
        {
            _cepService = cepService;
            _logger = logger;
        }

        /// <summary>GET /api/Cep/30130-010 ou /api/Cep/30130010</summary>
        [HttpGet("{cep}")]
        [ProducesResponseType(typeof(CepResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get(string cep, CancellationToken ct)
        {
            var digitos = new string(cep.Where(char.IsDigit).ToArray());
            if (digitos.Length != 8)
                return BadRequest(new { Message = "CEP deve ter 8 dígitos (00000-000)." });

            try
            {
                var resultado = await _cepService.ConsultarAsync($"{digitos[..5]}-{digitos[5..]}", ct);
                return resultado == null
                    ? NotFound(new { Message = "CEP não encontrado." })
                    : Ok(resultado);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                var codigo = HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Erro ao consultar CEP. Código {Codigo}", codigo);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "Erro interno do servidor.", Codigo = codigo });
            }
        }
    }
}
