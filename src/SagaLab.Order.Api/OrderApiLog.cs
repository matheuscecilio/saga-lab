namespace SagaLab.Order.Api;

internal static partial class OrderApiLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Order submitted. OrderId: {OrderId}; ItemCount: {ItemCount}; TotalAmount: {TotalAmount}; Currency: {Currency}")]
    internal static partial void OrderSubmitted(
        ILogger logger,
        Guid orderId,
        int itemCount,
        decimal totalAmount,
        string currency);
}
