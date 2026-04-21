// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Messaging.Contracts;

/// <summary>
/// Marker interface for domain events.
///
/// Semantics: "something happened" — past tense, immutable fact.
/// The publisher does not know or care who consumes it.
///
/// Routing: topic exchange, fan-out to N consumer queues.
/// One event can be received by multiple independent consumers simultaneously.
///
/// Naming convention: past participle noun — OrderPlaced, PaymentFailed, UserRegistered.
/// </summary>
public interface IEvent : IMessage { }
