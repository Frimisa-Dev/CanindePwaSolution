using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using FalcaoWebServer.Data;

namespace FalcaoWebServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // leitura/gravação requer usuário autenticado

    public class GenericCrudController : ControllerBase
    {
        private readonly FalcaoDbContext _db;
        private readonly ILogger<GenericCrudController> _logger;

        public GenericCrudController(FalcaoDbContext db, ILogger<GenericCrudController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET list (paginada simples)
        [HttpGet("{entity}")]
        public async Task<IActionResult> GetList(string entity, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(entity)) return BadRequest("entity required");

            // prefer view Uvw* for lists
            string viewName = entity.StartsWith("Uvw", StringComparison.OrdinalIgnoreCase)
                ? entity
                : "Uvw" + NormalizeShortName(entity);

            var entityType = FindTypeByName(viewName) ?? FindTypeByName(entity);
            if (entityType == null)
                return NotFound($"Entidade '{entity}' não encontrada.");

            try
            {
                var service = new Services.GenericCrudService(_db);
                var (items, total) = await service.GetAllAsync(entityType.Name, page, pageSize, null);
                return Ok(new { total, page, pageSize, items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetList failed for {Entity}", entity);
                return StatusCode(500, ex.Message);
            }
        }

        // GET by id
        [HttpGet("{entity}/{id}")]
        public async Task<IActionResult> GetById(string entity, int id)
        {
            var entityType = FindTypeByName(entity);
            if (entityType == null) return NotFound($"Entidade '{entity}' não encontrada.");

            try
            {
                var service = new Services.GenericCrudService(_db);
                var found = await service.GetByIdAsync(entity, id);
                if (found == null) return NotFound();
                return Ok(found);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetById failed for {Entity} {Id}", entity, id);
                return StatusCode(500, ex.Message);
            }
        }

        // POST create
        [HttpPost("{entity}")]
        public async Task<IActionResult> Create(string entity)
        {
            var entityType = FindTypeByName(entity);
            if (entityType == null) return NotFound($"Entidade '{entity}' não encontrada.");

            using var sr = new System.IO.StreamReader(Request.Body);
            var body = await sr.ReadToEndAsync();

            object toSave;
            var dtoType = FindDtoTypeFor(entityType);
            if (dtoType != null)
            {
                var dto = JsonSerializer.Deserialize(body, dtoType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return BadRequest("Falha na desserialização do DTO.");
                toSave = MapDtoToEntity(dto, entityType);
            }
            else
            {
                toSave = JsonSerializer.Deserialize(body, entityType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (toSave == null) return BadRequest("Falha na desserialização da entidade.");
            }

            // zero-out PK int
            var pkProp = GetPrimaryKeyProperty(entityType);
            if (pkProp != null && pkProp.PropertyType == typeof(int))
                pkProp.SetValue(toSave, 0);

            try
            {
                var service = new Services.GenericCrudService(_db);
                var added = await service.AddAsync(entityType.Name, toSave);
                var id = GetPrimaryKeyValue(added, entityType);
                return CreatedAtAction(nameof(GetById), new { entity = entity, id = id }, added);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create failed for {Entity}", entity);
                return StatusCode(500, ex.Message);
            }
        }

        // PUT update
        [HttpPut("{entity}/{id}")]
        public async Task<IActionResult> Update(string entity, int id)
        {
            var entityType = FindTypeByName(entity);
            if (entityType == null) return NotFound($"Entidade '{entity}' não encontrada.");

            using var sr = new System.IO.StreamReader(Request.Body);
            var body = await sr.ReadToEndAsync();

            object incoming;
            var dtoType = FindDtoTypeFor(entityType);
            if (dtoType != null)
            {
                var dto = JsonSerializer.Deserialize(body, dtoType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return BadRequest("Falha na desserialização do DTO.");
                incoming = MapDtoToEntity(dto, entityType);
            }
            else
            {
                incoming = JsonSerializer.Deserialize(body, entityType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (incoming == null) return BadRequest("Falha na desserialização da entidade.");
            }

            try
            {
                var service = new Services.GenericCrudService(_db);
                var updated = await service.UpdateAsync(entityType.Name, id, incoming);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update failed for {Entity} {Id}", entity, id);
                return StatusCode(500, ex.Message);
            }
        }

        // DELETE (apenas Admin)
        [HttpDelete("{entity}/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string entity, int id)
        {
            try
            {
                var service = new Services.GenericCrudService(_db);
                var ok = await service.DeleteAsync(entity, id);
                if (!ok) return NotFound();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete failed for {Entity} {Id}", entity, id);
                return StatusCode(500, ex.Message);
            }
        }

        #region Helpers

        private static string NormalizeShortName(string entity)
        {
            if (entity.StartsWith("Tbl", StringComparison.OrdinalIgnoreCase))
                return entity.Substring(3);
            return entity;
        }

        private Type? FindTypeByName(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                try
                {
                    var t = asm.GetTypes().FirstOrDefault(x => string.Equals(x.Name, typeName, StringComparison.OrdinalIgnoreCase));
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private Type? FindDtoTypeFor(Type entityType)
        {
            var namesToTry = new List<string>
            {
                entityType.Name + "Dto",
                (entityType.Name.StartsWith("Tbl") ? entityType.Name.Substring(3) : entityType.Name) + "Dto"
            };

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                foreach (var n in namesToTry)
                {
                    var t = asm.GetTypes().FirstOrDefault(x => string.Equals(x.Name, n, StringComparison.OrdinalIgnoreCase));
                    if (t != null) return t;
                }
            }
            return null;
        }

        private object MapDtoToEntity(object dto, Type entityType)
        {
            var dtoType = dto.GetType();
            var entity = Activator.CreateInstance(entityType) ?? throw new InvalidOperationException("Não foi possível criar entidade.");
            foreach (var dp in dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var ep = entityType.GetProperty(dp.Name, BindingFlags.Public | BindingFlags.Instance);
                if (ep == null) continue;
                var val = dp.GetValue(dto);
                if (val == null) continue;
                try
                {
                    var targetVal = ConvertIfNeeded(val, ep.PropertyType);
                    ep.SetValue(entity, targetVal);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Falha ao mapear propriedade {Prop}: {Ex}", dp.Name, ex.Message);
                }
            }
            return entity;
        }

        private object? ConvertIfNeeded(object value, Type targetType)
        {
            if (value == null) return null;
            var vType = value.GetType();
            if (targetType.IsAssignableFrom(vType)) return value;
            try
            {
                if (targetType.IsEnum && value is string sVal)
                    return Enum.Parse(targetType, sVal, true);

                return Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
            }
            catch
            {
                return value;
            }
        }

        private PropertyInfo? GetPrimaryKeyProperty(Type t)
        {
            var pk = t.GetProperties().FirstOrDefault(p => p.Name.StartsWith("ID_", StringComparison.OrdinalIgnoreCase));
            return pk;
        }

        private int GetPrimaryKeyValue(object entity, Type t)
        {
            var pk = GetPrimaryKeyProperty(t);
            if (pk == null) return 0;
            var val = pk.GetValue(entity);
            if (val is int i) return i;
            if (val is long l) return (int)l;
            return 0;
        }

        #endregion
    }
}
