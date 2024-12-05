// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal sealed class ConstructorInvoker : Invoker<IConstructor>, IConstructorInvoker
{
    public ConstructorInvoker( IConstructor constructor ) : base( constructor, InvokerOptions.Final, null ) { }

    public object Invoke( params object?[]? args )
    {
        if ( this.Member.IsStatic )
        {
            throw GeneralDiagnosticDescriptors.CannotInvokeStaticConstructor.CreateException( this.Member );
        }

        args ??= [];

        var parametersCount = this.Member.Parameters.Count;

        if ( parametersCount > 0 && this.Member.Parameters[parametersCount - 1].IsParams )
        {
            // The this.Declaration has a 'params' param.
            if ( args.Length < parametersCount - 1 )
            {
                throw GeneralDiagnosticDescriptors.MemberRequiresAtLeastNArguments.CreateException( (this.Member, parametersCount - 1, args.Length) );
            }
        }
        else if ( args.Length != parametersCount )
        {
            throw GeneralDiagnosticDescriptors.MemberRequiresNArguments.CreateException( (this.Member, parametersCount, args.Length) );
        }

        this.CheckInvocationOptionsAndTarget();

        return this.InvokeConstructor( args );
    }

    public object Invoke( IEnumerable<IExpression> args ) => this.Invoke( args.ToArray<object>() );

    private IExpression InvokeConstructor( object?[] args )
    {
        return new DelegateUserExpression(
            context =>
            {
                var type = context.SyntaxGenerator.TypeSyntax( this.Member.DeclaringType );

                var arguments = this.Member.GetArguments(
                    this.Member.Parameters,
                    TypedExpressionSyntaxImpl.FromValues( args, context ),
                    context.SyntaxGenerationContext );

                return CreateObjectCreationExpression( type, arguments, null );
            },
            this.Member.DeclaringType );
    }

    public IObjectCreationExpression CreateInvokeExpression()
        => new ObjectCreationExpression(
            this.Member,
            _ => Array.Empty<ExpressionSyntax>() );

    public IObjectCreationExpression CreateInvokeExpression( params object?[] args )
        => new ObjectCreationExpression(
            this.Member,
            context => TypedExpressionSyntaxImpl.FromValues( args, context ).SelectAsArray( tes => tes.Syntax ) );

    public IObjectCreationExpression CreateInvokeExpression( params IExpression[] args )
        => new ObjectCreationExpression(
            this.Member,
            context => args.SelectAsArray( arg => arg.ToExpressionSyntax( context ) ) );

    public IObjectCreationExpression CreateInvokeExpression( IEnumerable<IExpression> args )
        => new ObjectCreationExpression(
            this.Member,
            context => args.Select( arg => arg.ToExpressionSyntax( context ) ) );

    private static ExpressionSyntax CreateObjectCreationExpression(
        TypeSyntax type,
        IEnumerable<ArgumentSyntax> arguments,
        InitializerExpressionSyntax? initializerExpression )
    {
        var expression =
            ObjectCreationExpression(
                type,
                ArgumentList( SeparatedList( arguments ) ),
                initializerExpression );

        // Not using any aspect reference because these are not supported for constructor invocations.

        return expression;
    }

    private sealed class ObjectCreationExpression : UserExpression, IObjectCreationExpression
    {
        private readonly IConstructor _constructor;
        private readonly Func<SyntaxSerializationContext, IEnumerable<ExpressionSyntax>> _argumentFactory;

        public override IType Type => this._constructor.DeclaringType;

        public ObjectCreationExpression( IConstructor constructor, Func<SyntaxSerializationContext, IEnumerable<ExpressionSyntax>> argumentFactory )
        {
            this._constructor = constructor;
            this._argumentFactory = argumentFactory;
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
            => CreateObjectCreationExpression(
                syntaxSerializationContext.SyntaxGenerator.TypeSyntax( this._constructor.DeclaringType ),
                this._argumentFactory( syntaxSerializationContext )
                    .Select(
                        ( e, i ) =>
                            Argument(
                                NameColon( IdentifierName( this._constructor.Parameters[i].Name ) ),
                                this._constructor.Parameters[i].RefKind.InvocationRefKindToken(),
                                e ) ),
                null );

        public IExpression WithObjectInitializer( params (IFieldOrProperty FieldOrProperty, IExpression Value)[] initializationExpressions )
            => new ObjectCreationExpressionWithObjectInitializer(
                this._constructor,
                this._argumentFactory,
                initializationExpressions.SelectAsReadOnlyList( x => (x.FieldOrProperty.Name, x.Value) ) );

        public IExpression WithObjectInitializer( params (string FieldOrPropertyName, IExpression Value)[] initializationExpressions )
            => new ObjectCreationExpressionWithObjectInitializer( this._constructor, this._argumentFactory, initializationExpressions );
    }

    private sealed class ObjectCreationExpressionWithObjectInitializer : UserExpression
    {
        private readonly IConstructor _constructor;
        private readonly Func<SyntaxSerializationContext, IEnumerable<ExpressionSyntax>> _argumentFactory;
        private readonly IReadOnlyList<(string FieldOrPropertyName, IExpression Value)> _initializers;

        public override IType Type => this._constructor.DeclaringType;

        public ObjectCreationExpressionWithObjectInitializer(
            IConstructor constructor,
            Func<SyntaxSerializationContext, IEnumerable<ExpressionSyntax>> argumentFactory,
            IReadOnlyList<(string FieldOrPropertyName, IExpression Value)> initializers )
        {
            this._constructor = constructor;
            this._argumentFactory = argumentFactory;
            this._initializers = initializers;
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
            => CreateObjectCreationExpression(
                syntaxSerializationContext.SyntaxGenerator.TypeSyntax( this._constructor.DeclaringType ),
                this._argumentFactory( syntaxSerializationContext )
                    .Select(
                        ( e, i ) =>
                            Argument(
                                NameColon( IdentifierName( this._constructor.Parameters[i].Name ) ),
                                this._constructor.Parameters[i].RefKind.InvocationRefKindToken(),
                                e ) ),
                InitializerExpression(
                    SyntaxKind.ObjectInitializerExpression,
                    SeparatedList<ExpressionSyntax>(
                        this._initializers.SelectAsArray(
                            i =>
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName( i.FieldOrPropertyName ),
                                    i.Value.ToExpressionSyntax( syntaxSerializationContext ) ) ) ) ) );
    }
}