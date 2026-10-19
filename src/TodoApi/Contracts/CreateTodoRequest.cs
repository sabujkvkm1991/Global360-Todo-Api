using System.ComponentModel.DataAnnotations;

namespace TodoApi.Contracts;

/// <summary>Payload for adding an item to the TODO list.</summary>
/// <param name="Title">What needs doing. Required, at most 200 characters.</param>
public sealed record CreateTodoRequest(
    [Required, StringLength(CreateTodoRequest.TitleMaxLength)] string Title)
{
    public const int TitleMaxLength = 200;
}
