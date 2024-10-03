using API.Contracts;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Middleware
{
    public class InternalServerErrorOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.SchemaRepository.Schemas.ContainsKey(nameof(ErrorResponse)))
            {
                context.SchemaRepository.Schemas.Add(
                    nameof(ErrorResponse),
                    context.SchemaGenerator.GenerateSchema(typeof(ErrorResponse), context.SchemaRepository));
            }

            var errorResponseSchema = context.SchemaRepository.Schemas[nameof(ErrorResponse)];

            operation.Responses.Add("500", new OpenApiResponse
            {
                Description = "Internal Server Error",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.Schema,
                                Id = nameof(ErrorResponse)
                            }
                        }
                    }
                }
            });
        }
    }
}
