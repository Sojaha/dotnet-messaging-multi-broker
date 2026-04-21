// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Messaging.Contracts;

/// <summary>
/// Marker interface for commands.
///
/// Semantics: "do this" — imperative, directed at a specific service.
/// Exactly one consumer owns and handles each command type.
/// Sending the same command type to more than one queue is an architecture error.
///
/// Routing: direct exchange, point-to-point to a single named queue.
///
/// Naming convention: imperative verb + noun — CancelOrder, ProcessPayment, SendEmail.
/// </summary>
public interface ICommand : IMessage { }
