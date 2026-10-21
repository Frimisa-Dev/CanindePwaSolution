using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;
using System.Reflection;

namespace FalcaoWebServer.Swagger
{
    public class GenericCrudSwaggerOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            try
            {
                // Identifica o nome da entidade (ex: Produto, Bairro, etc.)
                var routeParameter = context.ApiDescription.RelativePath?.Split('/')?.LastOrDefault();

                if (string.IsNullOrEmpty(routeParameter))
                    return;

                var dtoType = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name.Equals($"{routeParameter}Dto", StringComparison.OrdinalIgnoreCase));

                if (dtoType == null)
                    return;

                // Remove possíveis duplicações
                if (operation.RequestBody?.Content != null)
                {
                    var distinctContent = operation.RequestBody.Content
                        .GroupBy(c => c.Key)
                        .ToDictionary(g => g.Key, g => g.First().Value);

                    operation.RequestBody.Content.Clear();

                    foreach (var kv in distinctContent)
                        operation.RequestBody.Content[kv.Key] = kv.Value;
                }

                // Ajusta o schema do corpo do request
                if (operation.RequestBody != null)
                {
                    var schema = context.SchemaGenerator.GenerateSchema(dtoType, context.SchemaRepository);

                    operation.RequestBody.Content["application/json"] = new OpenApiMediaType
                    {
                        Schema = schema
                    };
                }

                // Ajusta as respostas
                foreach (var response in operation.Responses.Values)
                {
                    if (response.Content.ContainsKey("application/json"))
                        continue;

                    response.Content.Add("application/json", new OpenApiMediaType());
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
