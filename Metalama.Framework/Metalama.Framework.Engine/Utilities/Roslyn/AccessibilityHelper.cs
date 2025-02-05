// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static class AccessibilityHelper
{
    private static readonly Func<IAssemblySymbol, IAssemblySymbol, bool>? _hasInternalAccessTo;

    static AccessibilityHelper()
    {
        _hasInternalAccessTo = GetHasInternalAccessToDelegate();
    }

    private static Func<IAssemblySymbol, IAssemblySymbol, bool>? GetHasInternalAccessToDelegate()
    {
        var accessCheckType = typeof(CSharpCompilation).Assembly.GetType( "Microsoft.CodeAnalysis.CSharp.AccessCheck" );

        if ( accessCheckType == null )
        {
            return null;
        }

        var publicAssemblySymbolType = typeof(CSharpCompilation).Assembly.GetType( "Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel.AssemblySymbol" );

        if ( publicAssemblySymbolType == null )
        {
            return null;
        }

        var underlyingAssemblySymbolProperty =
            publicAssemblySymbolType.GetProperty( "UnderlyingAssemblySymbol", BindingFlags.Instance | BindingFlags.NonPublic );

        if ( underlyingAssemblySymbolProperty == null )
        {
            return null;
        }

        var hasInternalAccessToMethod = accessCheckType.GetMethod( "HasInternalAccessTo", BindingFlags.Static | BindingFlags.NonPublic );

        if ( hasInternalAccessToMethod == null )
        {
            return null;
        }

        var fromParameter = Expression.Parameter( typeof(IAssemblySymbol), "from" );
        var toParameter = Expression.Parameter( typeof(IAssemblySymbol), "to" );

        var convertedFrom = Expression.Property( Expression.Convert( fromParameter, publicAssemblySymbolType ), underlyingAssemblySymbolProperty );
        var convertedTo = Expression.Property( Expression.Convert( toParameter, publicAssemblySymbolType ), underlyingAssemblySymbolProperty );

        var methodCall = Expression.Call( hasInternalAccessToMethod, convertedFrom, convertedTo );

        return Expression.Lambda<Func<IAssemblySymbol, IAssemblySymbol, bool>>( methodCall, fromParameter, toParameter ).Compile();
    }

    public static bool AreInternalsVisibleToImpl( this IAssemblySymbol toAssembly, IAssemblySymbol fromAssembly )
    {
        if ( _hasInternalAccessTo == null )
        {
            throw new AssertionFailedException();
        }

        return _hasInternalAccessTo( fromAssembly, toAssembly );
    }
}