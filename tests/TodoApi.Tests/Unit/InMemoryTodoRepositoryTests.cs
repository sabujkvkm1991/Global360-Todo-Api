using TodoApi.Data;
using TodoApi.Domain;

namespace TodoApi.Tests.Unit;

public sealed class InMemoryTodoRepositoryTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly InMemoryTodoRepository _repository = new();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var items = await _repository.GetAllAsync(_ct);

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsItemsOrderedByCreationTime()
    {
        var newer = new TodoItem(Guid.NewGuid(), "Second", BaseTime.AddMinutes(1));
        var older = new TodoItem(Guid.NewGuid(), "First", BaseTime);
        await _repository.AddAsync(newer, _ct);
        await _repository.AddAsync(older, _ct);

        var items = await _repository.GetAllAsync(_ct);

        Assert.Equal([older, newer], items);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAddedItem()
    {
        var item = new TodoItem(Guid.NewGuid(), "Buy milk", BaseTime);
        await _repository.AddAsync(item, _ct);

        var found = await _repository.GetByIdAsync(item.Id, _ct);

        Assert.Equal(item, found);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var found = await _repository.GetByIdAsync(Guid.NewGuid(), _ct);

        Assert.Null(found);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_Throws()
    {
        var item = new TodoItem(Guid.NewGuid(), "Buy milk", BaseTime);
        await _repository.AddAsync(item, _ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddAsync(item, _ct));
    }

    [Fact]
    public async Task DeleteAsync_WhenPresent_RemovesItemAndReturnsTrue()
    {
        var item = new TodoItem(Guid.NewGuid(), "Buy milk", BaseTime);
        await _repository.AddAsync(item, _ct);

        var deleted = await _repository.DeleteAsync(item.Id, _ct);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(item.Id, _ct));
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsFalse()
    {
        var deleted = await _repository.DeleteAsync(Guid.NewGuid(), _ct);

        Assert.False(deleted);
    }

    [Fact]
    public async Task ConcurrentAdds_AreAllStored()
    {
        const int count = 500;

        await Task.WhenAll(Enumerable.Range(0, count).Select(i =>
            Task.Run(() => _repository.AddAsync(new TodoItem(Guid.NewGuid(), $"Item {i}", BaseTime), _ct), _ct)));

        Assert.Equal(count, (await _repository.GetAllAsync(_ct)).Count);
    }
}
