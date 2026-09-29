using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingApi.Controllers;

namespace ShoppingApi.ApiTests;

public class ListAuthorizationTests
{
    [Theory]
    [InlineData(typeof(ShoppingListsController))]
    [InlineData(typeof(ListProductsController))]
    [InlineData(typeof(ListSharesController))]
    [InlineData(typeof(ProductsController))]
    [InlineData(typeof(ProductTypesController))]
    public void All_list_endpoints_require_authentication(Type controllerType)
    {
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());

        var actions = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<NonActionAttribute>() is null);

        Assert.NotEmpty(actions);
        Assert.All(actions, action => Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>()));
    }
}
