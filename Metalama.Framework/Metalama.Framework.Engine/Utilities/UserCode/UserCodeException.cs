// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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