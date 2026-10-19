using Microsoft.Extensions.Time.Testing;
using TodoApi.Data;
using TodoApi.Services;

namespace TodoApi.Tests.Unit;

public sealed class TodoServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 30, 0, TimeSpan.Zero);

    private readonly InMemoryTodoRepository _repository = new();
    private readonly TodoService _service;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public TodoServiceTests()
    {
        _service = new TodoService(_repository, new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task CreateAsync_PersistsItemWithIdAndTimestamp()
    {
        var created = await _service.CreateAsync("Buy milk", _ct);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Buy milk", created.Title);
        Assert.Equal(Now, created.CreatedAt);
        Assert.Equal(created, await _repository.GetByIdAsync(created.Id, _ct));
    }

    [Fact]
    public async Task CreateAsync_TrimsTitle()
    {
        var created = await _service.CreateAsync("  Buy milk  ", _ct);

        Assert.Equal("Buy milk", created.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithBlankTitle_Throws(string title)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(title, _ct));
    }

    [Fact]
    public async Task CreateAsync_GeneratesUniqueIds()
    {
        var first = await _service.CreateAsync("One", _ct);
        var second = await _service.CreateAsync("Two", _ct);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCreatedItems()
    {
        var first = await _service.CreateAsync("One", _ct);
        var second = await _service.CreateAsync("Two", _ct);

        var items = await _service.GetAllAsync(_ct);

        Assert.Equal(new[] { first.Id, second.Id }.Order(), items.Select(i => i.Id).Order());
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var created = await _service.CreateAsync("Buy milk", _ct);

        Assert.True(await _service.DeleteAsync(created.Id, _ct));
        Assert.Empty(await _service.GetAllAsync(_ct));
    }
}
