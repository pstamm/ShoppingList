namespace ShoppingApi.Services.Authentication;

public static class AdminUserManagementPolicy
{
    public static bool WouldRemoveLastActiveAdmin(
        bool targetIsAdmin,
        bool targetIsActive,
        bool willRemainAdmin,
        bool willRemainActive,
        int activeAdminCount)
    {
        return targetIsAdmin &&
               targetIsActive &&
               (!willRemainAdmin || !willRemainActive) &&
               activeAdminCount <= 1;
    }
}
