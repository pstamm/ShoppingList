namespace ShoppingList.Services.ShoppingLists;

public interface IShoppingListClient
{
    Task<IReadOnlyList<ShoppingListDto>> GetListsAsync();
    Task<ShoppingListDto> GetListAsync(int listId);
    Task<ShoppingListDto> CreateListAsync(string name);
    Task<ShoppingListDto> UpdateListAsync(int listId, string name, string rowVersion);
    Task DeleteListAsync(int listId, string rowVersion);

    Task<IReadOnlyList<ListProductDto>> GetListProductsAsync(int listId);
    Task<ListProductDto> AddListProductAsync(int listId, AddListProductRequest request);
    Task<ListProductDto> UpdateListProductAsync(
        int listId,
        int listProductId,
        UpdateListProductRequest request);
    Task DeleteListProductAsync(int listId, int listProductId, string rowVersion);

    Task<IReadOnlyList<ListShareDto>> GetSharesAsync(int listId);
    Task<ListShareDto> CreateShareAsync(int listId, string userId);
    Task RemoveShareAsync(int listId, string userId);
}
