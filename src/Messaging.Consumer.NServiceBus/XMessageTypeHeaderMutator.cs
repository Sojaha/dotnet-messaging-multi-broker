// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using global::NServiceBus.MessageMutator;

namespace Messaging.Consumer.NServiceBus;

/// <summary>
/// Bridges the raw-publisher wire format to the NServiceBus message pipeline.
///
/// Our publisher (Messaging.Publisher) stamps every AMQP message with a custom
/// "x-message-type" header containing the full CLR type name. NServiceBus requires
/// the "NServiceBus.EnclosedMessageTypes" header to route the message to its handler.
///
/// This mutator runs early in the pipeline (before routing and deserialization) and
/// copies the value across when the NServiceBus header is absent. NServiceBus
/// auto-discovers classes implementing IMutateIncomingTransportMessages so no
/// explicit registration is needed.
/// </summary>
public sealed class XMessageTypeHeaderMutator : IMutateIncomingTransportMessages
{
    public Task MutateIncoming(MutateIncomingTransportMessageContext context)
    {
        if (!context.Headers.ContainsKey(Headers.EnclosedMessageTypes) &&
            context.Headers.TryGetValue("x-message-type", out string? typeName) &&
            !string.IsNullOrEmpty(typeName))
        {
            context.Headers[Headers.EnclosedMessageTypes] = typeName;
        }

        return Task.CompletedTask;
    }
}
