// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Enumerates the kinds of code transformations.
/// </summary>
/// <seealso cref="IIntrospectionTransformation"/>
/// <seealso href="@introspection-api"/>
[PublicAPI]
public enum IntrospectionTransformationKind
{
    /// <summary>
    /// Represents an override of a member.
    /// </summary>
    OverrideMember,

    /// <summary>
    /// Represents the insertion of a statement.
    /// </summary>
    InsertStatement,

    /// <summary>
    /// Represents making a default constructor explicit.
    /// </summary>
    MakeDefaultConstructorExplicit,

    /// <summary>
    /// Represents the introduction of an attribute.
    /// </summary>
    IntroduceAttribute,

    /// <summary>
    /// Represents the insertion of a constructor initializer argument.
    /// </summary>
    InsertConstructorInitializerArgument,

    /// <summary>
    /// Represents the introduction of a member.
    /// </summary>
    IntroduceMember,

    /// <summary>
    /// Represents the implementation of an interface.
    /// </summary>
    ImplementInterface,

    /// <summary>
    /// Represents the introduction of a parameter.
    /// </summary>
    IntroduceParameter,

    /// <summary>
    /// Represents the removal of attributes.
    /// </summary>
    RemoveAttributes,

    /// <summary>
    /// Represents the addition of an annotation.
    /// </summary>
    AddAnnotation
}