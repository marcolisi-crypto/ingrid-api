using AIReception.Mvc.Data;
using AIReception.Mvc.Data.Entities;
using AIReception.Mvc.Models.Dms;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Services;

public class PartsManagementService
{
    private readonly IDbContextFactory<IngridDmsDbContext> _dbContextFactory;
    private readonly DmsCoreService _dmsCore;

    public PartsManagementService(IDbContextFactory<IngridDmsDbContext> dbContextFactory, DmsCoreService dmsCore)
    {
        _dbContextFactory = dbContextFactory;
        _dmsCore = dmsCore;
    }

    public IReadOnlyList<PartInventoryItemRecord> GetInventory(string? status = null)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var query = db.PartInventoryItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status.Trim());
        }

        return query
            .OrderBy(x => x.PartNumber)
            .AsEnumerable()
            .Select(MapInventoryItem)
            .ToList();
    }

    public IReadOnlyList<PartOrderRecord> GetPartOrders(Guid? repairOrderId = null)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var query = db.PartOrders.AsQueryable();

        if (repairOrderId.HasValue)
        {
            query = query.Where(x => x.RepairOrderId == repairOrderId.Value);
        }

        return query
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsEnumerable()
            .Select(MapPartOrder)
            .ToList();
    }

    public IReadOnlyList<PartReturnRecord> GetPartReturns(Guid? repairOrderId = null)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var query = db.PartReturns.AsQueryable();

        if (repairOrderId.HasValue)
        {
            query = query.Where(x => x.RepairOrderId == repairOrderId.Value);
        }

        return query
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsEnumerable()
            .Select(MapPartReturn)
            .ToList();
    }

    public PartInventoryItemRecord UpsertInventoryItem(CreatePartInventoryItemRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var partNumber = (request.PartNumber ?? "").Trim().ToUpperInvariant();
        var entity = db.PartInventoryItems.FirstOrDefault(x => x.PartNumber == partNumber);
        var isNew = entity == null;

        entity ??= new DmsPartInventoryItemEntity
        {
            Id = Guid.NewGuid(),
            PartNumber = partNumber,
            CreatedAtUtc = DateTime.UtcNow
        };

        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? entity.Description : request.Description.Trim();
        entity.Manufacturer = string.IsNullOrWhiteSpace(request.Manufacturer) ? entity.Manufacturer : request.Manufacturer.Trim();
        entity.SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? (string.IsNullOrWhiteSpace(entity.SourceType) ? "oem" : entity.SourceType) : request.SourceType.Trim().ToLowerInvariant();
        entity.BinLocation = string.IsNullOrWhiteSpace(request.BinLocation) ? entity.BinLocation : request.BinLocation.Trim().ToUpperInvariant();
        entity.QuantityOnHand = request.QuantityOnHand ?? entity.QuantityOnHand;
        entity.QuantityReserved = request.QuantityReserved ?? entity.QuantityReserved;
        entity.QuantityOnOrder = request.QuantityOnOrder ?? entity.QuantityOnOrder;
        entity.UnitCost = request.UnitCost ?? entity.UnitCost;
        entity.ListPrice = request.ListPrice ?? entity.ListPrice;
        entity.PricingMatrixCode = string.IsNullOrWhiteSpace(request.PricingMatrixCode) ? entity.PricingMatrixCode : request.PricingMatrixCode.Trim().ToUpperInvariant();
        entity.Status = string.IsNullOrWhiteSpace(request.Status) ? (string.IsNullOrWhiteSpace(entity.Status) ? "active" : entity.Status) : request.Status.Trim().ToLowerInvariant();
        entity.IsObsolete = request.IsObsolete ?? entity.IsObsolete;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (isNew)
        {
            db.PartInventoryItems.Add(entity);
        }

        db.SaveChanges();
        return MapInventoryItem(entity);
    }

    public PartOrderRecord CreatePartOrder(CreatePartOrderRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var partNumber = (request.PartNumber ?? "").Trim().ToUpperInvariant();

        var entity = new DmsPartOrderEntity
        {
            Id = Guid.NewGuid(),
            RepairOrderId = request.RepairOrderId,
            InventoryItemId = request.InventoryItemId,
            PartNumber = partNumber,
            Vendor = string.IsNullOrWhiteSpace(request.Vendor) ? "OEM" : request.Vendor.Trim(),
            OrderType = string.IsNullOrWhiteSpace(request.OrderType) ? "stock" : request.OrderType.Trim().ToLowerInvariant(),
            Quantity = request.Quantity.GetValueOrDefault(1m) <= 0 ? 1m : request.Quantity!.Value,
            UnitCost = request.UnitCost.GetValueOrDefault(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "ordered" : request.Status.Trim().ToLowerInvariant(),
            IsSpecialOrder = request.IsSpecialOrder ?? false,
            EtaAtUtc = request.EtaAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.PartOrders.Add(entity);

        var inventoryItem = ResolveInventoryItem(db, request.InventoryItemId, partNumber);
        if (inventoryItem != null)
        {
            inventoryItem.QuantityOnOrder += entity.Quantity;
            inventoryItem.UpdatedAtUtc = DateTime.UtcNow;
        }

        var repairOrder = request.RepairOrderId.HasValue ? db.RepairOrders.FirstOrDefault(x => x.Id == request.RepairOrderId.Value) : null;
        db.SaveChanges();

        if (repairOrder != null)
        {
            _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
            {
                CustomerId = repairOrder.CustomerId,
                VehicleId = repairOrder.VehicleId,
                EventType = "parts.order_created",
                Title = $"Parts order {entity.PartNumber}",
                Body = $"{entity.OrderType} {entity.Quantity:0.##} from {entity.Vendor}",
                Department = "parts",
                SourceSystem = "ingrid.parts",
                SourceId = entity.Id.ToString()
            });
        }

        return MapPartOrder(entity);
    }

    public PartReturnRecord CreatePartReturn(CreatePartReturnRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var partNumber = (request.PartNumber ?? "").Trim().ToUpperInvariant();
        var entity = new DmsPartReturnEntity
        {
            Id = Guid.NewGuid(),
            RepairOrderId = request.RepairOrderId,
            InventoryItemId = request.InventoryItemId,
            PartNumber = partNumber,
            Quantity = request.Quantity.GetValueOrDefault(),
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "core return" : request.Reason.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "requested" : request.Status.Trim().ToLowerInvariant(),
            IsObsolescence = request.IsObsolescence ?? false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.PartReturns.Add(entity);

        var inventoryItem = ResolveInventoryItem(db, request.InventoryItemId, partNumber);
        if (inventoryItem != null)
        {
            inventoryItem.QuantityOnHand = Math.Max(0m, inventoryItem.QuantityOnHand - entity.Quantity);
            inventoryItem.IsObsolete = inventoryItem.IsObsolete || entity.IsObsolescence;
            inventoryItem.UpdatedAtUtc = DateTime.UtcNow;
        }

        var repairOrder = request.RepairOrderId.HasValue ? db.RepairOrders.FirstOrDefault(x => x.Id == request.RepairOrderId.Value) : null;
        db.SaveChanges();

        if (repairOrder != null)
        {
            _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
            {
                CustomerId = repairOrder.CustomerId,
                VehicleId = repairOrder.VehicleId,
                EventType = "parts.return_created",
                Title = $"Part return {entity.PartNumber}",
                Body = $"{entity.Quantity:0.##} {entity.Reason}",
                Department = "parts",
                SourceSystem = "ingrid.parts",
                SourceId = entity.Id.ToString()
            });
        }

        return MapPartReturn(entity);
    }

    private static DmsPartInventoryItemEntity? ResolveInventoryItem(IngridDmsDbContext db, Guid? inventoryItemId, string partNumber)
    {
        if (inventoryItemId.HasValue)
        {
            return db.PartInventoryItems.FirstOrDefault(x => x.Id == inventoryItemId.Value);
        }

        return string.IsNullOrWhiteSpace(partNumber)
            ? null
            : db.PartInventoryItems.FirstOrDefault(x => x.PartNumber == partNumber);
    }

    private static PartInventoryItemRecord MapInventoryItem(DmsPartInventoryItemEntity entity)
    {
        return new PartInventoryItemRecord
        {
            Id = entity.Id,
            PartNumber = entity.PartNumber,
            Description = entity.Description,
            Manufacturer = entity.Manufacturer,
            SourceType = entity.SourceType,
            BinLocation = entity.BinLocation,
            QuantityOnHand = entity.QuantityOnHand,
            QuantityReserved = entity.QuantityReserved,
            QuantityOnOrder = entity.QuantityOnOrder,
            UnitCost = entity.UnitCost,
            ListPrice = entity.ListPrice,
            PricingMatrixCode = entity.PricingMatrixCode,
            Status = entity.Status,
            IsObsolete = entity.IsObsolete,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static PartOrderRecord MapPartOrder(DmsPartOrderEntity entity)
    {
        return new PartOrderRecord
        {
            Id = entity.Id,
            RepairOrderId = entity.RepairOrderId,
            InventoryItemId = entity.InventoryItemId,
            PartNumber = entity.PartNumber,
            Vendor = entity.Vendor,
            OrderType = entity.OrderType,
            Quantity = entity.Quantity,
            UnitCost = entity.UnitCost,
            Status = entity.Status,
            IsSpecialOrder = entity.IsSpecialOrder,
            EtaAtUtc = entity.EtaAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static PartReturnRecord MapPartReturn(DmsPartReturnEntity entity)
    {
        return new PartReturnRecord
        {
            Id = entity.Id,
            RepairOrderId = entity.RepairOrderId,
            InventoryItemId = entity.InventoryItemId,
            PartNumber = entity.PartNumber,
            Quantity = entity.Quantity,
            Reason = entity.Reason,
            Status = entity.Status,
            IsObsolescence = entity.IsObsolescence,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }
}
