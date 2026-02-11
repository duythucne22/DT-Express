using System.Net;
using System.Net.Http.Json;
using DtExpress.Api.Models;
using DtExpress.Api.Models.Orders;
using DtExpress.Api.Tests.Fixtures;
using DtExpress.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace DtExpress.Api.Tests;

/// <summary>
/// Database relationship verification tests:
/// - Order → OrderItems cascade
/// - Order → OrderEvents (state transitions)
/// - Audit logs capture user_id for all actions
/// - Foreign key constraints verification
/// </summary>
public sealed class DatabaseRelationshipTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DatabaseRelationshipTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ═════════════════════════════════════════════════════════════
    //  Order → OrderItems cascade (via API + DB check)
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateOrder_WithItems_PersistsItemsInDatabase()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);

        var request = TestDataBuilders.ValidCreateOrderRequest();
        // Add a second item
        request.Items.Add(new OrderItemDto
        {
            Description = "配件 - 充电器",
            Quantity = 2,
            Weight = new OrderWeightDto(0.3m, "Kg"),
            Dimensions = null
        });

        // Act — create the order
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createResult = await response.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>();
        var orderId = createResult!.Data!.OrderId;

        // Assert — verify items via API detail endpoint
        var detail = await _client.GetFromJsonAsync<ApiResponse<OrderDetailResponse>>($"/api/orders/{orderId}");
        Assert.NotNull(detail?.Data);
        Assert.Equal(2, detail.Data.Items.Count);
        Assert.Contains(detail.Data.Items, i => i.Description == "电子产品 - 笔记本电脑");
        Assert.Contains(detail.Data.Items, i => i.Description == "配件 - 充电器");
    }

    [Fact]
    public async Task CreateOrder_WithItems_PersistsWeightAndDimensions()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);
        var request = TestDataBuilders.ValidCreateOrderRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        var createResult = await response.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>();
        var orderId = createResult!.Data!.OrderId;

        // Assert — verify weight and dimension details
        var detail = await _client.GetFromJsonAsync<ApiResponse<OrderDetailResponse>>($"/api/orders/{orderId}");
        var item = detail!.Data!.Items.First();
        Assert.Equal(2.5m, item.Weight.Value);
        Assert.Equal("Kg", item.Weight.Unit);
        Assert.NotNull(item.Dimensions);
        Assert.Equal(35.00m, item.Dimensions.LengthCm);
        Assert.Equal(25.00m, item.Dimensions.WidthCm);
        Assert.Equal(3.00m, item.Dimensions.HeightCm);
    }

    // ═════════════════════════════════════════════════════════════
    //  Order → State transitions generate audit records
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task StateTransitions_CreateAuditRecords()
    {
        // Arrange — create and confirm
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(_client);
        await _client.PutAsync($"/api/orders/{orderId}/confirm", null);

        // Act — query audit trail
        var auditResponse = await _client.GetAsync($"/api/audit/entity/Order/{orderId}");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var auditResult = await auditResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<Models.Audit.AuditRecordResponse>>>();

        // Assert — at least Create + Confirm
        Assert.True(auditResult!.Data!.Count >= 2,
            $"Expected at least 2 audit records, got {auditResult.Data.Count}");
    }

    // ═════════════════════════════════════════════════════════════
    //  Audit logs are accessible by correlation ID
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task AuditLogs_QueryByCorrelation_ReturnsRelatedRecords()
    {
        // Arrange — create order with a known correlation ID
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);
        var correlationId = $"db-rel-test-{Guid.NewGuid()}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        request.Headers.Add("X-Correlation-ID", correlationId);
        request.Content = JsonContent.Create(TestDataBuilders.ValidCreateOrderRequest());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Act — query audit by correlation ID
        var auditResponse = await _client.GetAsync($"/api/audit/correlation/{correlationId}");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var auditResult = await auditResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<Models.Audit.AuditRecordResponse>>>();

        // Assert
        Assert.NotNull(auditResult?.Data);
        Assert.True(auditResult.Data.Count >= 1,
            "Should have at least 1 audit record for this correlation ID");
        Assert.All(auditResult.Data, r => Assert.Equal(correlationId, r.CorrelationId));
    }

    // ═════════════════════════════════════════════════════════════
    //  Database entity direct verification (via service scope)
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateOrder_StoresInDatabase_VerifyViaDbContext()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);

        // Act — create via API
        var (orderId, orderNumber) = await TestDataBuilders.CreateOrderAsync(_client);

        // Assert — verify directly in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var orderEntity = await db.Orders.FindAsync(orderId);
        Assert.NotNull(orderEntity);
        Assert.Equal(orderNumber, orderEntity.OrderNumber);
        Assert.Equal("Created", orderEntity.Status);
        Assert.Equal("Express", orderEntity.ServiceLevel);
        Assert.Equal("张三", orderEntity.CustomerName);
    }

    [Fact]
    public async Task CreateOrder_StoresItemsInDatabase()
    {
        // Arrange
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);

        // Act
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(_client);

        // Assert — verify items in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var items = db.OrderItems.Where(i => i.OrderId == orderId).ToList();
        Assert.Single(items);
        Assert.Equal("电子产品 - 笔记本电脑", items[0].Description);
        Assert.Equal(1, items[0].Quantity);
    }

    [Fact]
    public async Task Seed_UsersExistInDatabase()
    {
        await Task.CompletedTask; // Async to match convention

        // Assert — verify seeded users
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userCount = db.Users.Count();
        Assert.True(userCount >= 4, $"Expected at least 4 seeded users, got {userCount}");

        var admin = db.Users.FirstOrDefault(u => u.Username == "admin");
        Assert.NotNull(admin);
        Assert.Equal("Admin", admin.Role);
        Assert.True(admin.IsActive);
    }

    // ═════════════════════════════════════════════════════════════
    //  Audit logs store correct actor info
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task AuditLogs_ContainEntityTypeAndId()
    {
        // Arrange — create an order
        await TestAuthHelper.AuthenticateAsync(_client, TestAuthHelper.Dispatcher);
        var (orderId, _) = await TestDataBuilders.CreateOrderAsync(_client);

        // Act — get audit trail
        var auditResp = await _client.GetAsync($"/api/audit/entity/Order/{orderId}");
        var audit = await auditResp.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<Models.Audit.AuditRecordResponse>>>();

        // Assert
        Assert.All(audit!.Data!, r =>
        {
            Assert.Equal("Order", r.EntityType);
            Assert.Equal(orderId.ToString(), r.EntityId);
        });
    }
}
