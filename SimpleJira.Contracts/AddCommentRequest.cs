namespace SimpleJira.Contracts;

public record AddCommentRequest(
    string Body,
    Guid? AuthorId);
