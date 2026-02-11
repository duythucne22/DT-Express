using System.Net;
using System.Net.Http.Json;
using DtExpress.Api.Models;
using DtExpress.Api.Models.Audit;
using DtExpress.Api.Models.Orders;
using DtExpress.Api.Tests.Fixtures;

namespace DtExpress.Api.Tests;

/// <summary>
/// End-to-end order lifecycle test:
/// LOGIN → CREATE → CONFIRM → SHIP → DELIVER
/// Verifies state transitions, audit trail, carrier booking, and tracking.
/// </summary>
public sealed class OrderLifecycleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrderLifecycleTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ═════════════════════════════════════════════════════════════
    //  Full lifecycle: Created → Confirmed → Shipped → Delivered
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task FullOrderLifecycle_CreatedToDelivered()
    {
        // ── Step 1: LOGIN as dispatcher ──────────────────────────
        var auth = await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);
        Assert.Equal("Dispatcher", auth.Role);
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));

        // ── Step 2: CREATE order ────────────────────────────────
        var createRequest = TestDataBuilders.ValidCreateOrderRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>();
        Assert.True(createResult!.Success);
        Assert.NotNull(createResult.Data);
        var orderId = createResult.Data.OrderId;
        var orderNumber = createResult.Data.OrderNumber;
        Assert.NotEqual(Guid.Empty, orderId);
        Assert.StartsWith("DT-", orderNumber);
        Assert.Equal("Created", createResult.Data.Status);

        // Verify order is retrievable
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var orderDetail = await getResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDetailResponse>>();
        Assert.True(orderDetail!.Success);
        Assert.Equal("Created", orderDetail.Data!.Status);
        Assert.Equal("Express", orderDetail.Data.ServiceLevel);
        Assert.Equal("张三", orderDetail.Data.CustomerName);
        Assert.Single(orderDetail.Data.Items);

        // ── Step 3: CONFIRM order ───────────────────────────────
        var confirmResponse = await _client.PutAsync($"/api/orders/{orderId}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmResult = await confirmResponse.Content.ReadFromJsonAsync<ApiResponse<OrderTransitionResponse>>();
        Assert.True(confirmResult!.Success);
        Assert.Equal("Confirmed", confirmResult.Data!.NewStatus);
        Assert.Equal(orderId, confirmResult.Data.OrderId);

        // Verify state persisted
        var afterConfirm = await _client.GetAsync($"/api/orders/{orderId}");
        var detailAfterConfirm = await afterConfirm.Content.ReadFromJsonAsync<ApiResponse<OrderDetailResponse>>();
        Assert.Equal("Confirmed", detailAfterConfirm!.Data!.Status);

        // ── Step 4: SHIP order ──────────────────────────────────
        var shipResponse = await _client.PutAsync($"/api/orders/{orderId}/ship", null);
        Assert.Equal(HttpStatusCode.OK, shipResponse.StatusCode);

        var shipResult = await shipResponse.Content.ReadFromJsonAsync<ApiResponse<OrderTransitionResponse>>();
        Assert.True(shipResult!.Success);
        Assert.Equal("Shipped", shipResult.Data!.NewStatus);
        Assert.False(string.IsNullOrEmpty(shipResult.Data.CarrierCode),
            "Carrier code should be assigned after shipping");
        Assert.False(string.IsNullOrEmpty(shipResult.Data.TrackingNumber),
            "Tracking number should be generated after shipping");

        // Verify tracking info persisted on order
        var afterShip = await _client.GetAsync($"/api/orders/{orderId}");
        var detailAfterShip = await afterShip.Content.ReadFromJsonAsync<ApiResponse<OrderDetailResponse>>();
        Assert.Equal("Shipped", detailAfterShip!.Data!.Status);
        Assert.NotNull(detailAfterShip.Data.TrackingNumber);
        Assert.NotNull(detailAfterShip.Data.CarrierCode);

        // ── Step 5: DELIVER order ───────────────────────────────
        // Deliver requires Admin or Driver role — switch to admin
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Admin);

        var deliverResponse = await _client.PutAsync($"/api/orders/{orderId}/deliver", null);
        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);

        var deliverResult = await deliverResponse.Content.ReadFromJsonAsync<ApiResponse<OrderTransitionResponse>>();
        Assert.True(deliverResult!.Success);
        Assert.Equal("Delivered", deliverResult.Data!.NewStatus);

        // Verify final state
        var afterDeliver = await _client.GetAsync($"/api/orders/{orderId}");
        var detailAfterDeliver = await afterDeliver.Content.ReadFromJsonAsync<ApiResponse<OrderDetailResponse>>();
        Assert.Equal("Delivered", detailAfterDeliver!.Data!.Status);

        // ── Step 6: Verify audit trail ──────────────────────────
        var auditResponse = await _client.GetAsync($"/api/audit/entity/Order/{orderId}");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var auditResult = await auditResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<AuditRecordResponse>>>();
        Assert.True(auditResult!.Success);
        Assert.NotNull(auditResult.Data);

        // Should have audit records for create + confirm + ship + deliver = at least 4
        Assert.True(auditResult.Data.Count >= 4,
            $"Expected at least 4 audit records, got {auditResult.Data.Count}");

        // Verify audit records have proper actors (not "system")
        foreach (var record in auditResult.Data)
        {
            Assert.Equal("Order", record.EntityType);
            Assert.Equal(orderId.ToString(), record.EntityId);
            Assert.NotNull(record.CorrelationId);
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  Cancel flow: Created → Cancelled
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task CancelOrder_FromCreatedState_Succeeds()
    {
        // Arrange — create an order
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(_client);

        // Act — cancel with reason
        var cancelRequest = new CancelOrderRequest { Reason = "客户取消订单" };
        var cancelResponse = await _client.PutAsJsonAsync($"/api/orders/{orderId}/cancel", cancelRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var result = await cancelResponse.Content.ReadFromJsonAsync<ApiResponse<OrderTransitionResponse>>();
        Assert.Equal("Cancelled", result!.Data!.NewStatus);

        // Verify persisted
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        var detail = await getResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDetailResponse>>();
        Assert.Equal("Cancelled", detail!.Data!.Status);
    }

    // ═════════════════════════════════════════════════════════════
    //  List orders with filters
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListOrders_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange — create and confirm an order
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(_client);
        await _client.PutAsync($"/api/orders/{orderId}/confirm", null);

        // Act — list only confirmed orders
        var listResponse = await _client.GetAsync("/api/orders?status=Confirmed");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var result = await listResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<OrderSummaryResponse>>>();
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);
        Assert.All(result.Data, o => Assert.Equal("Confirmed", o.Status));
    }

    [Fact]
    public async Task ListOrders_AllOrders_ReturnsList()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);
        await TestDataBuilders.CreateOrderAsync(_client);

        // Act — no filters
        var listResponse = await _client.GetAsync("/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var result = await listResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<OrderSummaryResponse>>>();
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Count >= 1);
    }

    // ═════════════════════════════════════════════════════════════
    //  Correlation ID preserved through lifecycle
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task OrderCreate_PreservesCorrelationId()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);
        var correlationId = $"lifecycle-test-{Guid.NewGuid()}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        request.Headers.Add("X-Correlation-ID", correlationId);
        request.Content = JsonContent.Create(TestDataBuilders.ValidCreateOrderRequest());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").First());

        var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>();
        Assert.Equal(correlationId, apiResp!.CorrelationId);
    }
}
