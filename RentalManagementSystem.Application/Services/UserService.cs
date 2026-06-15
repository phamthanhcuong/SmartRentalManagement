using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork uow, ILogger<UserService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        try
        {
            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null) return null;
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
            user.LastLoginAt = DateTime.Now;
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", username);
            return null;
        }
    }

    public async Task<User?> GetByIdAsync(int id) => await _uow.Users.GetByIdAsync(id);

    public async Task<IEnumerable<User>> GetAllAsync() => await _uow.Users.GetAllAsync();

    public async Task<bool> CreateAsync(User user, string password)
    {
        try
        {
            var existing = await _uow.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
            if (existing != null) return false;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.Now;
            await _uow.Users.AddAsync(user);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}", user.Username);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(User user)
    {
        try
        {
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Id}", user.Id);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return false;
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash)) return false;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {Id}", userId);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user == null) return false;
            _uow.Users.Remove(user);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {Id}", id);
            return false;
        }
    }
}
