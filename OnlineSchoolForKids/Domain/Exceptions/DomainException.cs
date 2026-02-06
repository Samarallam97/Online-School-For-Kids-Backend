using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class InvalidEmailException : DomainException
{
    public InvalidEmailException(string email)
        : base($"Email '{email}' is invalid.") { }
}

public class InvalidPasswordException : DomainException
{
    public InvalidPasswordException(string reason)
        : base($"Password is invalid: {reason}") { }
}

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string identifier)
        : base($"User with identifier '{identifier}' was not found.") { }
}

public class DuplicateEmailException : DomainException
{
    public DuplicateEmailException(string email)
        : base($"Email '{email}' is already registered.") { }
}

public class InvalidTokenException : DomainException
{
    public InvalidTokenException(string message = "Token is invalid or expired.")
        : base(message) { }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Unauthorized access.")
        : base(message) { }
}
