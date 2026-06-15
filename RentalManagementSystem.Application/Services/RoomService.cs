using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Application.Services;

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RoomService> _logger;

    public RoomService(IUnitOfWork uow, ILogger<RoomService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IEnumerable<Room>> GetAllAsync() =>
        await _uow.Rooms.Query().Include(r => r.RentalArea).OrderBy(r => r.RoomCode).ToListAsync();

    public async Task<IEnumerable<Room>> GetByAreaAsync(int areaId) =>
        await _uow.Rooms.Query().Include(r => r.RentalArea).Where(r => r.RentalAreaId == areaId).ToListAsync();

    public async Task<Room?> GetByIdAsync(int id) =>
        await _uow.Rooms.Query().Include(r => r.RentalArea).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Room>> SearchAsync(string keyword)
    {
        keyword = keyword.ToLower();
        return await _uow.Rooms.Query()
            .Include(r => r.RentalArea)
            .Where(r => r.RoomCode.ToLower().Contains(keyword) ||
                        r.RoomName.ToLower().Contains(keyword) ||
                        (r.Description != null && r.Description.ToLower().Contains(keyword)))
            .ToListAsync();
    }

    public async Task<bool> CreateAsync(Room room)
    {
        try
        {
            room.CreatedAt = DateTime.Now;
            await _uow.Rooms.AddAsync(room);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room {Code}", room.RoomCode);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Room room)
    {
        try
        {
            _uow.Rooms.Update(room);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {Id}", room.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var room = await _uow.Rooms.GetByIdAsync(id);
            if (room == null) return false;
            _uow.Rooms.Remove(room);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room {Id}", id);
            return false;
        }
    }

    public async Task<int> GetAvailableCountAsync() =>
        await _uow.Rooms.CountAsync(r => r.Status == RoomStatus.Available);

    public async Task<int> GetOccupiedCountAsync() =>
        await _uow.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
}
