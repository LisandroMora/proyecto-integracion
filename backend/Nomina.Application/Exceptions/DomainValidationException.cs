namespace Nomina.Application.Exceptions;

public class DomainValidationException : Exception
{
    public int Status { get; }

    public DomainValidationException(string message, int status = 400) : base(message)
    {
        Status = status;
    }
}
