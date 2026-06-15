using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Services;

public class AssetService : IAssetService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AssetService> _logger;

    public AssetService(IUnitOfWork uow, ILogger<AssetService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IEnumerable<Asset>> GetAllAsync() =>
        await _uow.Assets.Query().OrderBy(a => a.AssetName).ToListAsync();

    public async Task<Asset?> GetByIdAsync(int id) => await _uow.Assets.GetByIdAsync(id);

    public async Task<bool> CreateAsync(Asset asset)
    {
        try
        {
            asset.CreatedAt = DateTime.Now;
            await _uow.Assets.AddAsync(asset);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Asset asset)
    {
        try
        {
            _uow.Assets.Update(asset);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset {Id}", asset.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var asset = await _uow.Assets.GetByIdAsync(id);
            if (asset == null) return false;
            _uow.Assets.Remove(asset);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset {Id}", id);
            return false;
        }
    }

    public async Task<IEnumerable<RoomAsset>> GetRoomAssetsAsync(int roomId) =>
        await _uow.RoomAssets.Query()
            .Include(ra => ra.Asset)
            .Include(ra => ra.Room)
            .Where(ra => ra.RoomId == roomId)
            .ToListAsync();

    public async Task<bool> AssignToRoomAsync(RoomAsset roomAsset)
    {
        try
        {
            roomAsset.CreatedAt = DateTime.Now;
            await _uow.RoomAssets.AddAsync(roomAsset);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning asset to room");
            return false;
        }
    }

    public async Task<bool> RemoveFromRoomAsync(int roomAssetId)
    {
        try
        {
            var ra = await _uow.RoomAssets.GetByIdAsync(roomAssetId);
            if (ra == null) return false;
            _uow.RoomAssets.Remove(ra);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing asset from room {Id}", roomAssetId);
            return false;
        }
    }
}
