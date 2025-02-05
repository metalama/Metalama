// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.References;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.TypedConstantKind;
using RoslynTypedConstant = Microsoft.CodeAnalysis.TypedConstant;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

internal static class TypedConstantExtensions
{
    public static TypedConstant ToOurTypedConstant( this RoslynTypedConstant constant, CompilationModel compilation )
    {
        var type = compilation.Factory.GetIType( constant.Type.AssertSymbolNotNull() );

        var value = constant.Kind switch
        {
            Primitive or TypedConstantKind.Enum => constant.Value,
            TypedConstantKind.Type => constant.Value == null ? null : compilation.Factory.GetIType( (ITypeSymbol) constant.Value ),
            TypedConstantKind.Array => constant.Values.IsDefault
                ? null
                : constant.Values.Select( x => ToOurTypedConstant( x, compilation ) ).ToImmutableArray(),
            _ => throw new ArgumentException( nameof(constant) )
        };

        return TypedConstant.CreateUnchecked( value, type );
    }

    public static TypedConstantRef ToOurTypedConstantRef( this RoslynTypedConstant constant, RefFactory refFactory )
    {
        var type = constant.Type.AssertSymbolNotNull().ToRef( refFactory );

        return constant.Kind switch
        {
            TypedConstantKind.Enum => new TypedConstantRef( constant.Value, type ),
            Primitive => new TypedConstantRef( constant.Value, default ),
            TypedConstantKind.Type => new TypedConstantRef(
                ((ITypeSymbol) constant.Value.AssertNotNull()).ToRef( refFactory ),
                type ),
            TypedConstantKind.Array => new TypedConstantRef(
                constant.Values.Select( x => ToOurTypedConstantRef( x, refFactory ) ).ToImmutableArray(),
                type ),
            _ => throw new ArgumentException( nameof(constant) )
        };
    }

    public static TypedConstantRef? ToRef( this TypedConstant? constant ) => constant?.ToRef();

    public static TypedConstantRef ToRef( this TypedConstant constant )
    {
        if ( !constant.IsInitialized )
        {
            return default;
        }
        else if ( constant.Value == null )
        {
            return new TypedConstantRef( null, constant.Type.ToRef() );
        }
        else if ( constant.IsArray )
        {
            return new TypedConstantRef( constant.Values.SelectAsImmutableArray( x => x.ToRef() ), constant.Type.ToRef() );
        }
        else
        {
            return new TypedConstantRef( constant.Value, constant.Type.ToRef() );
        }
    }
}