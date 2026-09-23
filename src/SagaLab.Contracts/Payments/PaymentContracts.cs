namespace SagaLab.Contracts.Payments;

public sealed record AuthorizePayment(Guid OrderId, decimal Amount, string Currency);

public sealed record PaymentAuthorized(Guid OrderId, Guid PaymentId, DateTimeOffset AuthorizedAt);

public sealed record PaymentAuthorizationFailed(Guid OrderId, string Reason, DateTimeOffset FailedAt);

public sealed record RefundPayment(Guid OrderId, Guid PaymentId, string Reason);

public sealed record PaymentRefunded(Guid OrderId, Guid PaymentId, DateTimeOffset RefundedAt);
