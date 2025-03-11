// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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