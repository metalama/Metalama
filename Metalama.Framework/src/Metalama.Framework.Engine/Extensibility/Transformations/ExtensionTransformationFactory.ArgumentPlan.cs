// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Extensibility.Transformations
{
    public sealed partial class ExtensionTransformationFactory
    {
        /// <summary>
        /// Creates the plan of the argument list of a redirected invocation from the arguments of the request, and validates it against the parameters
        /// of the target method and the arguments of the call site.
        /// </summary>
        private static ImmutableArray<CallSiteArgumentPlanItem> CreateArgumentPlan(
            InvocationRedirectionRequest request,
            IInvocationOperation operation,
            bool isReducedExtensionCall,
            bool hasReceiverValue,
            SyntaxGenerationContext context )
        {
            var callSite = request.CallSite;
            var targetParameters = request.Target.Method.Parameters;
            var parameterOffset = request.ReceiverMode == CallSiteReceiverMode.Drop ? 0 : 1;

            if ( request.Arguments.Length > targetParameters.Count - parameterOffset )
            {
                throw new ArgumentException( $"The argument list has more elements than the parameters of '{request.Target.Method}'.", nameof(request) );
            }

            // Map each parameter of the source method to the index of its argument in the source argument list.
            var sourceArgumentIndices = new Dictionary<int, int>();
            var sourceArgumentSyntaxes = callSite.ArgumentList.Arguments;

            foreach ( var argument in operation.Arguments )
            {
                if ( argument is { ArgumentKind: ArgumentKind.Explicit, Parameter: { } parameter, Syntax: ArgumentSyntax argumentSyntax } )
                {
                    sourceArgumentIndices[parameter.Ordinal] = sourceArgumentSyntaxes.IndexOf( argumentSyntax );
                }
            }

            // The elements of an expanded params argument are the source arguments that no parameter maps. They are written last.
            var expandedParams = operation.Arguments.FirstOrDefault( a => a.ArgumentKind is ArgumentKind.ParamArray or ArgumentKind.ParamCollection );

            var expandedElements = expandedParams == null
                ? ImmutableArray<int>.Empty
                : Enumerable.Range( 0, sourceArgumentSyntaxes.Count ).Where( i => !sourceArgumentIndices.ContainsValue( i ) ).ToImmutableArray();

            var usedSourceArguments = new HashSet<int>();
            var receiverUsed = false;
            var names = new HashSet<string>( StringComparer.Ordinal );
            var items = new List<(int Order, CallSiteArgumentPlanItem Item, IParameter Parameter, IType? CastType)>();

            for ( var i = 0; i < request.Arguments.Length; i++ )
            {
                var argument = request.Arguments[i];
                var name = argument.Name ?? targetParameters[i + parameterOffset].Name;

                // The linker writes the argument with this name, so the parameter of this name receives it.
                var receivingParameter = targetParameters.FirstOrDefault( p => p.Name == name ) ?? targetParameters[i + parameterOffset];

                if ( !names.Add( name ) )
                {
                    throw new ArgumentException( $"The argument list names the parameter '{name}' twice.", nameof(request) );
                }

                if ( argument.CastType != null && (argument.Kind != RedirectedArgumentKind.SourceArgument || (isReducedExtensionCall && argument.ParameterOrdinal == 0)) )
                {
                    throw new ArgumentException( $"The argument '{name}' cannot be cast, because only a source argument other than the receiver can be cast.", nameof(request) );
                }

                switch ( argument.Kind )
                {
                    case RedirectedArgumentKind.SourceReceiver:
                        if ( request.ReceiverMode != CallSiteReceiverMode.Drop || receiverUsed )
                        {
                            throw new ArgumentException( "The receiver of the source call site is passed more than once.", nameof(request) );
                        }

                        if ( isReducedExtensionCall || !hasReceiverValue )
                        {
                            throw new ArgumentException(
                                $"The call site '{callSite}' has no receiver, or it calls an extension method in reduced form, whose receiver is its first argument and is passed with {nameof(RedirectedArgument)}.{nameof(RedirectedArgument.SourceArgument)}( 0 ).",
                                nameof(request) );
                        }

                        receiverUsed = true;

                        // The receiver is evaluated before the arguments.
                        items.Add( (-1, new CallSiteArgumentPlanItem( RedirectedArgumentKind.SourceReceiver, -1, null, name ), receivingParameter, null) );

                        break;

                    case RedirectedArgumentKind.SourceArgument:
                        if ( isReducedExtensionCall && argument.ParameterOrdinal == 0 )
                        {
                            if ( request.ReceiverMode != CallSiteReceiverMode.Drop || receiverUsed )
                            {
                                throw new ArgumentException( "The receiver of the source call site is passed more than once.", nameof(request) );
                            }

                            receiverUsed = true;
                            items.Add( (-1, new CallSiteArgumentPlanItem( RedirectedArgumentKind.SourceReceiver, -1, null, name ), receivingParameter, null) );

                            break;
                        }

                        if ( expandedParams != null && argument.ParameterOrdinal == expandedParams.Parameter!.Ordinal )
                        {
                            if ( argument.CastType != null )
                            {
                                throw new ArgumentException( $"The expanded params argument of the call site '{callSite}' cannot be cast.", nameof(request) );
                            }

                            if ( expandedElements.Any( e => !usedSourceArguments.Add( e ) ) )
                            {
                                throw new ArgumentException( $"The params argument of the call site '{callSite}' is passed more than once.", nameof(request) );
                            }

                            if ( receivingParameter.RefKind != Code.RefKind.None )
                            {
                                throw new ArgumentException(
                                    $"The params argument of the call site '{callSite}' cannot be passed to the parameter '{receivingParameter.Name}', which is passed by reference.",
                                    nameof(request) );
                            }

                            // An empty collection has no evaluation, so it is written after the other values of the call site.
                            var order = expandedElements.IsEmpty ? sourceArgumentSyntaxes.Count : expandedElements[0];

                            items.Add(
                                (order,
                                 CreatePackedArgument( name, expandedElements, expandedParams.Parameter.Type, callSite, semanticModel: operation.SemanticModel!, context ),
                                 receivingParameter, null) );

                            break;
                        }

                        if ( !sourceArgumentIndices.TryGetValue( argument.ParameterOrdinal, out var sourceIndex ) || sourceIndex < 0 )
                        {
                            throw new ArgumentException(
                                $"The parameter {argument.ParameterOrdinal} of the source method has no argument written at the call site '{callSite}'.",
                                nameof(request) );
                        }

                        if ( !usedSourceArguments.Add( sourceIndex ) )
                        {
                            throw new ArgumentException( $"The argument {sourceIndex} of the call site '{callSite}' is passed more than once.", nameof(request) );
                        }

                        var sourceArgument = sourceArgumentSyntaxes[sourceIndex];

                        if ( !IsCompatibleRefKind( sourceArgument, receivingParameter ) )
                        {
                            throw new ArgumentException(
                                $"The argument '{sourceArgument}' cannot be passed to the parameter '{receivingParameter.Name}', whose passing mode is different.",
                                nameof(request) );
                        }

                        TypeSyntax? castType = null;

                        if ( argument.CastType != null )
                        {
                            if ( !sourceArgument.RefKindKeyword.IsKind( SyntaxKind.None ) || receivingParameter.RefKind != Code.RefKind.None )
                            {
                                throw new ArgumentException(
                                    $"The argument '{sourceArgument}' cannot be cast, because it is not passed by value to the parameter '{receivingParameter.Name}'.",
                                    nameof(request) );
                            }

                            castType = context.SyntaxGenerator.TypeSyntax( argument.CastType ).WithSimplifierAnnotationIfNecessary( context );
                        }

                        items.Add(
                            (sourceIndex, new CallSiteArgumentPlanItem( RedirectedArgumentKind.SourceArgument, sourceIndex, null, name ) { CastType = castType },
                             receivingParameter, argument.CastType) );

                        break;

                    default:
                        // Expressions are evaluated after all the values of the source call site.
                        items.Add( (int.MaxValue, new CallSiteArgumentPlanItem( RedirectedArgumentKind.Value, -1, argument.Expression, name ), receivingParameter, null) );

                        break;
                }
            }

            if ( request.ReceiverMode == CallSiteReceiverMode.Drop && hasReceiverValue && !receiverUsed )
            {
                throw new ArgumentException( $"The receiver of '{callSite}' is not passed.", nameof(request) );
            }

            // The order of the list is the order of evaluation of the source call site. The linker writes named arguments, so the order does not need
            // to match the order of the parameters.
            var orderedItems = items.OrderBy( x => x.Order ).ToImmutableArray();
            var precedingDiscards = new List<int>?[orderedItems.Length];
            var followingDiscards = new List<int>?[orderedItems.Length];
            var semanticModel = operation.SemanticModel!;

            // A value of the source call site that is not passed is still evaluated, in its source order, when it can have a side effect. It is evaluated
            // and discarded before the next argument of the new call, or after the previous one when the next one cannot hold it.
            for ( var i = 0; i < sourceArgumentSyntaxes.Count; i++ )
            {
                if ( usedSourceArguments.Contains( i ) )
                {
                    continue;
                }

                var argument = sourceArgumentSyntaxes[i];

                if ( !argument.RefKindKeyword.IsKind( SyntaxKind.None ) )
                {
                    throw new ArgumentException( $"The argument '{argument}' of the call site '{callSite}' is passed by reference, so it must be passed.", nameof(request) );
                }

                var value = GetArgumentValue( semanticModel, argument );

                if ( IsWithoutSideEffect( value ) )
                {
                    continue;
                }

                if ( !CanBeDiscarded( semanticModel, argument, value, out var reason ) )
                {
                    throw new ArgumentException(
                        $"The argument '{argument}' of the call site '{callSite}' is not passed, and it can have a side effect, but it cannot be evaluated into a discard: {reason}",
                        nameof(request) );
                }

                var nextIndex = 0;

                while ( nextIndex < orderedItems.Length && orderedItems[nextIndex].Order < i )
                {
                    nextIndex++;
                }

                var previousIndex = nextIndex - 1;

                if ( nextIndex < orderedItems.Length && CanHoldPrecedingDiscard( orderedItems[nextIndex], sourceArgumentSyntaxes ) )
                {
                    (precedingDiscards[nextIndex] ??= new List<int>()).Add( i );
                }
                else if ( previousIndex >= 0 && CanHoldFollowingDiscard( semanticModel, orderedItems[previousIndex], sourceArgumentSyntaxes, argument ) )
                {
                    (followingDiscards[previousIndex] ??= new List<int>()).Add( i );
                }
                else
                {
                    throw new ArgumentException(
                        $"The argument '{argument}' of the call site '{callSite}' is not passed, and it can have a side effect, but no argument of the new call can evaluate it in its source order. An adjacent argument must be passed by value.",
                        nameof(request) );
                }
            }

            if ( (precedingDiscards.Any( d => d != null ) || followingDiscards.Any( d => d != null ))
                 && ((CSharpParseOptions) callSite.SyntaxTree.Options).LanguageVersion < LanguageVersion.CSharp9 )
            {
                throw new ArgumentException(
                    $"An argument of the call site '{callSite}' that is not passed can have a side effect, and the switch expression that evaluates it requires C# 9 or later.",
                    nameof(request) );
            }

            var valueVariableName = followingDiscards.Any( d => d != null ) ? GetValueVariableName( semanticModel, callSite ) : null;

            return orderedItems.Select(
                    ( x, index ) => x.Item with
                    {
                        PrecedingDiscards = precedingDiscards[index]?.ToImmutableArray() ?? default,
                        FollowingDiscards = followingDiscards[index]?.ToImmutableArray() ?? default,
                        ValueVariableName = followingDiscards[index] != null ? valueVariableName : null
                    } )
                .ToImmutableArray();
        }

        /// <summary>
        /// Creates the argument that packs the elements of an expanded <c>params</c> argument into one collection, which is written as a named argument.
        /// </summary>
        /// <remarks>
        /// From C# 12, the collection is a collection expression, which creates the collection that the compiler creates for the expanded argument,
        /// also for a <c>params</c> collection of C# 13. Before C# 12, only <c>params</c> arrays exist, and the collection is an array creation. An
        /// empty array is written <c>Array.Empty&lt;T&gt;()</c> when the method exists, as the compiler does.
        /// </remarks>
        private static CallSiteArgumentPlanItem CreatePackedArgument(
            string name,
            ImmutableArray<int> elements,
            ITypeSymbol parameterType,
            InvocationExpressionSyntax callSite,
            SemanticModel semanticModel,
            SyntaxGenerationContext context )
        {
            var item = new CallSiteArgumentPlanItem( RedirectedArgumentKind.SourceArgument, -1, null, name ) { PackedElements = elements };

            if ( ((CSharpParseOptions) callSite.SyntaxTree.Options).LanguageVersion >= LanguageVersion.CSharp12 )
            {
                return item with { PackedEmptyValue = CollectionExpression() };
            }

            // A params parameter whose type is not an array is a params collection, which exists only from C# 13.
            Invariant.Assert( parameterType.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array );

            var elementType = context.SyntaxGenerator.TypeSyntax( ((IArrayTypeSymbol) parameterType).ElementType ).WithSimplifierAnnotationIfNecessary( context );

            var hasArrayEmpty = semanticModel.Compilation.GetSpecialType( Microsoft.CodeAnalysis.SpecialType.System_Array )
                .GetMembers( "Empty" )
                .Any( m => m.Kind == SymbolKind.Method && m.IsStatic && m.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public );

            ExpressionSyntax emptyValue = hasArrayEmpty
                ? InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        context.SyntaxGenerator.TypeSyntax( semanticModel.Compilation.GetSpecialType( Microsoft.CodeAnalysis.SpecialType.System_Array ) ),
                        GenericName( Identifier( "Empty" ), TypeArgumentList( SingletonSeparatedList( elementType ) ) ) ) )
                : ArrayCreationExpression(
                    SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.NewKeyword ),
                    ArrayType(
                        elementType,
                        SingletonList( ArrayRankSpecifier( SingletonSeparatedList<ExpressionSyntax>( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( 0 ) ) ) ) ) ),
                    null );

            return item with { PackedArrayElementType = elementType, PackedEmptyValue = emptyValue };
        }

        /// <summary>
        /// Determines whether a source argument that can have a side effect can be evaluated alone, as the governing expression of a switch expression,
        /// with the same effects as at the source call site.
        /// </summary>
        private static bool CanBeDiscarded( SemanticModel semanticModel, ArgumentSyntax argument, IOperation? value, out string reason )
        {
            var expression = argument.Expression;

            // The operation tree has no node for parentheses and for the suppression of a nullable warning, so the syntax of the value can be the
            // expression of the argument or an expression inside these nodes.
            var expressionForms = new List<SyntaxNode> { expression };

            while ( expression.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.SuppressNullableWarningExpression )
            {
                expression = expression.Kind() == SyntaxKind.ParenthesizedExpression
                    ? ((ParenthesizedExpressionSyntax) expression).Expression
                    : ((PostfixUnaryExpressionSyntax) expression).Operand;

                expressionForms.Add( expression );
            }

            // The implicit conversion to the type of the parameter is not evaluated when the value is discarded. A user-defined conversion runs user code
            // at the source call site, so it cannot be omitted.
            while ( value is IConversionOperation { IsImplicit: true } conversion )
            {
                if ( conversion.OperatorMethod != null )
                {
                    reason = "its conversion to the type of the parameter calls a user-defined operator.";

                    return false;
                }

                value = conversion.Operand;
            }

            if ( value == null || !expressionForms.Contains( value.Syntax ) || value is IInterpolatedStringHandlerCreationOperation
                 || expression.Kind() is SyntaxKind.ImplicitObjectCreationExpression or SyntaxKind.CollectionExpression )
            {
                reason = "its value depends on the type of the parameter.";

                return false;
            }

            var type = semanticModel.GetTypeInfo( expression ).Type;

            if ( type == null || type.TypeKind is Microsoft.CodeAnalysis.TypeKind.Pointer or Microsoft.CodeAnalysis.TypeKind.FunctionPointer || type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Void )
            {
                reason = "it has no type, or it has a pointer type, which a switch expression cannot take.";

                return false;
            }

            reason = "";

            return true;
        }

        /// <summary>
        /// Determines whether an argument of the new call can be written <c>D switch { _ =&gt; value }</c>.
        /// </summary>
        private static bool CanHoldPrecedingDiscard(
            (int Order, CallSiteArgumentPlanItem Item, IParameter Parameter, IType? CastType) item,
            SeparatedSyntaxList<ArgumentSyntax> sourceArguments )
            => item.Parameter.RefKind == Code.RefKind.None
               && item.Item.Kind switch
               {
                   RedirectedArgumentKind.SourceArgument when item.Item.IsPacked => true,
                   RedirectedArgumentKind.SourceArgument => sourceArguments[item.Item.SourceArgumentIndex].RefKindKeyword.IsKind( SyntaxKind.None ),
                   RedirectedArgumentKind.Value => true,
                   _ => false
               };

        /// <summary>
        /// Determines whether an argument of the new call can be written <c>value switch { var t =&gt; D switch { _ =&gt; t } }</c>.
        /// </summary>
        /// <remarks>
        /// The pattern variable has the natural type of the value, so the natural type must convert implicitly to the type of the parameter. This
        /// excludes a value whose conversion depends on the expression, for instance a constant that a constant conversion narrows. The discarded
        /// argument becomes part of an arm, so it must not declare a variable, whose scope would end with the arm.
        /// </remarks>
        private static bool CanHoldFollowingDiscard(
            SemanticModel semanticModel,
            (int Order, CallSiteArgumentPlanItem Item, IParameter Parameter, IType? CastType) item,
            SeparatedSyntaxList<ArgumentSyntax> sourceArguments,
            ArgumentSyntax discardedArgument )
        {
            if ( item.Parameter.RefKind != Code.RefKind.None || item.Item.Kind != RedirectedArgumentKind.SourceArgument || item.Item.IsPacked )
            {
                return false;
            }

            var sourceArgument = sourceArguments[item.Item.SourceArgumentIndex];

            // The pattern variable of the holder takes the natural type of the value, and a target-typed value has none.
            if ( sourceArgument.Expression.Kind() is SyntaxKind.ImplicitObjectCreationExpression or SyntaxKind.CollectionExpression or SyntaxKind.DefaultLiteralExpression
                 || semanticModel.GetOperation( sourceArgument.Expression ) is IInterpolatedStringHandlerCreationOperation )
            {
                return false;
            }

            if ( !sourceArgument.RefKindKeyword.IsKind( SyntaxKind.None )
                 || discardedArgument.Expression.DescendantNodesAndSelf().Any( n => n.Kind() is SyntaxKind.DeclarationExpression or SyntaxKind.SingleVariableDesignation ) )
            {
                return false;
            }

            // The natural type of a cast argument is the type of the cast.
            var naturalType = item.CastType != null ? item.CastType.GetSymbol() : semanticModel.GetTypeInfo( sourceArgument.Expression ).Type;
            var parameterType = item.Parameter.Type.GetSymbol();

            return naturalType != null && parameterType != null && naturalType.TypeKind is not (Microsoft.CodeAnalysis.TypeKind.Pointer or Microsoft.CodeAnalysis.TypeKind.FunctionPointer)
                   && semanticModel.Compilation.ClassifyConversion( naturalType, parameterType ).IsImplicit;
        }

        /// <summary>
        /// Returns the name of the pattern variable that holds the value of an argument while the discards that follow it are evaluated. The name is
        /// not the name of a symbol in scope.
        /// </summary>
        /// <remarks>
        /// The name contains the number of invocations that contain the call site. A call site nested in an argument of another one therefore gets
        /// another name, so the scope of its pattern variable, which is an arm of the switch expression of the outer call site, does not declare the
        /// name twice. Call sites with the same depth are in separate arms, which are separate scopes.
        /// </remarks>
        private static string GetValueVariableName( SemanticModel semanticModel, InvocationExpressionSyntax callSite )
        {
            var depth = callSite.Ancestors().Count( n => n.IsKind( SyntaxKind.InvocationExpression ) );
            var baseName = $"__value{depth}";

            for ( var i = 0;; i++ )
            {
                var name = i == 0 ? baseName : $"{baseName}_{i}";

                if ( semanticModel.LookupSymbols( callSite.SpanStart, name: name ).IsEmpty
                     && !callSite.DescendantTokens().Any( t => t.IsKind( SyntaxKind.IdentifierToken ) && t.ValueText == name ) )
                {
                    return name;
                }
            }
        }

        private static bool IsCompatibleRefKind( ArgumentSyntax argument, IParameter parameter )
            => (argument.RefKindKeyword.Kind(), parameter.RefKind) switch
            {
                (SyntaxKind.None, Code.RefKind.None or Code.RefKind.In) => true,
                (SyntaxKind.InKeyword, Code.RefKind.In or Code.RefKind.RefReadOnly) => true,
                (SyntaxKind.RefKeyword, Code.RefKind.Ref or Code.RefKind.RefReadOnly) => true,
                (SyntaxKind.OutKeyword, Code.RefKind.Out) => true,
                _ => false
            };

        /// <summary>
        /// Returns the operation of the value that a source argument passes, including the implicit conversion to the type of the parameter.
        /// </summary>
        private static IOperation? GetArgumentValue( SemanticModel semanticModel, ArgumentSyntax argument )
        {
            if ( semanticModel.GetOperation( argument ) is IArgumentOperation argumentOperation )
            {
                return argumentOperation.Value;
            }

            // An element of an expanded params argument has no argument operation of its own, and neither has an argument whose expression is in
            // parentheses or suppresses a nullable warning, because the operation tree has no node for these expressions. The value is the operation
            // of the inner expression, wrapped in the implicit conversions to the type of the parameter or of the elements.
            var expression = argument.Expression;

            while ( expression.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.SuppressNullableWarningExpression )
            {
                expression = expression.Kind() == SyntaxKind.ParenthesizedExpression
                    ? ((ParenthesizedExpressionSyntax) expression).Expression
                    : ((PostfixUnaryExpressionSyntax) expression).Operand;
            }

            var operation = semanticModel.GetOperation( expression );

            while ( operation?.Parent is IConversionOperation conversion && (conversion.Syntax == expression || conversion.Syntax == argument.Expression) )
            {
                operation = conversion;
            }

            return operation;
        }

        /// <summary>
        /// Determines whether the evaluation of a value can be omitted without changing the behavior of the program.
        /// </summary>
        /// <remarks>
        /// The method accepts constants, <c>default</c>, <c>typeof</c>, <c>this</c>, the values of locals and parameters, the fields of <c>this</c>,
        /// anonymous functions, and the delegates of static methods or of methods of such a value, through conversions that are not user-defined. It
        /// refuses static fields, because reading a static field can run the static constructor of its type. It refuses any other form, for instance a
        /// property, which runs a getter, and a field of another object, which can throw a <see cref="NullReferenceException"/>.
        /// </remarks>
        private static bool IsWithoutSideEffect( IOperation? operation )
            => operation switch
            {
                { ConstantValue.HasValue: true } => true,
                IDefaultValueOperation or ITypeOfOperation or IInstanceReferenceOperation or ILocalReferenceOperation or IParameterReferenceOperation => true,
                IFieldReferenceOperation { Field.IsStatic: false, Instance: IInstanceReferenceOperation } => true,
                IAnonymousFunctionOperation => true,
                IMethodReferenceOperation methodReference => methodReference.Instance == null || IsWithoutSideEffect( methodReference.Instance ),
                IDelegateCreationOperation delegateCreation => IsWithoutSideEffect( delegateCreation.Target ),
                IConversionOperation { IsImplicit: true, OperatorMethod: null } conversion
                    => IsWithoutSideEffect( conversion.Operand ),
                _ => false
            };
    }
}
