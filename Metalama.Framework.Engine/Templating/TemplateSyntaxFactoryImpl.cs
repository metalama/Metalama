// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Templating.Statements;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed partial class TemplateSyntaxFactoryImpl : ITemplateSyntaxFactory
    {
        private readonly TemplateExpansionContext _templateExpansionContext;
        private readonly ObjectReaderFactory _objectReaderFactory;

        public TemplateSyntaxFactoryImpl( TemplateExpansionContext templateExpansionContext )
        {
            this._templateExpansionContext = templateExpansionContext;
            this.SyntaxSerializationContext = templateExpansionContext.SyntaxSerializationContext;
            this._objectReaderFactory = templateExpansionContext.ServiceProvider.GetRequiredService<ObjectReaderFactory>();
        }

        public SyntaxSerializationContext SyntaxSerializationContext { get; }

        public ICompilation Compilation => this._templateExpansionContext.Compilation.AssertNotNull();

        public void AddStatement( List<StatementOrTrivia> list, StatementSyntax? statement ) => list.Add( new StatementOrTrivia( statement ) );

        public void AddStatement( List<StatementOrTrivia> list, IStatement statement )
            => list.Add( new StatementOrTrivia( ((IStatementImpl) statement).GetSyntax( this ) ) );

        public void AddStatement( List<StatementOrTrivia> list, IExpression expression )
        {
            var statement = SyntaxFactory.ExpressionStatement( expression.ToExpressionSyntax( this.SyntaxSerializationContext ).RemoveParenthesis() );

            list.Add( new StatementOrTrivia( statement ) );
        }

        public void AddStatement( List<StatementOrTrivia> list, string statement )
            => list.Add( new StatementOrTrivia( SyntaxFactoryEx.ParseStatementSafe( statement ) ) );

        public void AddComments( List<StatementOrTrivia> list, params string?[]? comments )
        {
            IEnumerable<SyntaxTrivia> CreateTrivia( string comment )
            {
                if ( comment.ContainsOrdinal( '\n' ) || comment.ContainsOrdinal( '\r' ) )
                {
                    yield return this._templateExpansionContext.SyntaxGenerationContext.ElasticEndOfLineTrivia;
                    yield return SyntaxFactory.Comment( "/* " + comment + " */" );
                    yield return this._templateExpansionContext.SyntaxGenerationContext.ElasticEndOfLineTrivia;
                }
                else
                {
                    yield return this._templateExpansionContext.SyntaxGenerationContext.ElasticEndOfLineTrivia;
                    yield return SyntaxFactory.Comment( "// " + comment );
                    yield return this._templateExpansionContext.SyntaxGenerationContext.ElasticEndOfLineTrivia;
                }
            }

            if ( comments != null )
            {
                list.Add( new StatementOrTrivia( SyntaxFactory.TriviaList( comments.WhereNotNull().SelectMany( CreateTrivia ) ) ) );
            }
        }

        public StatementSyntax? ToStatement( ExpressionSyntax expression )
        {
            if ( expression.Kind() == SyntaxKind.NullLiteralExpression )
            {
                // This is a special hack used when e.g. `meta.Proceed();` is invoked on a non-existing method.
                // Such call would be translated to the `null` expression, and we need to ignore it.
                return null;
            }
            else
            {
                return SyntaxFactory.ExpressionStatement( expression );
            }
        }

        public SyntaxList<StatementSyntax> ToStatementList( List<StatementOrTrivia> list )
        {
            var statementList = new List<StatementSyntax>( list.Count );

            // List of trivia added by previous items in the list, and not yet added to a statement.
            // This is used when trivia are added before the first statement and after a first newline withing the trivia block.
            var nextLeadingTrivia = SyntaxTriviaList.Empty;

            foreach ( var statementOrTrivia in list )
            {
                switch ( statementOrTrivia.Content )
                {
                    case StatementSyntax statement:
                        // Add
                        if ( nextLeadingTrivia.Count > 0 )
                        {
                            statement = statement.WithLeadingTrivia( nextLeadingTrivia.AddRange( statement.GetLeadingTrivia() ) );
                            nextLeadingTrivia = SyntaxTriviaList.Empty;
                        }

                        statementList.Add( statement );

                        break;

                    case SyntaxTriviaList trivia:
                        if ( statementList.Count == 0 )
                        {
                            // Add the trivia as the leading trivia of the next statement.
                            nextLeadingTrivia = nextLeadingTrivia.AddRange( trivia );
                        }
                        else
                        {
                            var previousStatement = statementList[^1];

                            // TODO: Optimize the lookup for newline.
                            if ( previousStatement.GetTrailingTrivia().Any( x => x.IsKind( SyntaxKind.EndOfLineTrivia ) ) )
                            {
                                nextLeadingTrivia = nextLeadingTrivia.AddRange( trivia );
                            }
                            else
                            {
                                var triviaUpToFirstNewLine = SyntaxTriviaList.Empty;
                                var triviaAfterFirstNewLine = SyntaxTriviaList.Empty;
                                var isAfterFirstNewLine = false;

                                // Split the trivia after the first newline.
                                foreach ( var t in trivia )
                                {
                                    if ( !isAfterFirstNewLine )
                                    {
                                        triviaUpToFirstNewLine = triviaUpToFirstNewLine.Add( t );

                                        if ( t.IsKind( SyntaxKind.EndOfLineTrivia ) )
                                        {
                                            isAfterFirstNewLine = true;
                                        }
                                    }
                                    else
                                    {
                                        triviaAfterFirstNewLine = triviaAfterFirstNewLine.Add( t );
                                    }
                                }

                                statementList[^1] =
                                    previousStatement
                                        .WithTrailingTrivia( previousStatement.GetTrailingTrivia().AddRange( triviaUpToFirstNewLine ) );

                                nextLeadingTrivia = triviaAfterFirstNewLine;
                            }
                        }

                        break;

                    default:
                        continue;
                }
            }

            // If there is any trivia left, we need to generate a dummy statement with the trivia (will be removed later).
            if ( nextLeadingTrivia.Count > 0 )
            {
                statementList.Add( SyntaxFactory.EmptyStatement().WithoutTrailingTrivia().WithLeadingTrivia( nextLeadingTrivia ) );
            }

            return SyntaxFactory.List( statementList );
        }

        public SyntaxKind Boolean( bool value ) => value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression;

        // This method is called when the expression of 'return' is a non-dynamic expression.
        public StatementSyntax ReturnStatement( ExpressionSyntax? returnExpression )
            => this._templateExpansionContext.CreateReturnStatement( returnExpression, false );

        // This overload is called when the expression of 'return' is a compile-time expression returning a dynamic value.
        public StatementSyntax DynamicReturnStatement( IUserExpression returnExpression, bool awaitResult )
            => this._templateExpansionContext.CreateReturnStatement( returnExpression, awaitResult );

        public StatementSyntax DynamicLocalAssignment( IdentifierNameSyntax identifier, SyntaxKind kind, IUserExpression? expression, bool awaitResult )
        {
            if ( expression == null )
            {
                return SyntaxFactoryEx.EmptyStatement;
            }
            else if ( expression.Type.Equals( SpecialType.Void ) )
            {
                if ( kind != SyntaxKind.SimpleAssignmentExpression )
                {
                    throw new InvalidOperationException(
                        $"Templates using context-dependent compound assignments (e.g. 'x += meta.Proceed()') cannot be expanded when the right side " +
                        $"expression is of type 'void'. Use a simple assignment ('x = meta.Proceed') instead." );
                }

                return SyntaxFactory.ExpressionStatement( expression.ToExpressionSyntax( this.SyntaxSerializationContext ).RemoveParenthesis() );
            }
            else if ( awaitResult && expression.Type.GetAsyncInfo().ResultType.Equals( SpecialType.Void ) )
            {
                return
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AwaitExpression( expression.ToExpressionSyntax( this.SyntaxSerializationContext ) )
                            .RemoveParenthesis() );
            }
            else
            {
                return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                            kind,
                            identifier,
                            awaitResult
                                ? SyntaxFactory.AwaitExpression( expression.ToExpressionSyntax( this.SyntaxSerializationContext ).RemoveParenthesis() )
                                : expression.ToExpressionSyntax( this.SyntaxSerializationContext ) )
                        .RemoveParenthesis() );
            }
        }

        // This overload is called when the value of a local is a dynamic expression but not a compile-time expression returning a dynamic value.
        public StatementSyntax DynamicLocalDeclaration(
            TypeSyntax type,
            SyntaxToken identifier,
            IUserExpression? value,
            bool awaitResult )
        {
            if ( value == null )
            {
                // Don't know how to process this case. Find an example first.
                throw new AssertionFailedException( "The expression should not be null." );
            }

            var runtimeExpression = value.ToExpressionSyntax( this.SyntaxSerializationContext );

            if ( value.Type.Equals( SpecialType.Void )
                 || (awaitResult && value.Type.GetAsyncInfo().ResultType.Equals( SpecialType.Void )) )
            {
                // If the method is void, we invoke the method as a statement (so we don't lose the side effect) and we define a local that
                // we assign to the default value. The local is necessary because it may be referenced later.
                TypeSyntax variableType;
                ExpressionSyntax variableValue;

                switch ( type )
                {
                    case IdentifierNameSyntax { IsVar: true }:
                        variableType = this.SyntaxSerializationContext.SyntaxGenerator.TypeSyntax( Microsoft.CodeAnalysis.SpecialType.System_Object );

                        variableValue = SyntaxFactoryEx.Null;

                        break;

                    default:
                        variableType = type;
                        variableValue = SyntaxFactory.DefaultExpression( variableType );

                        break;
                }

                var localDeclarationStatement = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(
                            variableType,
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(
                                    identifier,
                                    null,
                                    SyntaxFactory.EqualsValueClause( variableValue ) ) ) ) )
                    .WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation );

                // Special case for default(void), which cannot be an expression statement.
                if ( value is TypedDefaultUserExpression )
                {
                    return localDeclarationStatement;
                }

                return SyntaxFactory.Block(
                        awaitResult
                            ? SyntaxFactory.ExpressionStatement( SyntaxFactory.AwaitExpression( runtimeExpression.RemoveParenthesis() ) )
                            : SyntaxFactory.ExpressionStatement( runtimeExpression.RemoveParenthesis() ),
                        localDeclarationStatement )
                    .WithFlattenBlockAnnotation();
            }
            else
            {
                return SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        type,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                identifier,
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    awaitResult
                                        ? SyntaxFactory.AwaitExpression( runtimeExpression )
                                        : runtimeExpression ) ) ) ) );
            }
        }

        public TypedExpressionSyntax DynamicMemberAccessExpression( IUserExpression userExpression, string member )
        {
            if ( userExpression is UserReceiver dynamicMemberAccess )
            {
                return dynamicMemberAccess.CreateMemberAccessExpression( member );
            }

            var expression = userExpression.ToExpressionSyntax( this.SyntaxSerializationContext );

            return new TypedExpressionSyntaxImpl(
                SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        expression,
                        SyntaxFactory.IdentifierName( member ) )
                    .WithSimplifierAnnotationIfNecessary( this.SyntaxSerializationContext.SyntaxGenerationContext ),
                this.SyntaxSerializationContext.CompilationModel );
        }

        public SyntaxToken GetUniqueIdentifier( string hint )
            => SyntaxFactory.Identifier( this._templateExpansionContext.LexicalScope.GetUniqueIdentifier( hint ) );

        public ExpressionSyntax Serialize<T>( T o )
            => this._templateExpansionContext.SyntaxSerializationService.Serialize( o, this.SyntaxSerializationContext );

        public T AddSimplifierAnnotations<T>( T node )
            where T : SyntaxNode
            => node.WithSimplifierAnnotation();

        public AnonymousFunctionExpressionSyntax SimplifyAnonymousFunction<T>( T node )
            where T : AnonymousFunctionExpressionSyntax
            => node switch
            {
                SimpleLambdaExpressionSyntax { Block.Statements: [ExpressionStatementSyntax expressionStatement] } simpleLambdaExpression =>
                    simpleLambdaExpression.Update(
                        simpleLambdaExpression.AttributeLists,
                        simpleLambdaExpression.Modifiers,
                        simpleLambdaExpression.Parameter,
                        SyntaxFactory.Token( SyntaxKind.EqualsGreaterThanToken ),
                        null,
                        expressionStatement.Expression ),
                SimpleLambdaExpressionSyntax { Block.Statements: [ThrowStatementSyntax { Expression: not null } throwStatement] } simpleLambdaExpression =>
                    simpleLambdaExpression.Update(
                        simpleLambdaExpression.AttributeLists,
                        simpleLambdaExpression.Modifiers,
                        simpleLambdaExpression.Parameter,
                        SyntaxFactory.Token( SyntaxKind.EqualsGreaterThanToken ),
                        null,
                        SyntaxFactory.ThrowExpression( throwStatement.ThrowKeyword, throwStatement.Expression! ) ),
                SimpleLambdaExpressionSyntax { Block.Statements: [BlockSyntax { Statements.Count: 1 } nestedBlock] } simpleLambdaExpression
                    => this.SimplifyAnonymousFunction( simpleLambdaExpression.WithBlock( nestedBlock ) ),
                ParenthesizedLambdaExpressionSyntax { Block.Statements: [ExpressionStatementSyntax expressionStatement] } simpleLambdaExpression =>
                    simpleLambdaExpression.Update(
                        simpleLambdaExpression.AttributeLists,
                        simpleLambdaExpression.Modifiers,
                        simpleLambdaExpression.ParameterList,
                        SyntaxFactory.Token( SyntaxKind.EqualsGreaterThanToken ),
                        null,
                        expressionStatement.Expression ),
                ParenthesizedLambdaExpressionSyntax { Block.Statements: [ThrowStatementSyntax throwStatement] } simpleLambdaExpression =>
                    simpleLambdaExpression.Update(
                        simpleLambdaExpression.AttributeLists,
                        simpleLambdaExpression.Modifiers,
                        simpleLambdaExpression.ParameterList,
                        SyntaxFactory.Token( SyntaxKind.EqualsGreaterThanToken ),
                        null,
                        SyntaxFactory.ThrowExpression( throwStatement.ThrowKeyword, throwStatement.Expression! ) ),
                ParenthesizedLambdaExpressionSyntax { Block.Statements: [BlockSyntax { Statements.Count: 1 } nestedBlock] } simpleLambdaExpression
                    => this.SimplifyAnonymousFunction( simpleLambdaExpression.WithBlock( nestedBlock ) ),

                _ => node
            };

        public ExpressionSyntax RenderInterpolatedString( InterpolatedStringExpressionSyntax interpolatedString )
            => this.SyntaxSerializationContext.SyntaxGenerator.RenderInterpolatedString( interpolatedString );

        public ExpressionSyntax ConditionalExpression( ExpressionSyntax condition, ExpressionSyntax whenTrue, ExpressionSyntax whenFalse )
        {
            // We try simplify the conditional expression when the result is known when the template is expanded.

            switch ( condition.Kind() )
            {
                case SyntaxKind.TrueLiteralExpression:
                    return whenTrue;

                case SyntaxKind.FalseLiteralExpression:
                    return whenFalse;

                default:
                    return SyntaxFactory.ConditionalExpression( condition, whenTrue, whenFalse );
            }
        }

        public IUserExpression Proceed( string methodName ) => this._templateExpansionContext.Proceed( methodName );

        public IUserExpression ConfigureAwait( IUserExpression expression, bool continueOnCapturedContext )
            => TemplateExpansionContext.ConfigureAwait( expression, continueOnCapturedContext );

        public ExpressionSyntax GetDynamicSyntax( object? expression )
        {
            switch ( expression )
            {
                case IExpression dynamicExpression:
                    {
                        var typedExpression = dynamicExpression.ToTypedExpressionSyntax( this.SyntaxSerializationContext );

                        var syntax = typedExpression.Syntax;

                        // TODO: Fix the data flow. We have a UserExpression, generate an ExpressionSyntax, which may then be
                        // wrapped again into a UserExpression.

                        if ( typedExpression.ExpressionType != null )
                        {
                            syntax = TypeAnnotationMapper.AddExpressionTypeAnnotation( syntax, typedExpression.ExpressionType );
                        }

                        if ( typedExpression.IsReferenceable != null )
                        {
                            syntax = TypeAnnotationMapper.AddIsExpressionReferenceableAnnotation( syntax, typedExpression.IsReferenceable.Value );
                        }

                        return syntax;
                    }

                case ExpressionSyntax expressionSyntax:
                    // TODO: Fix the data flow. This method call was redundant.
                    return expressionSyntax;

                default:
                    if ( this._templateExpansionContext.SyntaxSerializationService.TrySerialize(
                            expression,
                            this._templateExpansionContext.SyntaxSerializationContext,
                            out var result ) )
                    {
                        return result;
                    }

                    throw new ArgumentOutOfRangeException( nameof(expression), $"Don't know how to extract the syntax from '{expression}'." );
            }
        }

        public TypedExpressionSyntax GetTypedExpression( IExpression expression )
            => expression.ToTypedExpressionSyntax( this.SyntaxSerializationContext );

        public TypedExpressionSyntax RunTimeExpression( ExpressionSyntax syntax, string? type = null )
        {
            var syntaxSerializationContext = this.SyntaxSerializationContext;

            var expressionType = type != null
                ? syntaxSerializationContext.CompilationModel.SerializableTypeIdResolver.ResolveId(
                    new SerializableTypeId( type ),
                    this._templateExpansionContext.TemplateGenericArguments )
                : null;

            return new TypedExpressionSyntaxImpl( syntax, expressionType, syntaxSerializationContext.CompilationModel );
        }

        public TypedExpressionSyntax RunTimeExpression( IUserExpression syntax, string? type = null )
            => syntax.ToTypedExpressionSyntax( this.SyntaxSerializationContext );

        public IUserExpression GetUserExpression( object value ) => this.GetUserExpression( value, null );

        private IUserExpression GetUserExpression( object value, string? type )
        {
            switch ( value )
            {
                case IExpression expression:
                    return expression.ToUserExpression();

                case ExpressionSyntax expression:
                    // TODO: Clean the code generation, as we should not get here.
                    if ( !TypeAnnotationMapper.TryFindExpressionTypeFromAnnotation(
                            expression,
                            this.Compilation.GetCompilationModel(),
                            out var expressionType ) )
                    {
                        if ( type != null )
                        {
                            expressionType = new SerializableTypeId( type ).Resolve(
                                this._templateExpansionContext.Compilation.AssertNotNull(),
                                this._templateExpansionContext.TemplateGenericArguments );
                        }
                        else
                        {
                            expressionType = this._templateExpansionContext.Compilation.AssertNotNull().Cache.SystemObjectType;
                        }
                    }

                    return new SyntaxUserExpression( expression, expressionType );

                case TypedExpressionSyntax typedExpressionSyntax:
                    return typedExpressionSyntax.ToUserExpression( this.Compilation );

                default:
                    throw new AssertionFailedException( $"Don't know how to convert a {value.GetType()} to a UserExpression" );
            }
        }

        public ExpressionSyntax SuppressNullableWarningExpression( ExpressionSyntax operand, string? type )
        {
            TypeAnnotationMapper.TryFindExpressionTypeFromAnnotation(
                operand,
                this.SyntaxSerializationContext.CompilationModel,
                out var expressionType );

            if ( expressionType == null && type != null )
            {
                expressionType = new SerializableTypeId( type ).Resolve(
                    this._templateExpansionContext.Compilation.AssertNotNull(),
                    this._templateExpansionContext.TemplateGenericArguments );
            }

            return this._templateExpansionContext.SyntaxGenerator.SuppressNullableWarningExpression( operand, expressionType );
        }

        public IUserExpression SuppressNullableWarningUserExpression( object operand, string? type )
        {
            var userExpression = this.GetUserExpression( operand, type );

            return new SuppressNullableWarningUserExpression( userExpression );
        }

        public ExpressionSyntax ConditionalAccessExpression( ExpressionSyntax expression, ExpressionSyntax whenNotNullExpression )
        {
            TypeAnnotationMapper.TryFindExpressionTypeFromAnnotation( expression, this.SyntaxSerializationContext.CompilationModel, out var type );

            return type?.IsNullable != false
                ? SyntaxFactory.ConditionalAccessExpression( expression, whenNotNullExpression )
                : (ExpressionSyntax) new RemoveConditionalAccessRewriter( expression ).Visit( whenNotNullExpression )!;
        }

        public ExpressionSyntax StringLiteralExpression( string? value ) => SyntaxFactoryEx.LiteralExpression( value );

        public TypeOfExpressionSyntax TypeOf( string typeOfString, Dictionary<string, TypeSyntax>? substitutions )
        {
            var typeOfExpression = (TypeOfExpressionSyntax) SyntaxFactoryEx.ParseExpressionSafe( typeOfString );

            if ( substitutions is { Count: > 0 } )
            {
                var rewriter = new SerializedTypeOfRewriter( substitutions );
                typeOfExpression = (TypeOfExpressionSyntax) rewriter.Visit( typeOfExpression )!;
            }

            return typeOfExpression;
        }

        public InterpolationSyntax FixInterpolationSyntax( InterpolationSyntax interpolation ) => InterpolationSyntaxHelper.Fix( interpolation );

        public ITemplateSyntaxFactory ForLocalFunction( string returnType, Dictionary<string, IType> genericArguments, bool isAsync = false )
        {
            var returnTypeSymbol = new SerializableTypeId( returnType ).Resolve( this._templateExpansionContext.Compilation.AssertNotNull(), genericArguments );

            return new TemplateSyntaxFactoryImpl( this._templateExpansionContext.ForLocalFunction( new LocalFunctionInfo( returnTypeSymbol, isAsync ) ) );
        }

        private BlockSyntax InvokeTemplate( string templateName, TemplateProvider templateProvider, IObjectReader args )
        {
            var (templateClass, templateMember) = this.GetTemplateDescription( templateName, templateProvider );

            var context = this._templateExpansionContext.ForTemplate( templateMember, templateProvider );
            var templateArguments = templateMember.ArgumentsForCalledTemplate( args );

            // Add ITemplateSyntaxFactory as the first template argument.
            var allArguments = new object?[templateArguments.Length + 1];
            allArguments[0] = this.ForTemplate( templateName, templateProvider );
            TemplateDriver.CopyTemplateArguments( templateArguments, allArguments, 1, this._templateExpansionContext.SyntaxGenerationContext );

            var compiledTemplateMethodInfo = templateClass.GetCompiledTemplateMethodInfo( templateMember.Symbol );

            return compiledTemplateMethodInfo.Invoke( templateProvider.Object ?? context.TemplateProvider.Object, allArguments )
                .AssertNotNull()
                .AssertCast<BlockSyntax>();
        }

        public BlockSyntax InvokeTemplate( string templateName, object? templateInstanceOrType = null, object? args = null )
            => this.InvokeTemplate( templateName, GetTemplateProvider( templateInstanceOrType ), this._objectReaderFactory.GetReader( args ) );

        public BlockSyntax InvokeTemplate( TemplateInvocation templateInvocation, object? args = null )
        {
            var invocationArgs = this._objectReaderFactory.GetReader( templateInvocation.Arguments );
            var directArgs = this._objectReaderFactory.GetReader( args );

            return this.InvokeTemplate(
                templateInvocation.TemplateName,
                templateInvocation.TemplateProvider,
                ObjectReader.Merge( invocationArgs, directArgs ) );
        }

        private (TemplateClass TemplateClass, TemplateMember<IMethod> TemplateMember) GetTemplateDescription(
            string templateName,
            TemplateProvider templateProvider )
        {
            if ( templateProvider == this._templateExpansionContext.TemplateProvider )
            {
                templateProvider = default;
            }

            var templateClass = this._templateExpansionContext.GetTemplateClass( templateProvider );
            var templateMemberRef = TemplateNameValidator.ValidateTemplateName( templateClass, templateName, TemplateKind.Default, required: true )!.Value;

            var templateMember = templateMemberRef.GetTemplateMember<IMethod>(
                this.Compilation.GetCompilationModel(),
                this._templateExpansionContext.ServiceProvider,
                templateProvider,
                this._templateExpansionContext.MetaApi.Tags );

            return (templateClass, templateMember);
        }

        private ITemplateSyntaxFactory ForTemplate( string templateName, TemplateProvider templateProvider )
        {
            var templateMember = this.GetTemplateDescription( templateName, templateProvider ).TemplateMember;

            var context = this._templateExpansionContext.ForTemplate( templateMember, templateProvider );

            context.CheckTemplateLanguageVersion( context, templateMember );

            return context.SyntaxFactory;
        }

        public ITemplateSyntaxFactory ForTemplate( string templateName, object? templateInstanceOrType )
        {
            var templateProvider = GetTemplateProvider( templateInstanceOrType );

            return this.ForTemplate( templateName, templateProvider );
        }

        private static TemplateProvider GetTemplateProvider( object? templateInstanceOrType )
            => templateInstanceOrType switch
            {
                null => default,
                TemplateProvider templateProvider => templateProvider,
                Type type => TemplateProvider.FromTypeUnsafe( type ),
                ITemplateProvider instance => TemplateProvider.FromInstance( instance ),
                _ => throw new AssertionFailedException()
            };

        public TemplateTypeArgument TemplateTypeArgument( string name, Type type )
            => TemplateTypeArgumentFactory.Create(
                TypeFactory.Implementation.GetTypeByReflectionType( type ),
                name,
                this._templateExpansionContext.SyntaxGenerationContext );

        public IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, IType type, ExpressionSyntax? expression )
        {
            var localVariableName = this._templateExpansionContext.LexicalScope.GetUniqueIdentifier( name );

            var variableType = this.SyntaxSerializationContext.SyntaxGenerator.TypeSyntax( type );

            var declarator = SyntaxFactory.VariableDeclarator( localVariableName );

            if ( expression != null )
            {
                declarator = declarator.WithInitializer( SyntaxFactory.EqualsValueClause( expression ) );
            }

            var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                default,
                default,
                SyntaxFactory.VariableDeclaration(
                    variableType,
                    SyntaxFactory.SingletonSeparatedList( declarator ) ) );

            statementList.Add( new StatementOrTrivia( variableDeclaration ) );

            return new SyntaxUserExpression( SyntaxFactory.IdentifierName( localVariableName ), type, true, true );
        }

        public IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, Type type, ExpressionSyntax? expression )
            => this.DefineLocalVariable(
                statementList,
                name,
                this._templateExpansionContext.Compilation.AssertNotNull().Factory.GetTypeByReflectionType( type ),
                expression );

        public IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, IType type )
            => this.DefineLocalVariable( statementList, name, type, default(ExpressionSyntax) );

        public IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, Type type )
            => this.DefineLocalVariable( statementList, name, type, default(ExpressionSyntax) );

        public IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, IType type, IExpression? expression )
            => this.DefineLocalVariable( statementList, name, type, expression?.ToExpressionSyntax( this.SyntaxSerializationContext, type ) );

        public IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, Type type, IExpression? expression )
        {
            var itype = this._templateExpansionContext.Compilation.AssertNotNull().Factory.GetTypeByReflectionType( type );

            return this.DefineLocalVariable( statementList, name, itype, expression );
        }

        public IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, ExpressionSyntax expression )
        {
            if ( !TypeAnnotationMapper.TryFindExpressionTypeFromAnnotation(
                    expression,
                    this._templateExpansionContext.Compilation.AssertNotNull(),
                    out var expressionType ) )
            {
                expressionType = this._templateExpansionContext.Compilation.Cache.SystemObjectType.ToNullable();
            }

            return this.DefineLocalVariableVar( statementList, name, expression, expressionType );
        }

        private IExpression DefineLocalVariableVar( List<StatementOrTrivia> statementList, string name, ExpressionSyntax expression, IType expressionType )
        {
            var localVariableName = this._templateExpansionContext.LexicalScope.GetUniqueIdentifier( name );

            var declarator = SyntaxFactory.VariableDeclarator( localVariableName )
                .WithInitializer( SyntaxFactory.EqualsValueClause( expression ) );

            var varType = SyntaxFactory.IdentifierName(
                SyntaxFactory.Identifier(
                    default,
                    SyntaxKind.VarKeyword,
                    "var",
                    "var",
                    default ) );

            var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                default,
                default,
                SyntaxFactory.VariableDeclaration(
                    varType,
                    SyntaxFactory.SingletonSeparatedList( declarator ) ) );

            statementList.Add( new StatementOrTrivia( variableDeclaration ) );

            return new SyntaxUserExpression( SyntaxFactory.IdentifierName( localVariableName ), expressionType, true, true );
        }

        public IExpression DefineLocalVariable( List<StatementOrTrivia> statementList, string name, IExpression expression )
            => this.DefineLocalVariableVar( statementList, name, expression.ToExpressionSyntax( this.SyntaxSerializationContext ), expression.Type );

        public ExpressionSyntax ConvertToExpressionSyntax( object value )
            => value switch
            {
                IUserExpression userExpression => userExpression.ToExpressionSyntax( this.SyntaxSerializationContext ),
                ExpressionSyntax expression => expression,
                TypedExpressionSyntax typedExpressionSyntax => typedExpressionSyntax.Syntax,
                _ => throw new AssertionFailedException( $"Unexpected expression type: {value.GetType()}" )
            };
    }
}