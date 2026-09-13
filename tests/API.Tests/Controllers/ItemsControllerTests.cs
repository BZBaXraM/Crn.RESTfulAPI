using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using API.Tests.TestSupport;
using Xunit;

namespace API.Tests.Controllers;

public class ItemsControllerTests : ApiTestBase
{
    private async Task<string> LoginAsAdminAsync()
    {
        await Factory.CreateAdminAsync("admin1", "Admin123!");
        var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin1", "Admin123!"));
        return (await login.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    [Fact]
    public async Task Create_item_for_unknown_product_returns_404()
    {
        Client.UseBearerToken(await LoginAsAdminAsync());

        var response = await Client.PostAsJsonAsync("/api/v1/products/999/items", new CreateItemRequest(5));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Item_lifecycle_is_scoped_to_its_product()
    {
        Client.UseBearerToken(await LoginAsAdminAsync());

        var product = await (await Client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest("Widget")))
            .Content.ReadFromJsonAsync<ProductDto>();
        var otherProduct = await (await Client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest("Gadget")))
            .Content.ReadFromJsonAsync<ProductDto>();

        var createResponse = await Client.PostAsJsonAsync($"/api/v1/products/{product!.Id}/items", new CreateItemRequest(10));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();

        // Same item id, wrong product in the URL -> not found.
        var wrongScopeResponse = await Client.GetAsync($"/api/v1/products/{otherProduct!.Id}/items/{item!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, wrongScopeResponse.StatusCode);

        var correctScopeResponse = await Client.GetAsync($"/api/v1/products/{product.Id}/items/{item.Id}");
        Assert.Equal(HttpStatusCode.OK, correctScopeResponse.StatusCode);

        var updateResponse = await Client.PutAsJsonAsync($"/api/v1/products/{product.Id}/items/{item.Id}", new UpdateItemRequest(50));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var listResponse = await Client.GetAsync($"/api/v1/products/{product.Id}/items");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var deleteResponse = await Client.DeleteAsync($"/api/v1/products/{product.Id}/items/{item.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
