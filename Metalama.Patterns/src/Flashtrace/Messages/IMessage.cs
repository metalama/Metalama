// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Records;
using JetBrains.Annotations;

namespace Flashtrace.Messages;

/// <summary>
/// Represents a message that can be used with the <see cref="FlashtraceSource"/> class. To create a message 
/// instance, you would typically use the <see cref="SemanticMessageBuilder"/> or <see cref="FormattedMessageBuilder"/> class.
/// </summary>
[PublicAPI]
public interface IMessage
{
    /// <summary>
    /// Renders the current message into a given <see cref="ILogRecordBuilder"/>.
    /// </summary>
    /// <param name="builder">A <see cref="ILogRecordBuilder"/>.</param>
    /// <param name="kind">The situation in which the message is rendered (to be passed to <see cref="ILogRecordBuilder.BeginWriteItem"/>)..</param>
    /// <remarks>
    /// <para>The <see cref="IMessage"/> implementation is responsible for invoking <see cref="ILogRecordBuilder.BeginWriteItem"/>
    /// and <see cref="ILogRecordBuilder.EndWriteItem(LogRecordItem)"/>.
    /// </para>
    /// </remarks>
    void Write( ILogRecordBuilder builder, LogRecordItem kind );
}