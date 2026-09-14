// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// Represents a transformation that introduces a declaration based on a <see cref="IDeclarationBuilder"/>, but does not
/// represent an override.
/// </summary>
internal interface IIntroduceDeclarationTransformation : ITransformation
{
    DeclarationBuilderData DeclarationBuilderData { get; }

    /// <summary>
    /// Gets a value indicating whether the compiler synthesizes the declaration from the declaration of the type that
    /// contains it, so that this transformation registers it in the code model and nothing emits syntax for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both emitters skip such a transformation: <c>LinkerInjectionStep</c> at build time and
    /// <c>DesignTimeSyntaxTreeGenerator</c> at design time. Section 4.2 of
    /// <c>Metalama.Framework/docs/introducing-types.md</c> states the rule and names the members concerned.
    /// </para>
    /// </remarks>
    bool IsCompilerSynthesized { get; }
}