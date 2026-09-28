using ShoppingApi.DTOs.ShoppingLists;

namespace ShoppingApi.Services.ShoppingLists;

public interface IShoppingListService
{
    Task<IEnumerable<ShoppingListDto>> GetListsAsync(string userId, bool isAdmin);
    Task<ShoppingListDto?> GetListAsync(int listId, string userId, bool isAdmin);
    Task<ShoppingListDto> CreateListAsync(CreateShoppingListRequest request, string userId);
    Task<ShoppingListDto> UpdateListAsync(int listId, UpdateShoppingListRequest request, string userId, bool isAdmin);
    Task DeleteListAsync(int listId, string rowVersion, string userId, bool isAdmin);

    Task<IEnumerable<ListProductDto>?> GetListProductsAsync(int listId, string userId, bool isAdmin);
    Task<ListProductDto> AddListProductAsync(int listId, AddListProductRequest request, string userId, bool isAdmin);
    Task<ListProductDto> UpdateListProductAsync(
        int listId,
        int listProductId,
        UpdateListProductRequest request,
        string userId,
        bool isAdmin);
    Task DeleteListProductAsync(
        int listId,
        int listProductId,
        string rowVersion,
        string userId,
        bool isAdmin);

    Task<IEnumerable<ListShareDto>> GetSharesAsync(int listId, string userId, bool isAdmin);
    Task<ListShareDto> CreateShareAsync(int listId, ShareListRequest request, string userId, bool isAdmin);
    Task RemoveShareAsync(int listId, string sharedUserId, string userId, bool isAdmin);
}
