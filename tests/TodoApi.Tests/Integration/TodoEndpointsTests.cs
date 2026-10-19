using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi.Contracts;

namespace TodoApi.Tests.Integration;

/// <summary>
/// Exercises the API end to end over HTTP. A new host is created per test so the
/// singleton in-memory store never leaks state between tests.
/// </summary>
public sealed class TodoEndpointsTests : IAsyncDisposable
{
    private const string TodosUrl = "/api/todos";
    private const string FrontendOrigin = "http://localhost:4200";

    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public TodoEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetTodos_WhenEmpty_ReturnsEmptyArray()
    {
        var todos = await _client.GetFromJsonAsync<TodoResponse[]>(TodosUrl, _ct);

        Assert.NotNull(todos);
        Assert.Empty(todos);
    }

    [Fact]
    public async Task PostTodo_ReturnsCreatedWithLocationAndBody()
    {
        var response = await _client.PostAsJsonAsync(TodosUrl, new CreateTodoRequest("Buy milk"), _ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TodoResponse>(_ct);
        Assert.NotNull(created);
        Assert.Equal("Buy milk", created.Title);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal($"{TodosUrl}/{created.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task PostTodo_ThenGetTodos_ContainsNewItem()
    {
        var created = await CreateTodoAsync("Buy milk");

        var todos = await _client.GetFromJsonAsync<TodoResponse[]>(TodosUrl, _ct);

        Assert.NotNull(todos);
        Assert.Equal([created], todos);
    }

    [Fact]
    public async Task GetTodoById_ReturnsItem()
    {
        var created = await CreateTodoAsync("Buy milk");

        var fetched = await _client.GetFromJsonAsync<TodoResponse>($"{TodosUrl}/{created.Id}", _ct);

        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task GetTodoById_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"{TodosUrl}/{Guid.NewGuid()}", _ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostTodo_WithBlankTitle_ReturnsValidationProblem(string title)
    {
        var response = await _client.PostAsJsonAsync(TodosUrl, new CreateTodoRequest(title), _ct);

        await AssertValidationProblemForTitleAsync(response);
    }

    [Fact]
    public async Task PostTodo_WithTooLongTitle_ReturnsValidationProblem()
    {
        var title = new string('x', CreateTodoRequest.TitleMaxLength + 1);

        var response = await _client.PostAsJsonAsync(TodosUrl, new CreateTodoRequest(title), _ct);

        await AssertValidationProblemForTitleAsync(response);
    }

    [Fact]
    public async Task PostTodo_WithMissingTitle_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(TodosUrl, new { }, _ct);

        await AssertValidationProblemForTitleAsync(response);
    }

    [Fact]
    public async Task PostTodo_WithMalformedJson_ReturnsBadRequest()
    {
        using var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(TodosUrl, content, _ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_RemovesItem()
    {
        var created = await CreateTodoAsync("Buy milk");

        var response = await _client.DeleteAsync($"{TodosUrl}/{created.Id}", _ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var todos = await _client.GetFromJsonAsync<TodoResponse[]>(TodosUrl, _ct);
        Assert.NotNull(todos);
        Assert.Empty(todos);
    }

    [Fact]
    public async Task DeleteTodo_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"{TodosUrl}/{Guid.NewGuid()}", _ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_OnlyRemovesTargetedItem()
    {
        var keep = await CreateTodoAsync("Keep me");
        var remove = await CreateTodoAsync("Remove me");

        await _client.DeleteAsync($"{TodosUrl}/{remove.Id}", _ct);

        var todos = await _client.GetFromJsonAsync<TodoResponse[]>(TodosUrl, _ct);
        Assert.NotNull(todos);
        Assert.Equal([keep], todos);
    }

    [Fact]
    public async Task Preflight_FromFrontendOrigin_IsAllowed()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, TodosUrl);
        request.Headers.Add("Origin", FrontendOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request, _ct);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(FrontendOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Request_FromUnknownOrigin_HasNoCorsHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, TodosUrl);
        request.Headers.Add("Origin", "https://evil.example");

        var response = await _client.SendAsync(request, _ct);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health", _ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<TodoResponse> CreateTodoAsync(string title)
    {
        var response = await _client.PostAsJsonAsync(TodosUrl, new CreateTodoRequest(title), _ct);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<TodoResponse>(_ct);
        return created ?? throw new InvalidOperationException("API returned an empty body.");
    }

    private async Task AssertValidationProblemForTitleAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_ct);
        Assert.NotNull(problem);
        Assert.Contains(problem.Errors.Keys, key => key.Equals("Title", StringComparison.OrdinalIgnoreCase));
    }
}
