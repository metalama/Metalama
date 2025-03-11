// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Represents a code transformation.
/// </summary>
[PublicAPI]
public interface IIntrospectionTransformation : IComparable<IIntrospectionTransformation>
{
    /// <summary>
    /// Gets the transformation kind.
    /// </summary>
    IntrospectionTransformationKind TransformationKind { get; }

    /// <summary>
    /// Gets the declaration being transformed.
    /// </summary>
    IDeclaration TargetDeclaration { get; }

    /// <summary>
    /// Gets a human-readable description of the transformation.
    /// </summary>
    FormattableString Description { get; }

    /// <summary>
    /// Gets the declaration being introduced (i.e. added) into <see cref="TargetDeclaration"/>, if any.
    /// </summary>
    IDeclaration? IntroducedDeclaration { get; }

    /// <summary>
    /// Gets the piece of advice that provided the current transformation.
    /// </summary>
    IIntrospectionAdvice Advice { get; }

    /// <summary>
    /// Gets the order of the transformation within the aspect instance that provided it.
    /// </summary>
    int Order { get; }
}