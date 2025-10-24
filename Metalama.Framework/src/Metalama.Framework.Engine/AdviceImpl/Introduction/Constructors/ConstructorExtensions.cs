// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal static class ConstructorExtensions
{
    /// <summary>
    /// Determines if a constructor can be called in a constructor chain (as <c>base</c>) from an outside assembly.
    /// </summary>
    public static bool CanBeChainedFromOutsideAssembly( this IConstructor constructor )
        => constructor.DeclaringType is { IsReferenceType: true, IsSealed: false } && constructor.IsAccessibleFromOutsideAssembly();
}