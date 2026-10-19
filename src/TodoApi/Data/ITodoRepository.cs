using TodoApi.Domain;

namespace TodoApi.Data;

/// <summary>
/// Persistence abstraction for TODO items. Asynchronous so an in-memory store can later be
/// swapped for a real database without changing callers.
/// </summary>
public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TodoItem item, CancellationToken cancellationToken = default);

    /// <returns><c>true</c> if the item existed and was removed; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
