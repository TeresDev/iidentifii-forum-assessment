namespace Forum.Application.Abstractions;

/// <summary>
/// Takes the stored hash rather than the user, so the caller can verify against a
/// throwaway hash when no user was found without inventing a fake user to pass in.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}
