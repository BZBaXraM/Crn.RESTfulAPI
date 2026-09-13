using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class UserMappings
{
    public static UserDto ToDto(this User user) => new(user.Id, user.UserName, user.Email, user.Role.ToString());
}
