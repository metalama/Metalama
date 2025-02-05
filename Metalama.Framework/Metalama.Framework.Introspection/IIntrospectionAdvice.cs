// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using System.Collections.Immutable;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Represents a piece of advice provided by an aspect.
/// </summary>
[PublicAPI]
public interface IIntrospectionAdvice
{
    /// <summary>
    /// Gets the aspect that provided the piece of advice.
    /// </summary>
    IIntrospectionAspectInstance AspectInstance { get; }

    /// <summary>
    /// Gets the kind of advice.
    /// </summary>
    AdviceKind AdviceKind { get; }

    /// <summary>
    /// Gets the advised declaration.
    /// </summary>
    IDeclaration TargetDeclaration { get; }

    /// <summary>
    /// Gets the identifier of the aspect layer that provided the piece of advice.
    /// </summary>
    string AspectLayerId { get; }

    /// <summary>
    /// Gets the list of transformations provided by the current piece of advice.
    /// </summary>
    ImmutableArray<IIntrospectionTransformation> Transformations { get; }
}