using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Models;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? Error { get; }
    public List<string> Errors { get; }

    private Result(bool isSuccess, T? data, string? error, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        Errors = errors ?? new List<string>();
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> Failure(List<string> errors) => new(false, default, null, errors);
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public record UserDto
{
    public string Id { get; init; }
    public string FullName { get; init; }
    public string Email { get; init; }
    public string Role { get; init; }
    public bool EmailVerified { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public DateTime DateOfBirth { get; init; }
    public string Country { get; init; }
    public DateTime CreatedAt { get; init; }

    // Optional fields
    public string? Expertise { get; init; }
    public string? PortfolioUrl { get; init; }
    public string? CvLink { get; init; }
}
