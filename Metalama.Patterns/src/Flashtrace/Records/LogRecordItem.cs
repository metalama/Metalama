// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Messages;
using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Enumerates the scenarios in which an <see cref="IMessage"/> can be rendered.
/// </summary>
[PublicAPI]
public enum LogRecordItem
{
    /// <summary>
    /// Message.
    /// </summary>
    Message,

    /// <summary>
    /// Description of an activity.
    /// </summary>
    ActivityDescription,

    /// <summary>
    /// Outcome of an activity.
    /// </summary>
    ActivityOutcome
}