using CanindePwaShared.Models.Dto.TableDto;
using CanindeWebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanindeWebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // leitura/gravação requer usuário autenticado

    public class PessoaEmailController : ControllerBase
    {
        private readonly IPessoaEmailService _service;
        private readonly ILogger<PessoaEmailController> _logger;

        public PessoaEmailController(IPessoaEmailService service, ILogger<PessoaEmailController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("{idPessoa:int}")]
        public async Task<IActionResult> GetAll(int idPessoa)
        {
            var items = await _service.GetAllAsync(idPessoa);
            return Ok(items);
        }

        [HttpGet("email/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TblPessoaEmailRequestDto dto)
        {
            int id = await _service.CreateAsync(dto);
            return Ok(new { Message = "E-mail cadastrado com sucesso.", ID_PessoaEmail = id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TblPessoaEmailRequestDto dto)
        {
            dto.ID_PessoaEmail = id;
            await _service.UpdateAsync(dto);
            return Ok(new { Message = "E-mail atualizado com sucesso." });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { Message = "E-mail excluído com sucesso." });
        }
    }
}
