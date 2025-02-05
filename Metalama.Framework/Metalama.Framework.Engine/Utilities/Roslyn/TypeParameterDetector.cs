// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static partial class TypeParameterDetector
{
    /// <summary>
    /// Gets the generic method or type defining type parameters referenced in a given type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IDeclaration? GetTypeContext( IType type ) => TypeVisitor.Instance.Visit( type )?.ContainingDeclaration;

    public static bool ReferencesTypeParameter( IType type ) => TypeVisitor.Instance.Visit( type ) != null;

    public static bool ReferencesTypeParameter( ITypeSymbol type ) => TypeSymbolVisitor.Instance.Visit( type );
}