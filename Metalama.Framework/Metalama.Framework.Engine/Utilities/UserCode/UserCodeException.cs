// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.Utilities.UserCode;

/// <summary>
/// An <see cref="Exception"/> thrown by user code. It does not necessarily mean that the culprit is the user.
/// It can be a Metalama bug, too.
/// </summary>
internal sealed class UserCodeException : Exception
{
    internal UserCodeException( UserCodeExecutionContext context, Exception innerException )
        : base(
            MetalamaStringFormatter.Format(
                $"'Exception of type '{innerException.GetType().FullName}' thrown while {context.Description}': {innerException.Message}" ),
            innerException )
    {
        this.TargetDeclaration = context.TargetDeclaration;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    internal IDeclaration? TargetDeclaration { get; }
}