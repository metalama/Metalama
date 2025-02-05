// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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