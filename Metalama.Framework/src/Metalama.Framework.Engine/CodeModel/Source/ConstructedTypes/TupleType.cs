// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes;

internal sealed class TupleType : SourceNamedType, ITupleType
{
    internal TupleType(
        INamedTypeSymbol typeSymbol,
        CompilationModel compilation,
        GenericContext? genericContextForSymbolMapping,
        IReadOnlyList<string>? names = null ) : base(
        typeSymbol,
        compilation,
        genericContextForSymbolMapping,
        new TupleTypeImpl( typeSymbol, compilation, genericContextForSymbolMapping, names ) ) { }

    private new TupleTypeImpl Implementation => (TupleTypeImpl) base.Implementation;

    public IReadOnlyList<ITupleElement> TupleElements
    {
        get
        {
            this.OnUsingDeclaration();

            return this.Implementation.TupleElements;
        }
    }

    public int TupleLength
    {
        get
        {
            this.OnUsingDeclaration();

            return this.Implementation.TupleLength;
        }
    }

    public IExpression CreateCreateInstanceExpression( params IReadOnlyCollection<IExpression> values )
    {
        if ( values.Count != this.TupleLength )
        {
            throw new ArgumentOutOfRangeException( nameof(values), "The number of values must equal the length of the tuple type." );
        }
        
        return new CreateTupleExpression(
            this,
            context => values.SelectAsReadOnlyCollection( x => (TypedExpressionSyntaxImpl) ((IUserExpression) x).ToTypedExpressionSyntax( context ) ).ToReadOnlyList() );
    }

    public IExpression CreateCreateInstanceExpression( params object?[] values )
    {
        if ( values.Length != this.TupleLength )
        {
            throw new ArgumentOutOfRangeException( nameof(values), "The number of values must equal the length of the tuple type." );
        }

        return new CreateTupleExpression( this, context => TypedExpressionSyntaxImpl.FromValues( values, context ).AssertNotNull() );
    }

    public IExpression CreateCreateToArrayExpression( IExpression tuple ) => throw new NotImplementedException();

    public IExpression CreateCreateToArrayExpression( object tuple ) => throw new NotImplementedException();

    // We explicitly don't want to expose the source because we represent all equivalent instances (equivalent
    // by type and element names) as the same object and the same reference. Therefore, the source is non-deterministic.
    public override ImmutableArray<SourceReference> Sources => ImmutableArray<SourceReference>.Empty;
}