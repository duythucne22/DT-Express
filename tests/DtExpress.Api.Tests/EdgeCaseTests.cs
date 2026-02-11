using System.Net;
using System.Net.Http.Json;
using DtExpress.Api.Models;
using DtExpress.Api.Models.Orders;
using DtExpress.Api.Tests.Fixtures;

namespace DtExpress.Api.Tests;

/// <summary>
/// Edge case tests:
/// - Invalid state transitions (should fail gracefully)
/// - Large orders with 10+ items
/// - Cancel order from various states
/// - Concurrent order creation
/// - Nonexistent resources
/// </summary>
public sealed class EdgeCaseTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EdgeCaseTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    // ═════════════════════════════════════════════════════════════
    //  Invalid state transitions
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task Ship_FromCreatedState_Returns400()
    {
        // Arrange — create order (Created state) then try to Ship without Confirming
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Dispatcher);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(client);

        // Act — skip Confirm and go straight to Ship
        var response = await client.PutAsync($"/api/orders/{orderId}/ship", null);

        // Assert — should be InvalidStateTransitionException → 400
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(result!.Success);
        Assert.Equal("INVALID_TRANSITION", result.Error!.Code);
    }

    [Fact]
    public async Task Deliver_FromCreatedState_Returns400()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Admin);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(client);

        var response = await client.PutAsync($"/api/orders/{orderId}/deliver", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("INVALID_TRANSITION", result!.Error!.Code);
    }

    [Fact]
    public async Task Confirm_AlreadyConfirmedOrder_Returns400()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Dispatcher);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(client);

        // Confirm first time — succeeds
        var first = await client.PutAsync($"/api/orders/{orderId}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Confirm again — should fail
        var second = await client.PutAsync($"/api/orders/{orderId}/confirm", null);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

        var result = await second.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("INVALID_TRANSITION", result!.Error!.Code);
    }

    [Fact]
    public async Task Cancel_DeliveredOrder_Returns400()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Admin);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(client);

        // Drive to Delivered state
        await client.PutAsync($"/api/orders/{orderId}/confirm", null);
        await client.PutAsync($"/api/orders/{orderId}/ship", null);
        await client.PutAsync($"/api/orders/{orderId}/deliver", null);

        // Try to cancel
        var cancelBody = JsonContent.Create(new CancelOrderRequest { Reason = "too late" });
        var response = await client.PutAsync($"/api/orders/{orderId}/cancel", cancelBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("INVALID_TRANSITION", result!.Error!.Code);
    }

    [Fact]
    public async Task Cancel_CancelledOrder_Returns400()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Dispatcher);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(client);

        // Cancel first time
        var cancelBody = JsonContent.Create(new CancelOrderRequest { Reason = "first cancel" });
        var first = await client.PutAsync($"/api/orders/{orderId}/cancel", cancelBody);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Cancel again — should fail (terminal state)
        var cancelBody2 = JsonContent.Create(new CancelOrderRequest { Reason = "double cancel" });
        var second = await client.PutAsync($"/api/orders/{orderId}/cancel", cancelBody2);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Ship_CancelledOrder_Returns400()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Dispatcher);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(client);

        // Cancel
        var cancelBody = JsonContent.Create(new CancelOrderRequest { Reason = "cancelled" });
        await client.PutAsync($"/api/orders/{orderId}/cancel", cancelBody);

        // Try to ship cancelled order
        var response = await client.PutAsync($"/api/orders/{orderId}/ship", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════
    //  Large order with 10+ items
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateOrder_With12Items_Succeeds()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Dispatcher);

        var request = TestDataBuilders.LargeOrderRequest(12);
        var response = await client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createResult = await response.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>();
        var orderId = createResult!.Data!.OrderId;

        // Verify all 12 items persisted
        var detail = await client.GetFromJsonAsync<ApiResponse<OrderDetailResponse>>($"/api/orders/{orderId}");
        Assert.Equal(12, detail!.Data!.Items.Count);
    }

    // ═════════════════════════════════════════════════════════════
    //  Nonexistent resources
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetOrder_NonexistentId_Returns404()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Viewer);

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Carrier_BookWithInvalidCode_Returns404()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Dispatcher);

        var request = new Models.Carrier.BookShipmentRequest
        {
            Origin = new Models.Carrier.AddressDto
            {
                Street = "浦东新区陆家嘴环路1000号", City = "上海",
                Province = "Shanghai", PostalCode = "200120"
            },
            Destination = new Models.Carrier.AddressDto
            {
                Street = "天河区珠江新城花城大道18号", City = "广州",
                Province = "Guangdong", PostalCode = "510623"
            },
            Weight = new Models.Carrier.CarrierWeightDto(2.5m, "Kg"),
            Sender = new Models.Carrier.ContactInfoDto { Name = "张三", Phone = "13812345678" },
            Recipient = new Models.Carrier.ContactInfoDto { Name = "李四", Phone = "13987654321" },
            ServiceLevel = "Express"
        };

        var response = await client.PostAsJsonAsync("/api/carriers/UNKNOWN/book", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("CARRIER_NOT_FOUND", result!.Error!.Code);
    }

    [Fact]
    public async Task Routing_InvalidStrategy_Returns400()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Dispatcher);

        var response = await client.PostAsJsonAsync("/api/routing/calculate", new Models.Routing.CalculateRouteRequest
        {
            Origin = new Models.Routing.GeoCoordinateDto(31.2304m, 121.4737m),
            Destination = new Models.Routing.GeoCoordinateDto(39.9042m, 116.4074m),
            PackageWeight = new Models.Routing.WeightDto(2.5m, "Kg"),
            ServiceLevel = "Express",
            Strategy = "SuperFast"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("STRATEGY_NOT_FOUND", result!.Error!.Code);
    }

    // ═════════════════════════════════════════════════════════════
    //  Concurrent order creation
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConcurrentOrderCreation_AllSucceed()
    {
        const int concurrency = 5;
        var tasks = new Task<HttpResponseMessage>[concurrency];

        for (int i = 0; i < concurrency; i++)
        {
            var client = CreateClient();
            await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Dispatcher);
            tasks[i] = client.PostAsJsonAsync("/api/orders", TestDataBuilders.ValidCreateOrderRequest());
        }

        var responses = await Task.WhenAll(tasks);

        // All should succeed
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        // All order IDs should be unique
        var orderIds = new HashSet<Guid>();
        foreach (var resp in responses)
        {
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>();
            Assert.True(orderIds.Add(result!.Data!.OrderId), "Duplicate order ID detected");
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  Cancel confirmed order (still allowed)
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task Cancel_ConfirmedOrder_Succeeds()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Dispatcher);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(client);

        // Confirm first
        await client.PutAsync($"/api/orders/{orderId}/confirm", null);

        // Cancel should still work from Confirmed
        var cancelBody = JsonContent.Create(new CancelOrderRequest { Reason = "changed mind" });
        var response = await client.PutAsync($"/api/orders/{orderId}/cancel", cancelBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderTransitionResponse>>();
        Assert.Equal("Cancelled", result!.Data!.NewStatus);
    }

    // ═════════════════════════════════════════════════════════════
    //  Cancel shipped order — should fail (Shipped only goes to Delivered)
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task Cancel_ShippedOrder_Returns400()
    {
        var client = CreateClient();
        await TestAuthHelper.AuthenticateAsync(client, TestAuthHelper.Admin);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(client);

        await client.PutAsync($"/api/orders/{orderId}/confirm", null);
        await client.PutAsync($"/api/orders/{orderId}/ship", null);

        // Try cancel from Shipped
        var cancelBody = JsonContent.Create(new CancelOrderRequest { Reason = "too late" });
        var response = await client.PutAsync($"/api/orders/{orderId}/cancel", cancelBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal("INVALID_TRANSITION", result!.Error!.Code);
    }
}
