using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EventArch.Api.Idempotency;

/// <summary>
/// Documents the Idempotency-Key header on <see cref="IdempotentAttribute"/> actions,
/// so it shows up as a field in Scalar and in generated clients.
/// </summary>
internal sealed class IdempotencyKeyOpenApiTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        bool isIdempotent = context.Description.ActionDescriptor.EndpointMetadata.OfType<IdempotentAttribute>().Any();
        if (!isIdempotent)
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = IdempotencyFilter.HeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Unique key per operation (e.g. a new GUID). Retrying with the same key returns the original response without moving money again.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, MaxLength = 100 }
        });

        return Task.CompletedTask;
    }
}
