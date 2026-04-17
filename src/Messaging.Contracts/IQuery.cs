namespace Messaging.Contracts;

/// <summary>
/// Marker interface for request/reply queries.
///
/// Semantics: "give me data" — expects a response on a reply queue.
/// Use the AMQP reply-to property to carry the caller's reply queue name.
/// The handler publishes the result back to that queue.
///
/// Routing: direct exchange + ephemeral exclusive reply queue per caller.
///
/// Use sparingly: if a query crosses a service boundary on every request,
/// a synchronous REST/gRPC call may be more appropriate.
///
/// Naming convention: Get/Find/Check + noun — GetOrderStatus, FindCustomer.
/// </summary>
public interface IQuery<TResult> : IMessage { }
