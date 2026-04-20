using AIReception.Mvc.Data;
using AIReception.Mvc.Data.Entities;
using AIReception.Mvc.Models.Dms;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Services;

public class MediaAssetService
{
    private readonly IDbContextFactory<IngridDmsDbContext> _dbContextFactory;
    private readonly DmsCoreService _dmsCore;

    public MediaAssetService(IDbContextFactory<IngridDmsDbContext> dbContextFactory, DmsCoreService dmsCore)
    {
        _dbContextFactory = dbContextFactory;
        _dmsCore = dmsCore;
    }

    public IReadOnlyList<MediaAssetRecord> GetMediaAssets(
        Guid? customerId = null,
        Guid? vehicleId = null,
        Guid? repairOrderId = null,
        string? contextType = null)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var query = db.MediaAssets.AsQueryable();

        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (vehicleId.HasValue) query = query.Where(x => x.VehicleId == vehicleId.Value);
        if (repairOrderId.HasValue) query = query.Where(x => x.RepairOrderId == repairOrderId.Value);
        if (!string.IsNullOrWhiteSpace(contextType)) query = query.Where(x => x.ContextType == contextType.Trim().ToLowerInvariant());

        return query
            .OrderByDescending(x => x.CapturedAtUtc)
            .AsEnumerable()
            .Select(MapMediaAsset)
            .ToList();
    }

    public MediaAssetRecord CreateMediaAsset(CreateMediaAssetRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var contextType = string.IsNullOrWhiteSpace(request.ContextType) ? "vin_archive" : request.ContextType.Trim().ToLowerInvariant();
        var mediaType = string.IsNullOrWhiteSpace(request.MediaType) ? "photo" : request.MediaType.Trim().ToLowerInvariant();

        var asset = new DmsMediaAssetEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            VehicleId = request.VehicleId,
            RepairOrderId = request.RepairOrderId,
            NoteId = request.NoteId,
            ContextType = contextType,
            MediaType = mediaType,
            StorageUrl = (request.StorageUrl ?? "").Trim(),
            ThumbnailUrl = (request.ThumbnailUrl ?? "").Trim(),
            FileName = (request.FileName ?? "").Trim(),
            Caption = (request.Caption ?? "").Trim(),
            CapturedBy = (request.CapturedBy ?? "").Trim(),
            Visibility = string.IsNullOrWhiteSpace(request.Visibility) ? "internal" : request.Visibility.Trim().ToLowerInvariant(),
            CapturedAtUtc = request.CapturedAtUtc ?? DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        if (asset.NoteId.HasValue)
        {
            var note = db.Notes.FirstOrDefault(x => x.Id == asset.NoteId.Value);
            if (note != null)
            {
                asset.CustomerId ??= note.CustomerId;
                asset.VehicleId ??= note.VehicleId;
            }
        }

        if (asset.RepairOrderId.HasValue)
        {
            var repairOrder = db.RepairOrders.FirstOrDefault(x => x.Id == asset.RepairOrderId.Value);
            if (repairOrder != null)
            {
                asset.CustomerId ??= repairOrder.CustomerId;
                asset.VehicleId ??= repairOrder.VehicleId;
            }
        }

        db.MediaAssets.Add(asset);
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = asset.CustomerId,
            VehicleId = asset.VehicleId,
            EventType = InferEventType(asset),
            Title = InferTitle(asset),
            Body = BuildBody(asset),
            Department = InferDepartment(asset),
            SourceSystem = "ingrid.media",
            SourceId = asset.Id.ToString(),
            OccurredAtUtc = asset.CapturedAtUtc
        });

        return MapMediaAsset(asset);
    }

    private static string InferEventType(DmsMediaAssetEntity asset)
    {
        if (asset.ContextType == "vin_archive")
        {
            return "vin_archive_media";
        }

        if (asset.ContextType == "repair_order")
        {
            return asset.MediaType == "video" ? "repair_order.video_added" : "repair_order.photo_added";
        }

        return asset.MediaType == "video" ? "media.video_added" : "media.photo_added";
    }

    private static string InferTitle(DmsMediaAssetEntity asset)
    {
        if (asset.ContextType == "vin_archive")
        {
            return asset.MediaType == "video" ? "VIN archive video" : "VIN archive photo";
        }

        if (asset.ContextType == "repair_order")
        {
            return asset.MediaType == "video" ? "RO video added" : "RO photo added";
        }

        return asset.MediaType == "video" ? "Internal video added" : "Internal photo added";
    }

    private static string InferDepartment(DmsMediaAssetEntity asset)
    {
        return asset.ContextType == "repair_order" ? "technicians" : "service";
    }

    private static string BuildBody(DmsMediaAssetEntity asset)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(asset.Caption)) parts.Add(asset.Caption);
        if (!string.IsNullOrWhiteSpace(asset.FileName)) parts.Add(asset.FileName);
        if (!string.IsNullOrWhiteSpace(asset.CapturedBy)) parts.Add($"Captured by {asset.CapturedBy}");
        if (!string.IsNullOrWhiteSpace(asset.StorageUrl)) parts.Add(asset.StorageUrl);
        return string.Join(" • ", parts);
    }

    private static MediaAssetRecord MapMediaAsset(DmsMediaAssetEntity asset)
    {
        return new MediaAssetRecord
        {
            Id = asset.Id,
            CustomerId = asset.CustomerId,
            VehicleId = asset.VehicleId,
            RepairOrderId = asset.RepairOrderId,
            NoteId = asset.NoteId,
            ContextType = asset.ContextType,
            MediaType = asset.MediaType,
            StorageUrl = asset.StorageUrl,
            ThumbnailUrl = asset.ThumbnailUrl,
            FileName = asset.FileName,
            Caption = asset.Caption,
            CapturedBy = asset.CapturedBy,
            Visibility = asset.Visibility,
            CapturedAtUtc = asset.CapturedAtUtc,
            CreatedAtUtc = asset.CreatedAtUtc,
            UpdatedAtUtc = asset.UpdatedAtUtc
        };
    }
}
