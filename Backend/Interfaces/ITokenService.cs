using TaskManagement.API.Entities;

namespace TaskManagement.API.Interfaces;

public interface ITokenService
{
    (string token, DateTime expiresAt) CreateToken(User user);
}