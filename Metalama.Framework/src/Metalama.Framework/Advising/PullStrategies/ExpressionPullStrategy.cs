// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Advising.PullStrategies;

/// <remarks>
/// The expression is held in its durable form, because a pull strategy is aspect state and survives an edit. An
/// <see cref="IExpression"/> here held either its compilation or the syntax tree of its source file.
/// </remarks>
internal sealed class ExpressionPullStrategy : IPullStrategy
{
    private readonly IDurableExpression _expression;

    public ExpressionPullStrategy( IExpression expression ) : this( expression.ToDurable() ) { }

    private ExpressionPullStrategy( IDurableExpression expression )
    {
        this._expression = expression;
    }

    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember ) => PullAction.UseDurableExpression( this._expression );

    /// <remarks>
    /// The durable expression serializes itself, including its type and flags, so there is a single value to write.
    /// </remarks>
    [UsedImplicitly]
    private class Serializer : ReferenceTypeSerializer<ExpressionPullStrategy>
    {
        public override ExpressionPullStrategy CreateInstance( IArgumentsReader constructorArguments )
            => new( constructorArguments.GetValue<IDurableExpression>( "expression" )! );

        public override void SerializeObject( ExpressionPullStrategy obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( "expression", obj._expression );
        }
    }
}