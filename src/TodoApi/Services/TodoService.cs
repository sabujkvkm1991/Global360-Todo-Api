using TodoApi.Data;
using TodoApi.Domain;

namespace TodoApi.Services;

public sealed class TodoService(ITodoRepository repository, TimeProvider timeProvider) : ITodoService
{
    public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);

    public async Task<TodoItem> CreateAsync(string title, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var item = new TodoItem(Guid.CreateVersion7(), title.Trim(), timeProvider.GetUtcNow());
        await repository.AddAsync(item, cancellationToken);
        return item;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, cancellationToken);
}
