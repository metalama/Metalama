// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Introspection.Navigation.Results;

/// <summary>
/// An exception thrown while getting a value. Rendering continues around it.
/// </summary>
internal sealed class ErrorNode : ResultNode
{
    public ErrorNode( string exceptionType, string message )
    {
        this.ExceptionType = exceptionType;
        this.Message = message;
    }

    public string ExceptionType { get; }

    public string Message { get; }
}
