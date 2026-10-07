// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Extensibility.CallSites
{
    public sealed partial class ExtensionTransformationFactory
    {
        /// <summary>
        /// Creates the plan of the argument list of a redirected invocation from the arguments of the request, and validates it against the parameters
        /// of the target method and the arguments of the call site.
        /// </summary>
        /// <param name="description">The description of the redirection, which is stored in the variable renames.</param>
        /// <param name="variableRenames">The collection to which the method adds the variables that must be renamed, because they hide a parameter
        /// that the request passes as a value. It is created when the first variable is added, because a rename is rare.</param>
        private static ImmutableArray<CallSiteArgumentPlanItem> CreateArgumentPlan(
            InvocationRedirectionRequest request,
            IInvocationOperation operation,
            bool isReducedExtensionCall,
            bool hasReceiverValue,
            CompilationModel compilation,
            SyntaxGenerationContext context,
            string description,
            ref List<CallSiteVariableRename>? variableRenames )
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

            // The linker writes each argument with its name, so the parameter of this name receives it. The receiving parameters are computed first,
            // because the validation of an argument passed more than once considers all the parameters that receive it.
            var argumentNames = new string[request.Arguments.Length];
            var receivingParameters = new IParameter[request.Arguments.Length];

            for ( var i = 0; i < request.Arguments.Length; i++ )
            {
                argumentNames[i] = request.Arguments[i].Name ?? targetParameters[i + parameterOffset].Name;
                receivingParameters[i] = targetParameters.FirstOrDefault( p => p.Name == argumentNames[i] ) ?? targetParameters[i + parameterOffset];
            }

            // The context in which expressions of the code model are generated. It depends only on the call site, so it is created once, when the first
            // such expression is generated.
            SyntaxSerializationContext? serializationContext = null;

            for ( var i = 0; i < request.Arguments.Length; i++ )
            {
                var argument = request.Arguments[i];
                var name = argumentNames[i];
                var receivingParameter = receivingParameters[i];

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

                        var sourceArgument = sourceArgumentSyntaxes[sourceIndex];

                        // An argument can be passed more than once only when it is passed by value and each occurrence gives the same value, because
                        // each occurrence is evaluated and converted.
                        if ( !usedSourceArguments.Add( sourceIndex )
                             && (!sourceArgument.RefKindKeyword.IsKind( SyntaxKind.None )
                                 || !CanBeDuplicated(
                                     operation.SemanticModel!,
                                     sourceArgument,
                                     receivingParameters.Where(
                                         ( _, j ) => request.Arguments[j].Kind == RedirectedArgumentKind.SourceArgument
                                                     && request.Arguments[j].ParameterOrdinal == argument.ParameterOrdinal ) )) )
                        {
                            throw new ArgumentException(
                                $"The argument '{sourceArgument}' of the call site '{callSite}' is passed more than once, which is accepted only for an argument passed by value whose evaluation has no side effect and gives the same value at each occurrence.",
                                nameof(request) );
                        }

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
                        // Expressions are evaluated after all the values of the source call site. An expression of the code model is generated in the
                        // context of the call site.
                        if ( argument.Expression == null )
                        {
                            serializationContext ??= new SyntaxSerializationContext(
                                compilation,
                                context,
                                null,
                                GetEnclosingDeclaration( operation.SemanticModel!, callSite, compilation ) );
                        }

                        var valueSyntax = argument.Expression ?? GetValueExpressionSyntax( argument.ValueExpression!, name, serializationContext! );

                        // A parameter of the calling member is written as its name, which a variable of a lambda or local function can hide.
                        if ( argument.ValueExpression is IParameter parameter && valueSyntax.IsKind( SyntaxKind.IdentifierName ) )
                        {
                            valueSyntax = GetParameterValueSyntax( parameter, (IdentifierNameSyntax) valueSyntax, operation.SemanticModel!, callSite, context, description, ref variableRenames );
                        }

                        items.Add( (int.MaxValue, new CallSiteArgumentPlanItem( RedirectedArgumentKind.Value, -1, valueSyntax, name ), receivingParameter, null) );

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
            var dropTypes = new Dictionary<int, (ITypeSymbol Type, bool IsNatural)>();
            var semanticModel = operation.SemanticModel!;

            // A value of the source call site that is not passed is still evaluated, in its source order, when it can have a side effect. It is passed
            // to CallSiteHelper.DropBefore together with the next argument of the new call, or to CallSiteHelper.DropAfter together with the previous
            // one when the next one cannot hold it.
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

                if ( !CanBeDropped( semanticModel, argument, value, out var dropType, out var isNaturalType, out var reason ) )
                {
                    throw new ArgumentException(
                        $"The argument '{argument}' of the call site '{callSite}' is not passed, and it can have a side effect, but it cannot be evaluated separately: {reason}",
                        nameof(request) );
                }

                dropTypes[i] = (dropType, isNaturalType);

                var nextIndex = 0;

                while ( nextIndex < orderedItems.Length && orderedItems[nextIndex].Order < i )
                {
                    nextIndex++;
                }

                var previousIndex = nextIndex - 1;

                if ( nextIndex < orderedItems.Length && CanHoldDroppedValue( semanticModel, orderedItems[nextIndex], sourceArgumentSyntaxes ) )
                {
                    (precedingDiscards[nextIndex] ??= new List<int>()).Add( i );
                }
                else if ( previousIndex >= 0 && CanHoldDroppedValue( semanticModel, orderedItems[previousIndex], sourceArgumentSyntaxes ) )
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

            if ( dropTypes.Count == 0 )
            {
                return orderedItems.SelectAsImmutableArray( x => x.Item );
            }

            var dropHelperType = context.SyntaxGenerator.TypeSyntax( context.ReflectionMapper.GetTypeSymbol( typeof(CallSiteHelper) ) )
                .WithSimplifierAnnotationIfNecessary( context );

            var targetMethodSymbol = request.Target.Method.GetSymbol();

            return orderedItems.Select(
                    ( x, index ) =>
                    {
                        var preceding = precedingDiscards[index]?.ToImmutableArray() ?? default;
                        var following = followingDiscards[index]?.ToImmutableArray() ?? default;

                        if ( preceding.IsDefault && following.IsDefault )
                        {
                            return x.Item;
                        }

                        var item = x.Item with { PrecedingDiscards = preceding, FollowingDiscards = following, DropHelperType = dropHelperType };

                        // The type arguments of the helper are inferred, unless the kept value has no natural type, or a type that does not convert
                        // implicitly to the type of the parameter, or a dropped value has no natural type. Then both type arguments are written, so that
                        // each value is converted to the same type as in the original call.
                        var dropped = (preceding.IsDefault ? Enumerable.Empty<int>() : preceding).Concat( following.IsDefault ? Enumerable.Empty<int>() : following );

                        if ( !NeedsExplicitKeepType( semanticModel, x, sourceArgumentSyntaxes ) && dropped.All( i => dropTypes[i].IsNatural ) )
                        {
                            return item;
                        }

                        if ( x.Parameter.Type.GetSymbol() is not { } parameterType
                             || !CanBeWrittenAsTypeArgument( parameterType, targetMethodSymbol ) )
                        {
                            throw new ArgumentException(
                                $"An argument of the call site '{callSite}' that is not passed can have a side effect, and the type of the parameter '{x.Parameter.Name}' of the target cannot be written as a type argument of the call that evaluates it.",
                                nameof(request) );
                        }

                        foreach ( var i in dropped )
                        {
                            if ( !CanBeWrittenAsTypeArgument( dropTypes[i].Type, targetMethodSymbol ) )
                            {
                                throw new ArgumentException(
                                    $"The argument '{sourceArgumentSyntaxes[i]}' of the call site '{callSite}' is not passed, and it can have a side effect, but its type '{dropTypes[i].Type.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' cannot be written as a type argument of the call that evaluates it.",
                                    nameof(request) );
                            }
                        }

                        TypeSyntax? CreateDropType( ImmutableArray<int> indices )
                            => indices.IsDefault
                                ? null
                                : indices.Length == 1
                                    ? context.SyntaxGenerator.TypeSyntax( dropTypes[indices[0]].Type ).WithSimplifierAnnotationIfNecessary( context )
                                    : TupleType(
                                        SeparatedList(
                                            indices.Select(
                                                i => TupleElement(
                                                    context.SyntaxGenerator.TypeSyntax( dropTypes[i].Type ).WithSimplifierAnnotationIfNecessary( context ) ) ) ) );

                        return item with
                        {
                            KeepTypeArgument = context.SyntaxGenerator.TypeSyntax( parameterType ).WithSimplifierAnnotationIfNecessary( context ),
                            PrecedingDropTypeArgument = CreateDropType( preceding ),
                            FollowingDropTypeArgument = CreateDropType( following )
                        };
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
        /// Determines whether a source argument that can have a side effect can be passed alone to <c>CallSiteHelper</c>, with the same effects as at
        /// the source call site, and returns the type with which it is passed.
        /// </summary>
        /// <param name="semanticModel">The semantic model of the call site.</param>
        /// <param name="argument">The source argument.</param>
        /// <param name="value">The operation of the value of the argument, including its conversion to the type of the parameter.</param>
        /// <param name="type">The natural type of the value, or the type of the parameter when the value has no natural type.</param>
        /// <param name="isNaturalType">Indicates whether <paramref name="type"/> is the natural type of the value.</param>
        /// <param name="reason">The reason of a refusal.</param>
        private static bool CanBeDropped(
            SemanticModel semanticModel,
            ArgumentSyntax argument,
            IOperation? value,
            out ITypeSymbol type,
            out bool isNaturalType,
            out string reason )
        {
            type = null!;
            isNaturalType = false;

            // The implicit conversion to the type of the parameter is not evaluated when the value is dropped with its natural type. A user-defined
            // conversion runs user code at the source call site, so it cannot be omitted.
            while ( value is IConversionOperation { IsImplicit: true } conversion )
            {
                if ( conversion.OperatorMethod != null )
                {
                    reason = "its conversion to the type of the parameter calls a user-defined operator.";

                    return false;
                }

                value = conversion.Operand;
            }

            if ( value is IInterpolatedStringHandlerCreationOperation )
            {
                reason = "it creates an interpolated string handler, whose construction depends on the other arguments of the call.";

                return false;
            }

            var naturalType = GetNaturalType( semanticModel, argument.Expression );
            var actualType = naturalType ?? semanticModel.GetTypeInfo( argument.Expression ).ConvertedType;

            if ( actualType == null || actualType.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Void )
            {
                reason = "it has no type.";

                return false;
            }

            if ( actualType.IsRefLikeType || actualType.TypeKind is Microsoft.CodeAnalysis.TypeKind.Pointer or Microsoft.CodeAnalysis.TypeKind.FunctionPointer )
            {
                reason = $"its type '{actualType.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' is a ref struct or a pointer type, which cannot be a type argument.";

                return false;
            }

            type = actualType;
            isNaturalType = naturalType != null;
            reason = "";

            return true;
        }

        /// <summary>
        /// Determines whether an argument of the new call can be passed to <c>CallSiteHelper</c> together with dropped values.
        /// </summary>
        /// <remarks>
        /// The argument must be passed by value. An interpolated string passed to an interpolated string handler cannot be passed, because the handler
        /// would be created for the parameter of the helper, which is not the parameter of the target. A ref struct or a pointer cannot be a type
        /// argument.
        /// </remarks>
        private static bool CanHoldDroppedValue(
            SemanticModel semanticModel,
            (int Order, CallSiteArgumentPlanItem Item, IParameter Parameter, IType? CastType) item,
            SeparatedSyntaxList<ArgumentSyntax> sourceArguments )
        {
            if ( item.Parameter.RefKind != Code.RefKind.None || (item.Parameter.Type.GetSymbol() is { } parameterType && !IsTypeArgumentKind( parameterType )) )
            {
                return false;
            }

            if ( item.Item.Kind != RedirectedArgumentKind.SourceArgument || item.Item.IsPacked )
            {
                return item.Item.Kind is RedirectedArgumentKind.SourceArgument or RedirectedArgumentKind.Value;
            }

            var sourceArgument = sourceArguments[item.Item.SourceArgumentIndex];

            if ( !sourceArgument.RefKindKeyword.IsKind( SyntaxKind.None )
                 || semanticModel.GetOperation( sourceArgument.Expression ) is IInterpolatedStringHandlerCreationOperation )
            {
                return false;
            }

            var naturalType = item.CastType != null ? item.CastType.GetSymbol() : GetNaturalType( semanticModel, sourceArgument.Expression );

            return naturalType == null || IsTypeArgumentKind( naturalType );
        }

        /// <summary>
        /// Determines whether the type arguments of <c>CallSiteHelper</c> must be written for an argument of the new call, because the kept value has
        /// no natural type, or because its natural type does not convert implicitly to the type of the parameter, for instance a constant that a
        /// constant conversion narrows.
        /// </summary>
        private static bool NeedsExplicitKeepType(
            SemanticModel semanticModel,
            (int Order, CallSiteArgumentPlanItem Item, IParameter Parameter, IType? CastType) item,
            SeparatedSyntaxList<ArgumentSyntax> sourceArguments )
        {
            if ( item.Item.Kind != RedirectedArgumentKind.SourceArgument || item.Item.IsPacked )
            {
                // The type of an expression of the request or of a packed collection is not known before the rewrite.
                return true;
            }

            var naturalType = item.CastType != null
                ? item.CastType.GetSymbol()
                : GetNaturalType( semanticModel, sourceArguments[item.Item.SourceArgumentIndex].Expression );

            var parameterType = item.Parameter.Type.GetSymbol();

            return naturalType == null || parameterType == null || !semanticModel.Compilation.ClassifyConversion( naturalType, parameterType ).IsImplicit;
        }

        /// <summary>
        /// Generates the syntax of an expression of the code model that a request passes with <see cref="RedirectedArgument.Value(IExpression)"/>,
        /// in the context of the call site.
        /// </summary>
        /// <exception cref="ArgumentException">The expression cannot be emitted, for instance because it is an inspection-only source
        /// expression.</exception>
        /// <remarks>
        /// The expression is generated in the context of the declaration that contains the call site, so that an instance member of the calling type
        /// is written with <c>this</c>.
        /// </remarks>
        private static ExpressionSyntax GetValueExpressionSyntax( IExpression expression, string name, SyntaxSerializationContext serializationContext )
        {
            try
            {
                return expression.ToTypedExpressionSyntax( serializationContext ).Syntax;
            }
            catch ( Exception e ) when ( e is not OperationCanceledException )
            {
                throw new ArgumentException( $"The value of the argument '{name}' cannot be emitted at the call site: {e.Message}", "request", e );
            }
        }

        /// <summary>
        /// Returns the declaration of the code model that contains a call site: the member, or the field, property or event of an initializer. A
        /// lambda or a local function is attributed to the member that contains it.
        /// </summary>
        private static IDeclaration? GetEnclosingDeclaration( SemanticModel semanticModel, SyntaxNode callSite, CompilationModel compilation )
        {
            var symbol = semanticModel.GetEnclosingSymbol( callSite.SpanStart );

            while ( symbol is { Kind: SymbolKind.Method } && ((IMethodSymbol) symbol).MethodKind is Microsoft.CodeAnalysis.MethodKind.AnonymousFunction or Microsoft.CodeAnalysis.MethodKind.LocalFunction )
            {
                symbol = symbol.ContainingSymbol;
            }

            return symbol != null && compilation.Factory.TryGetDeclaration( symbol, out var declaration ) ? declaration : null;
        }

        /// <summary>
        /// Determines whether an argument of the call site can be passed to several parameters, which evaluate and convert it once each.
        /// </summary>
        /// <remarks>
        /// The argument must have no side effect, and the evaluations must give the same value. An anonymous function or a method group creates a new
        /// delegate or a new expression tree at each evaluation, also under an implicit conversion, and a value that is not of a reference type is
        /// boxed at each conversion to a reference type. These arguments are refused, because the parameters would receive different objects. A type
        /// parameter without the <c>class</c> constraint is not of a reference type, because it can be a value type.
        /// </remarks>
        /// <param name="semanticModel">The semantic model of the call site.</param>
        /// <param name="argument">The argument of the call site.</param>
        /// <param name="receivingParameters">All the parameters of the target that receive the argument.</param>
        private static bool CanBeDuplicated( SemanticModel semanticModel, ArgumentSyntax argument, IEnumerable<IParameter> receivingParameters )
        {
            var value = GetArgumentValue( semanticModel, argument );
            var convertedValue = value;

            while ( convertedValue is IConversionOperation { IsImplicit: true } conversion )
            {
                convertedValue = conversion.Operand;
            }

            if ( !IsWithoutSideEffect( value ) || convertedValue is IAnonymousFunctionOperation or IMethodReferenceOperation or IDelegateCreationOperation )
            {
                return false;
            }

            var type = semanticModel.GetTypeInfo( argument.Expression ).Type;

            return type is null or { IsReferenceType: true } || receivingParameters.All( p => p.Type.GetSymbol() is not { IsReferenceType: true } );
        }

        /// <summary>
        /// Returns the natural type of an expression, or <c>null</c> when the expression has none and takes its type from its target.
        /// </summary>
        /// <remarks>
        /// The semantic model gives the type of the target as the type of a target-typed object creation (<c>new()</c>) and of the <c>default</c>
        /// literal, so these forms and collection expressions are recognized by their syntax. The other expressions without natural type, for
        /// instance <c>null</c>, a lambda or a target-typed conditional expression, have no type in the semantic model.
        /// </remarks>
        private static ITypeSymbol? GetNaturalType( SemanticModel semanticModel, ExpressionSyntax expression )
        {
            var unwrapped = expression;

            while ( unwrapped.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.SuppressNullableWarningExpression )
            {
                unwrapped = unwrapped.Kind() == SyntaxKind.ParenthesizedExpression
                    ? ((ParenthesizedExpressionSyntax) unwrapped).Expression
                    : ((PostfixUnaryExpressionSyntax) unwrapped).Operand;
            }

            if ( unwrapped.Kind() is SyntaxKind.ImplicitObjectCreationExpression or SyntaxKind.DefaultLiteralExpression or SyntaxKind.CollectionExpression )
            {
                return null;
            }

            return semanticModel.GetTypeInfo( expression ).Type;
        }

        /// <summary>
        /// Determines whether a type can be a type argument of a method: it is not a ref struct, a pointer type or a function pointer type.
        /// </summary>
        private static bool IsTypeArgumentKind( ITypeSymbol type )
            => !type.IsRefLikeType && type.TypeKind is not (Microsoft.CodeAnalysis.TypeKind.Pointer or Microsoft.CodeAnalysis.TypeKind.FunctionPointer);

        /// <summary>
        /// Determines whether a type can be written as a type argument at the call site: it can be a type argument, it can be named, and it does not
        /// contain a type parameter of the target method, which is not in scope at the call site.
        /// </summary>
        private static bool CanBeWrittenAsTypeArgument( ITypeSymbol type, ISymbol? targetMethod )
            => IsTypeArgumentKind( type ) && CanBeNamed( type ) && !ContainsTypeParameterOf( type, targetMethod );

        /// <summary>
        /// Determines whether a type contains a type parameter of a given method.
        /// </summary>
        private static bool ContainsTypeParameterOf( ITypeSymbol type, ISymbol? method )
            => method != null
               && type.Kind switch
               {
                   SymbolKind.TypeParameter => SymbolEqualityComparer.Default.Equals(
                       ((ITypeParameterSymbol) type).DeclaringMethod?.OriginalDefinition,
                       method.OriginalDefinition ),
                   SymbolKind.ArrayType => ContainsTypeParameterOf( ((IArrayTypeSymbol) type).ElementType, method ),
                   SymbolKind.NamedType => ((INamedTypeSymbol) type).TypeArguments.Any( t => ContainsTypeParameterOf( t, method ) ),
                   _ => false
               };

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
