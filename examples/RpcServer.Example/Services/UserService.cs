namespace RpcServer.Example.Services;

public interface IUserService
{
    UserDto? GetUser(int id);
    UserDto CreateUser(string name, string email);
    UserDto[] ListUsers();
}

public class UserService : IUserService
{
    private readonly Dictionary<int, UserDto> _users = new();
    private int _nextId = 1;

    public UserService()
    {
        // Add some sample users
        CreateUser("John Doe", "john@example.com");
        CreateUser("Jane Smith", "jane@example.com");
    }

    public UserDto? GetUser(int id)
    {
        return _users.TryGetValue(id, out var user) ? user : null;
    }

    public UserDto CreateUser(string name, string email)
    {
        var user = new UserDto(_nextId++, name, email, DateTime.UtcNow);
        _users[user.Id] = user;
        return user;
    }

    public UserDto[] ListUsers()
    {
        return _users.Values.ToArray();
    }
}

public record UserDto(int Id, string Name, string Email, DateTime CreatedAt);
