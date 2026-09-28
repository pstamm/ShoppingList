using ShoppingApi.Domain.Entities;

namespace ShoppingApi.Services.ShoppingLists;

public static class ShoppingListAccessPolicy
{
    public static bool CanEdit(ShoppingList list, string userId, bool isAdmin, bool hasActiveShare)
    {
        return isAdmin || list.OwnerUserId == userId || hasActiveShare;
    }

    public static bool CanManageSharing(ShoppingList list, string userId, bool isAdmin)
    {
        return isAdmin || list.OwnerUserId == userId;
    }

    public static bool CanDelete(ShoppingList list, string userId, bool isAdmin)
    {
        return CanManageSharing(list, userId, isAdmin);
    }
}
