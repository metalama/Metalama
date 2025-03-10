// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Advising;

internal static class TemplateMemberExtensions
{
    /// <summary>
    /// Returns <c>null</c> if the input <see cref="BoundTemplateMethod"/> does not represent a method with an implementation.
    /// </summary>
    public static BoundTemplateMethod? ExplicitlyImplementedOrNull( this BoundTemplateMethod? templateMethod )
        => templateMethod == null ? null : ((IMethodSymbol) templateMethod.TemplateMember.Symbol).IsAutoAccessor() ? null : templateMethod;
}