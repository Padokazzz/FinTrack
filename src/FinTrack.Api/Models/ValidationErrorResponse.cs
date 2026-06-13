namespace FinTrack.Api.Models;

public class ValidationErrorResponse
{
    public string Message { get; set; } = "Validation failed.";

    public List<ValidationError> Errors { get; set; } = new();
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}