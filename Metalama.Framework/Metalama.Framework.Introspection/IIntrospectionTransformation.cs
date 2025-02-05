// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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