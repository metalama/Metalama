// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// Provides names for overriden declarations.
/// </summary>
internal abstract class InjectionNameProvider
{
    internal abstract string GetOverrideName( INamedType targetType, AspectLayerId aspectLayer, IMember overriddenMember );

    internal abstract string GetInitializerName( INamedType targetType, AspectLayerId aspectLayer, IMember initializedMember );

    // TODO: Check why it is never used.
    // Resharper disable UnusedMember.Global

    internal abstract string GetInitializationName(
        INamedType targetType,
        AspectLayerId aspectLayer,
        IDeclaration targetDeclaration,
        InitializerKind reason );

    internal abstract TypeSyntax GetOverriddenByType( IAspectInstanceInternal aspect, IMember overriddenMember, SyntaxGenerationContext context );
}