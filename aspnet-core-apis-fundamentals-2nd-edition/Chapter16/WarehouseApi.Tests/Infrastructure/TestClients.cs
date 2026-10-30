using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WarehouseApi.Contracts;

namespace WarehouseApi.Tests.Infrastructure;

public static class TestClients
{
    // A client whose requests authenticate as the given roles, optionally for one warehouse
    public static HttpClient CreateClientAs<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        string roles,
        string? warehouseCode = null,
        string user = "test-user") where TProgram : class
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, user);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);

        if (warehouseCode is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.WarehouseHeader, warehouseCode);
        }

        return client;
    }

    // Creating an order needs an Idempotency-Key (Chapter 7): a new one for each call unless the test passes one
    public static Task<HttpResponseMessage> PostOrderAsync(
        this HttpClient client, CreateOrderRequest order, string? idempotencyKey = null, string version = "v2")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/{version}/orders") { Content = JsonContent.Create(order) };
        request.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }
}
