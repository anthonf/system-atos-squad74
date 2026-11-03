namespace SupportAtos.Application.Exceptions;

public class ValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new[] { message };
    }

    public ValidationException(IEnumerable<string> errors) : base("Um ou mais erros de validação ocorreram.")
    {
        Errors = errors.ToList().AsReadOnly();
    }
}