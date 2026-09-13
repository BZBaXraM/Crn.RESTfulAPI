namespace Domain.Exceptions;

public class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException(string message = "Invalid credentials.") : base(message)
    {
    }
}
