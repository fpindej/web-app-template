namespace MyProject.Application.Features.Authentication.Dtos;

public record RegisterInput(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? PhoneNumber
);
