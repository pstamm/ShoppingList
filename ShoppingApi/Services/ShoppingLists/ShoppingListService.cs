using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.Data;
using ShoppingApi.Domain.Entities;
using ShoppingApi.DTOs.ShoppingLists;

namespace ShoppingApi.Services.ShoppingLists;

public class ShoppingListService : IShoppingListService
{
    private readonly ApplicationDbContext _dbContext;

    public ShoppingListService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ShoppingListDto>> GetListsAsync(string userId, bool isAdmin)
    {
        var query = _dbContext.ShoppingLists
            .AsNoTracking();

        if (!isAdmin)
        {
            query = query.Where(list =>
                list.OwnerUserId == userId ||
                list.Shares.Any(share => share.UserId == userId));
        }

        var lists = await query
            .Include(list => list.ListProducts)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product!.ProductType)
            .OrderBy(list => list.Name)
            .ToListAsync();

        return lists.Select(ToDto);
    }

    public async Task<ShoppingListDto?> GetListAsync(int listId, string userId, bool isAdmin)
    {
        var list = await _dbContext.ShoppingLists
            .AsNoTracking()
            .Include(item => item.ListProducts)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product!.ProductType)
            .FirstOrDefaultAsync(item => item.Id == listId);

        if (list is null)
        {
            return null;
        }

        await EnsureCanEditAsync(list, userId, isAdmin);
        return ToDto(list);
    }

    public async Task<ShoppingListDto> CreateListAsync(CreateShoppingListRequest request, string userId)
    {
        var name = NormalizeName(request.Name);
        var now = DateTimeOffset.UtcNow;
        var list = new ShoppingList
        {
            Name = name,
            OwnerUserId = userId,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedByUserId = userId,
            UpdatedAt = now
        };

        _dbContext.ShoppingLists.Add(list);
        await _dbContext.SaveChangesAsync();
        return ToDto(list);
    }

    public async Task<ShoppingListDto> UpdateListAsync(
        int listId,
        UpdateShoppingListRequest request,
        string userId,
        bool isAdmin)
    {
        var list = await FindListAsync(listId);
        EnsureCanManageListName(list, userId, isAdmin);
        var name = NormalizeName(request.Name);

        SetOriginalRowVersion(list, request.RowVersion);
        list.Name = name;
        list.UpdatedAt = DateTimeOffset.UtcNow;
        list.UpdatedByUserId = userId;
        await _dbContext.SaveChangesAsync();

        return ToDto(list);
    }

    public async Task DeleteListAsync(int listId, string rowVersion, string userId, bool isAdmin)
    {
        var list = await FindListAsync(listId);
        EnsureCanDelete(list, userId, isAdmin);

        var hasActiveItems = await _dbContext.ListProducts
            .AnyAsync(item => item.ListId == listId);
        if (hasActiveItems)
        {
            throw new InvalidOperationException("A list cannot be deleted while it contains active products.");
        }

        SetOriginalRowVersion(list, rowVersion);
        list.DeletedAt = DateTimeOffset.UtcNow;
        list.DeletedByUserId = userId;
        list.UpdatedAt = DateTimeOffset.UtcNow;
        list.UpdatedByUserId = userId;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ListProductDto>?> GetListProductsAsync(int listId, string userId, bool isAdmin)
    {
        var list = await FindListOrNullAsync(listId);
        if (list is null)
        {
            return null;
        }

        await EnsureCanEditAsync(list, userId, isAdmin);

        var items = await _dbContext.ListProducts
            .AsNoTracking()
            .Where(item => item.ListId == listId)
            .Include(item => item.Product)
            .ThenInclude(product => product!.ProductType)
            .OrderBy(item => item.Id)
            .ToListAsync();
        return items.Select(item => ToDto(item));
    }

    public async Task<ListProductDto> AddListProductAsync(
        int listId,
        AddListProductRequest request,
        string userId,
        bool isAdmin)
    {
        var list = await FindListAsync(listId);
        await EnsureCanEditAsync(list, userId, isAdmin);

        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(value => value.ProductType)
            .FirstOrDefaultAsync(value => value.Id == request.ProductId);
        if (product is null)
        {
            throw new KeyNotFoundException("The selected product was not found or is inactive.");
        }

        var duplicateExists = await _dbContext.ListProducts
            .AnyAsync(item =>
                item.ListId == listId &&
                item.ProductId == request.ProductId);
        if (duplicateExists)
        {
            throw new InvalidOperationException("This product is already present in the list.");
        }

        ValidateNotes(request.Notes);
        var now = DateTimeOffset.UtcNow;
        var item = new ListProduct
        {
            ListId = listId,
            ProductId = request.ProductId,
            TipicalOrder = request.TipicalOrder,
            ToOrderNow = request.ToOrderNow,
            Notes = NormalizeNotes(request.Notes),
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _dbContext.ListProducts.Add(item);
        await _dbContext.SaveChangesAsync();
        return ToDto(item, product);
    }

    public async Task<ListProductDto> UpdateListProductAsync(
        int listId,
        int listProductId,
        UpdateListProductRequest request,
        string userId,
        bool isAdmin)
    {
        var list = await FindListAsync(listId);
        await EnsureCanEditAsync(list, userId, isAdmin);

        var item = await _dbContext.ListProducts
            .Include(value => value.Product)
            .FirstOrDefaultAsync(value =>
                value.Id == listProductId &&
                value.ListId == listId);
        if (item is null)
        {
            throw new KeyNotFoundException("List product was not found.");
        }

        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(value => value.ProductType)
            .FirstOrDefaultAsync(value => value.Id == request.ProductId);
        if (product is null)
        {
            throw new KeyNotFoundException("The selected product was not found or is inactive.");
        }

        var duplicateExists = await _dbContext.ListProducts
            .AnyAsync(value =>
                value.Id != listProductId &&
                value.ListId == listId &&
                value.ProductId == request.ProductId);
        if (duplicateExists)
        {
            throw new InvalidOperationException("This product is already present in the list.");
        }

        ValidateNotes(request.Notes);
        SetOriginalRowVersion(item, request.RowVersion);
        item.ProductId = request.ProductId;
        item.TipicalOrder = request.TipicalOrder;
        item.ToOrderNow = request.ToOrderNow;
        item.Notes = NormalizeNotes(request.Notes);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByUserId = userId;
        await _dbContext.SaveChangesAsync();

        return ToDto(item, product);
    }

    public async Task DeleteListProductAsync(
        int listId,
        int listProductId,
        string rowVersion,
        string userId,
        bool isAdmin)
    {
        var list = await FindListAsync(listId);
        await EnsureCanEditAsync(list, userId, isAdmin);

        var item = await _dbContext.ListProducts
            .FirstOrDefaultAsync(value =>
                value.Id == listProductId &&
                value.ListId == listId);
        if (item is null)
        {
            throw new KeyNotFoundException("List product was not found.");
        }

        SetOriginalRowVersion(item, rowVersion);
        item.DeletedAt = DateTimeOffset.UtcNow;
        item.DeletedByUserId = userId;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByUserId = userId;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ListShareDto>> GetSharesAsync(int listId, string userId, bool isAdmin)
    {
        var list = await FindListAsync(listId);
        EnsureCanManageSharing(list, userId, isAdmin);

        return await _dbContext.ListShares
            .AsNoTracking()
            .Where(share => share.ListId == listId)
            .OrderBy(share => share.User!.Email)
            .Select(share => new ListShareDto(
                share.Id,
                share.ListId,
                share.UserId,
                share.User!.UserName ?? string.Empty,
                share.User!.Email ?? string.Empty,
                share.CreatedAt))
            .ToListAsync();
    }

    public async Task<ListShareDto> CreateShareAsync(
        int listId,
        ShareListRequest request,
        string userId,
        bool isAdmin)
    {
        var list = await FindListAsync(listId);
        EnsureCanManageSharing(list, userId, isAdmin);

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            throw new ValidationException("A username is required.");
        }

        var normalizedUserName = request.UserName.Trim().ToUpperInvariant();
        var targetUser = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.NormalizedUserName == normalizedUserName && user.IsActive)
            .Select(user => new { user.Id, user.UserName, user.Email })
            .FirstOrDefaultAsync();
        if (targetUser is null)
        {
            throw new KeyNotFoundException("The target user was not found or is inactive.");
        }

        var targetUserId = targetUser.Id;
        if (targetUserId == list.OwnerUserId)
        {
            throw new ValidationException("A list cannot be shared with its owner.");
        }

        var alreadyShared = await _dbContext.ListShares
            .AnyAsync(share =>
                share.ListId == listId &&
                share.UserId == targetUserId);
        if (alreadyShared)
        {
            throw new InvalidOperationException("This user already has access to the list.");
        }

        var share = new ListShare
        {
            ListId = listId,
            UserId = targetUser.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        _dbContext.ListShares.Add(share);
        await _dbContext.SaveChangesAsync();
        return new ListShareDto(
            share.Id,
            share.ListId,
            share.UserId,
            targetUser.UserName ?? string.Empty,
            targetUser.Email ?? string.Empty,
            share.CreatedAt);
    }

    public async Task RemoveShareAsync(int listId, string sharedUserId, string userId, bool isAdmin)
    {
        var list = await FindListAsync(listId);
        EnsureCanManageSharing(list, userId, isAdmin);

        if (sharedUserId == list.OwnerUserId)
        {
            throw new ValidationException("The list owner cannot be removed.");
        }

        var share = await _dbContext.ListShares.FirstOrDefaultAsync(item =>
            item.ListId == listId &&
            item.UserId == sharedUserId);
        if (share is null)
        {
            throw new KeyNotFoundException("The active share was not found.");
        }

        share.DeletedAt = DateTimeOffset.UtcNow;
        share.DeletedByUserId = userId;
        await _dbContext.SaveChangesAsync();
    }

    private async Task<ShoppingList> FindListAsync(int listId)
    {
        var list = await FindListOrNullAsync(listId);
        return list ?? throw new KeyNotFoundException("Shopping list was not found.");
    }

    private Task<ShoppingList?> FindListOrNullAsync(int listId)
    {
        return _dbContext.ShoppingLists
            .FirstOrDefaultAsync(item => item.Id == listId);
    }

    private async Task EnsureCanEditAsync(ShoppingList list, string userId, bool isAdmin)
    {
        var hasActiveShare = !isAdmin && await _dbContext.ListShares.AnyAsync(share =>
            share.ListId == list.Id &&
            share.UserId == userId);

        if (!ShoppingListAccessPolicy.CanEdit(list, userId, isAdmin, hasActiveShare))
        {
            throw new UnauthorizedAccessException("You do not have permission to access this shopping list.");
        }
    }

    private static void EnsureCanManageSharing(ShoppingList list, string userId, bool isAdmin)
    {
        if (!ShoppingListAccessPolicy.CanManageSharing(list, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("Only the list owner or an administrator can manage shares.");
        }
    }

    private static void EnsureCanManageListName(ShoppingList list, string userId, bool isAdmin)
    {
        if (!ShoppingListAccessPolicy.CanManageSharing(list, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("Only the list owner or an administrator can rename the list.");
        }
    }

    private static void EnsureCanDelete(ShoppingList list, string userId, bool isAdmin)
    {
        if (!ShoppingListAccessPolicy.CanDelete(list, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("Only the list owner or an administrator can delete the list.");
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("List name is required.");
        }

        var normalized = name.Trim();
        if (normalized.Length > 200)
        {
            throw new ValidationException("List name cannot exceed 200 characters.");
        }

        return normalized;
    }

    private static string? NormalizeNotes(string? notes)
    {
        return string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    private static void ValidateNotes(string? notes)
    {
        if (notes?.Length > 2000)
        {
            throw new ValidationException("Notes cannot exceed 2000 characters.");
        }
    }

    private void SetOriginalRowVersion<T>(T entity, string rowVersion) where T : class
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            throw new ValidationException("A row version is required.");
        }

        byte[] version;
        try
        {
            version = Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            throw new ValidationException("The row version is invalid.");
        }

        if (version.Length != 8)
        {
            throw new ValidationException("The row version is invalid.");
        }

        _dbContext.Entry(entity).Property(nameof(ShoppingList.RowVersion)).OriginalValue = version;
    }

    private static ShoppingListDto ToDto(ShoppingList list)
    {
        var products = list.ListProducts
            .Where(item => item.Product is not null)
            .Select(item => ToDto(item))
            .ToList();

        return new ShoppingListDto(
            list.Id,
            list.Name,
            list.OwnerUserId,
            list.CreatedAt,
            list.UpdatedAt,
            products,
            Convert.ToBase64String(list.RowVersion));
    }

    private static ListProductDto ToDto(ListProduct item, Product? product = null)
    {
        product ??= item.Product;
        return new ListProductDto(
            item.Id,
            item.ListId,
            item.ProductId,
            product?.Name ?? string.Empty,
            product?.ProductTypeId ?? 0,
            product?.ProductType?.Name ?? string.Empty,
            product?.Price ?? 0m,
            item.TipicalOrder,
            item.ToOrderNow,
            item.Notes,
            item.CreatedAt,
            item.UpdatedAt,
            Convert.ToBase64String(item.RowVersion));
    }
}
