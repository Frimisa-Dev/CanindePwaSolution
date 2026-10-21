using FalcaoWebServer.Data;
using FalcaoPwaShared.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FalcaoWebServer.Services;
using FalcaoPwaShared.Models.Dtos.AuthDto;
using FalcaoPwaShared.Models.Dtos.UserDto;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly FalcaoDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UsersController(FalcaoDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _db.Users.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        return Ok(u);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CadastroUsuarioDto dto)
    {
        if (dto == null) return BadRequest("Dados invalidos");

        if (await _db.Users.AnyAsync(x => x.Email == dto.Email))
            return BadRequest("Email ja cadastrado.");

        var user = new User
        {
            Username = dto.Username ?? dto.Email,
            Email = dto.Email,
            PasswordHash = _passwordHasher.HashPassword(dto.Password ?? ""),
            IsActive = true,
            TipoUsuario = dto.TipoUsuario.ToString()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        u.Username = dto.Username ?? u.Username;
        u.Email = dto.Email ?? u.Email;
        u.IsActive = dto.IsActive;

        _db.Users.Update(u);
        await _db.SaveChangesAsync();
        return Ok(u);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
