using ShoppingApi.Domain.Entities;
using ShoppingApi.Services.ShoppingLists;

namespace ShoppingApi.ApiTests;

public class ShoppingListAccessPolicyTests
{
    private static readonly ShoppingList List = new() { OwnerUserId = "owner-id" };

    [Fact]
    public void Owner_can_edit_manage_and_delete_their_list()
    {
        Assert.True(ShoppingListAccessPolicy.CanEdit(List, "owner-id", isAdmin: false, hasActiveShare: false));
        Assert.True(ShoppingListAccessPolicy.CanManageSharing(List, "owner-id", isAdmin: false));
        Assert.True(ShoppingListAccessPolicy.CanDelete(List, "owner-id", isAdmin: false));
    }

    [Fact]
    public void Admin_can_access_manage_and_delete_another_users_list()
    {
        Assert.True(ShoppingListAccessPolicy.CanEdit(List, "admin-id", isAdmin: true, hasActiveShare: false));
        Assert.True(ShoppingListAccessPolicy.CanManageSharing(List, "admin-id", isAdmin: true));
        Assert.True(ShoppingListAccessPolicy.CanDelete(List, "admin-id", isAdmin: true));
    }

    [Fact]
    public void Active_shared_user_can_edit_but_cannot_manage_shares_or_delete()
    {
        Assert.True(ShoppingListAccessPolicy.CanEdit(List, "shared-id", isAdmin: false, hasActiveShare: true));
        Assert.False(ShoppingListAccessPolicy.CanManageSharing(List, "shared-id", isAdmin: false));
        Assert.False(ShoppingListAccessPolicy.CanDelete(List, "shared-id", isAdmin: false));
    }

    [Fact]
    public void Unshared_user_cannot_access_manage_or_delete_private_list()
    {
        Assert.False(ShoppingListAccessPolicy.CanEdit(List, "other-id", isAdmin: false, hasActiveShare: false));
        Assert.False(ShoppingListAccessPolicy.CanManageSharing(List, "other-id", isAdmin: false));
        Assert.False(ShoppingListAccessPolicy.CanDelete(List, "other-id", isAdmin: false));
    }
}
