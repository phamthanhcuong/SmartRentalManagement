using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Services;

public class UtilityService : IUtilityService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UtilityService> _logger;

    public UtilityService(IUnitOfWork uow, ILogger<UtilityService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IEnumerable<UtilityReading>> GetByRoomAsync(int roomId) =>
        await _uow.UtilityReadings.Query()
            .Include(u => u.Room)
            .Where(u => u.RoomId == roomId)
            .OrderByDescending(u => u.Year).ThenByDescending(u => u.Month)
            .ToListAsync();

    public async Task<UtilityReading?> GetByRoomMonthYearAsync(int roomId, int month, int year) =>
        await _uow.UtilityReadings.FirstOrDefaultAsync(u => u.RoomId == roomId && u.Month == month && u.Year == year);

    public async Task<UtilityReading?> GetPreviousReadingAsync(int roomId, int month, int year) =>
        await _uow.UtilityReadings.Query()
            .Where(u => u.RoomId == roomId && (u.Year < year || (u.Year == year && u.Month < month)))
            .OrderByDescending(u => u.Year).ThenByDescending(u => u.Month)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<UtilityBatchRow>> GetBatchEntryAsync(int month, int year)
    {
        // Các phòng đang có hợp đồng hiệu lực
        var activeContracts = await _uow.Contracts.Query()
            .Include(c => c.Room).ThenInclude(r => r.RentalArea)
            .Where(c => c.Status == Domain.Enums.ContractStatus.Active)
            .ToListAsync();

        var rows = new List<UtilityBatchRow>();
        foreach (var c in activeContracts.DistinctBy(c => c.RoomId).OrderBy(c => c.Room.RoomCode))
        {
            var room = c.Room;
            var area = room.RentalArea;
            var existing = await GetByRoomMonthYearAsync(room.Id, month, year);
            var prev = await GetPreviousReadingAsync(room.Id, month, year);

            var electricOld = existing?.ElectricOld ?? prev?.ElectricNew ?? 0;
            var waterOld = existing?.WaterOld ?? prev?.WaterNew ?? 0;

            rows.Add(new UtilityBatchRow(
                room.Id, room.RoomCode, area?.AreaName ?? "",
                electricOld, waterOld,
                existing?.ElectricNew ?? electricOld,
                existing?.WaterNew ?? waterOld,
                existing?.ElectricPrice ?? area?.ElectricPrice ?? 3500,
                existing?.WaterPrice ?? area?.WaterPrice ?? 15000,
                existing?.Id
            ));
        }
        return rows;
    }

    public async Task<int> SaveBatchAsync(int month, int year, IEnumerable<UtilityReading> readings)
    {
        int saved = 0;
        foreach (var r in readings)
        {
            // Bỏ qua dòng chưa nhập chỉ số mới (bằng chỉ số cũ) để tránh tạo bản ghi rỗng
            if (r.ElectricNew == r.ElectricOld && r.WaterNew == r.WaterOld) continue;
            if (r.ElectricNew < r.ElectricOld || r.WaterNew < r.WaterOld) continue;

            var existing = await GetByRoomMonthYearAsync(r.RoomId, month, year);
            if (existing != null)
            {
                existing.ElectricOld = r.ElectricOld;
                existing.ElectricNew = r.ElectricNew;
                existing.WaterOld = r.WaterOld;
                existing.WaterNew = r.WaterNew;
                existing.ElectricPrice = r.ElectricPrice;
                existing.WaterPrice = r.WaterPrice;
                _uow.UtilityReadings.Update(existing);
            }
            else
            {
                r.Month = month;
                r.Year = year;
                r.CreatedAt = DateTime.Now;
                await _uow.UtilityReadings.AddAsync(r);
            }
            saved++;
        }
        if (saved > 0) await _uow.SaveChangesAsync();
        return saved;
    }

    public async Task<bool> CreateAsync(UtilityReading reading)
    {
        try
        {
            // Ràng buộc nghiệp vụ: chỉ số mới không được nhỏ hơn chỉ số cũ
            if (reading.ElectricNew < reading.ElectricOld || reading.WaterNew < reading.WaterOld)
            {
                _logger.LogWarning("Chỉ số mới nhỏ hơn chỉ số cũ cho phòng {RoomId}", reading.RoomId);
                return false;
            }

            // Tự kế thừa chỉ số đầu kỳ từ chỉ số cuối kỳ trước (nếu chưa nhập)
            if (reading.ElectricOld == 0 && reading.WaterOld == 0)
            {
                var prev = await GetPreviousReadingAsync(reading.RoomId, reading.Month, reading.Year);
                if (prev != null)
                {
                    reading.ElectricOld = prev.ElectricNew;
                    reading.WaterOld = prev.WaterNew;
                }
            }

            reading.CreatedAt = DateTime.Now;
            await _uow.UtilityReadings.AddAsync(reading);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating utility reading for room {RoomId}", reading.RoomId);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(UtilityReading reading)
    {
        try
        {
            _uow.UtilityReadings.Update(reading);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating utility reading {Id}", reading.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var reading = await _uow.UtilityReadings.GetByIdAsync(id);
            if (reading == null) return false;
            _uow.UtilityReadings.Remove(reading);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting utility reading {Id}", id);
            return false;
        }
    }

    public async Task<IEnumerable<UtilityReading>> GetByMonthYearAsync(int month, int year) =>
        await _uow.UtilityReadings.Query()
            .Include(u => u.Room)
            .Where(u => u.Month == month && u.Year == year)
            .ToListAsync();
}
