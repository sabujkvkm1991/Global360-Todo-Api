using TodoApi.Domain;

namespace TodoApi.Contracts;

/// <summary>A TODO item as returned by the API.</summary>
public sealed record TodoResponse(Guid Id, string Title, DateTimeOffset CreatedAt)
{
    public static TodoResponse FromDomain(TodoItem item) => new(item.Id, item.Title, item.CreatedAt);
}
