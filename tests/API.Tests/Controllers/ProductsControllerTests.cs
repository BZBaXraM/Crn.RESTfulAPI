using System.Net;
using System.Net.Http.Json;
using Application.Common;
using Application.DTOs;
using API.Tests.TestSupport;
using Xunit;

namespace API.Tests.Controllers;

public class ProductsControllerTests : ApiTestBase
{
    [Fact]
    public async Task GetAll_without_a_token_returns_401()
    {
        var response = await Client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_for_unknown_id_returns_404_problem_details()
    {
        var auth = await Client.RegisterAndLoginAsync();
        Client.UseBearerToken(auth.AccessToken);

        var response = await Client.GetAsync("/api/v1/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":404", body);
    }

    [Fact]
    public async Task Create_as_regular_user_returns_403()
    {
        var auth = await Client.RegisterAndLoginAsync();
        Client.UseBearerToken(auth.AccessToken);

        var response = await Client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest("Widget"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_invalid_body_returns_400()
    {
        await Factory.CreateAdminAsync("admin1", "Admin123!");
        var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin1", "Admin123!"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Client.UseBearerToken(auth!.AccessToken);

        var response = await Client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> LoginAsAdminAsync(string userName = "admin1")
    {
        await Factory.CreateAdminAsync(userName, "Admin123!");
        var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(userName, "Admin123!"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    [Fact]
    public async Task Full_CRUD_lifecycle_as_admin()
    {
        Client.UseBearerToken(await LoginAsAdminAsync());

        var createResponse = await Client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest("Widget"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var getResponse = await Client.GetAsync($"/api/v1/products/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await Client.PutAsJsonAsync($"/api/v1/products/{created.Id}", new UpdateProductRequest("Widget v2"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Widget v2", updated!.ProductName);

        var deleteResponse = await Client.DeleteAsync($"/api/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await Client.GetAsync($"/api/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task GetAll_returns_a_paged_result_honouring_page_size()
    {
        Client.UseBearerToken(await LoginAsAdminAsync());

        for (var i = 1; i <= 3; i++)
        {
            await Client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest($"Product {i}"));
        }

        var response = await Client.GetAsync("/api/v1/products?pageNumber=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
    }
}
