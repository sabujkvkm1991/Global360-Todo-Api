using Microsoft.AspNetCore.Http.HttpResults;
using TodoApi.Contracts;
using TodoApi.Services;

namespace TodoApi.Endpoints;

public static class TodoEndpoints
{
    public const string GetTodoByIdRouteName = "GetTodoById";

    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos").WithTags("Todos");

        group.MapGet("/", GetAllAsync)
            .WithName("GetTodos")
            .WithSummary("List all TODO items, oldest first.");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName(GetTodoByIdRouteName)
            .WithSummary("Get a single TODO item.");

        group.MapPost("/", CreateAsync)
            .WithName("CreateTodo")
            .WithSummary("Add an item to the TODO list.")
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTodo")
            .WithSummary("Remove an item from the TODO list.");

        return app;
    }

    internal static async Task<Ok<TodoResponse[]>> GetAllAsync(
        ITodoService service, CancellationToken cancellationToken)
    {
        var items = await service.GetAllAsync(cancellationToken);
        return TypedResults.Ok(items.Select(TodoResponse.FromDomain).ToArray());
    }

    internal static async Task<Results<Ok<TodoResponse>, NotFound>> GetByIdAsync(
        Guid id, ITodoService service, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(id, cancellationToken);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(TodoResponse.FromDomain(item));
    }

    internal static async Task<CreatedAtRoute<TodoResponse>> CreateAsync(
        CreateTodoRequest request, ITodoService service, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(request.Title, cancellationToken);
        return TypedResults.CreatedAtRoute(
            TodoResponse.FromDomain(item), GetTodoByIdRouteName, new { id = item.Id });
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid id, ITodoService service, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
