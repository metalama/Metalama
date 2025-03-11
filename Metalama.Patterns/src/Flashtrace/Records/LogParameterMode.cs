// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Determines how a parameter of a log record should be rendered.
/// method.
/// </summary>
[PublicAPI]
public enum LogParameterMode
{
    /// <summary>
    /// Only the parameter value is rendered.
    /// </summary>
    Default,

    /// <summary>
    /// The parameter is rendered in <c>name = value</c> form.
    /// </summary>
    NameValuePair,

    /// <summary>
    /// The parameter is not rendered.
    /// </summary>
    Hidden
}