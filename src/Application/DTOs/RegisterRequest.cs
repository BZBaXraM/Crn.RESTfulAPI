namespace Application.DTOs;

public sealed record RegisterRequest(string UserName, string Email, string Password);
