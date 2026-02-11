using System.Net.Http.Json;
using DtExpress.Api.Models;
using DtExpress.Api.Models.Orders;

namespace DtExpress.Api.Tests.Fixtures;

/// <summary>
/// Reusable test data builders for API requests.
/// </summary>
public static class TestDataBuilders
{
    /// <summary>Creates a valid order request with Shanghai→Guangzhou Express shipment.</summary>
    public static CreateOrderRequest ValidCreateOrderRequest() => new()
    {
        CustomerName = "张三",
        CustomerPhone = "13812345678",
        CustomerEmail = "zhangsan@example.com",
        Origin = new OrderAddressDto
        {
            Street = "浦东新区陆家嘴环路1000号",
            City = "上海",
            Province = "Shanghai",
            PostalCode = "200120",
            Country = "CN"
        },
        Destination = new OrderAddressDto
        {
            Street = "天河区珠江新城花城大道18号",
            City = "广州",
            Province = "Guangdong",
            PostalCode = "510623",
            Country = "CN"
        },
        ServiceLevel = "Express",
        Items =
        [
            new OrderItemDto
            {
                Description = "电子产品 - 笔记本电脑",
                Quantity = 1,
                Weight = new OrderWeightDto(2.5m, "Kg"),
                Dimensions = new OrderDimensionDto(35.00m, 25.00m, 3.00m)
            }
        ]
    };

    /// <summary>Creates a large order with the specified number of items.</summary>
    public static CreateOrderRequest LargeOrderRequest(int itemCount) => new()
    {
        CustomerName = "大订单测试",
        CustomerPhone = "13999888777",
        CustomerEmail = "large@test.com",
        Origin = new OrderAddressDto
        {
            Street = "朝阳区建国门外大街1号",
            City = "北京",
            Province = "Beijing",
            PostalCode = "100020",
            Country = "CN"
        },
        Destination = new OrderAddressDto
        {
            Street = "武侯区天府大道北段1700号",
            City = "成都",
            Province = "Sichuan",
            PostalCode = "610041",
            Country = "CN"
        },
        ServiceLevel = "Standard",
        Items = Enumerable.Range(1, itemCount).Select(i => new OrderItemDto
        {
            Description = $"测试商品 #{i}",
            Quantity = i,
            Weight = new OrderWeightDto(0.5m * i, "Kg"),
            Dimensions = i % 2 == 0 ? new OrderDimensionDto(10m + i, 8m + i, 5m) : null
        }).ToList()
    };

    /// <summary>Create an order via the API and return its ID.</summary>
    public static async Task<(Guid OrderId, string OrderNumber)> CreateOrderAsync(HttpClient client)
    {
        var request = ValidCreateOrderRequest();
        var response = await client.PostAsJsonAsync("/api/orders", request);
        response.EnsureSuccessStatusCode();

        var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<CreateOrderResponse>>();
        return (apiResp!.Data!.OrderId, apiResp.Data.OrderNumber);
    }
}
