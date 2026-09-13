namespace Application.DTOs;

public sealed record ProductDto(
    int Id,
    string ProductName,
    string CreatedBy,
    DateTime CreatedOn,
    string? ModifiedBy,
    DateTime? ModifiedOn);
