using Forum.Application.Contracts;
using Forum.Domain.Entities;

namespace Forum.Application.Abstractions;

public interface ITokenService
{
    AccessToken Issue(User user);
}
