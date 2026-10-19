namespace TodoApi.Domain;

/// <summary>A single entry on the user's TODO list.</summary>
public sealed record TodoItem(Guid Id, string Title, DateTimeOffset CreatedAt);
