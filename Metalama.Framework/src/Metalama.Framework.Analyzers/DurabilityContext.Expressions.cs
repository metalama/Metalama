// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// Evaluates the durability of an <i>expression</i>, which is what the rules on assignments and arguments need.
    /// </summary>
    /// <remarks>
    /// The declared type of a member or a parameter is not always the best available evidence. An expression carries
    /// more: a lambda shows what it captures, an object creation shows what was constructed, and a read of a member
    /// that is itself under the contract carries the obligation that was already discharged where the value entered.
    /// </remarks>
    internal sealed partial class DurabilityContext
    {
        /// <summary>
        /// The result of evaluating an expression, which may name a captured variable in addition to a verdict.
        /// </summary>
        public readonly struct ExpressionVerdict
        {
            public DurabilityVerdict DurabilityVerdict { get; }

            /// <summary>
            /// Gets the variable captured by a lambda that makes the expression not durable, or <c>null</c> when the
            /// verdict does not come from a closure.
            /// </summary>
            public ISymbol? CapturedSymbol { get; }

            /// <summary>
            /// Gets the location at which to report, which for a closure is the use of the captured variable rather
            /// than the lambda, so that the squiggle lands on the thing to remove.
            /// </summary>
            public Location? Location { get; }

            /// <summary>
            /// Gets the expression whose type makes the verdict fail, or <c>null</c> when the verdict does not come from
            /// the type of one expression.
            /// </summary>
            /// <remarks>
            /// For <c>new { T = x }</c>, it is <c>x</c>. The rule that reports the verdict uses it to suggest a method that
            /// converts the value to a durable one.
            /// </remarks>
            public IOperation? Operand { get; }

            public bool IsDurable => this.DurabilityVerdict.IsDurable;

            public ExpressionVerdict(
                DurabilityVerdict verdict,
                ISymbol? capturedSymbol = null,
                Location? location = null,
                IOperation? operand = null )
            {
                this.DurabilityVerdict = verdict;
                this.CapturedSymbol = capturedSymbol;
                this.Location = location;
                this.Operand = operand;
            }

            public static readonly ExpressionVerdict Durable = new( DurabilityVerdict.Durable );
        }

        /// <summary>
        /// Evaluates an expression that is about to be stored in, or passed to, something under the contract.
        /// </summary>
        public ExpressionVerdict GetExpressionVerdict( IOperation? value )
        {
            if ( value == null )
            {
                return ExpressionVerdict.Durable;
            }

            value = Unwrap( value );

            switch ( value )
            {
                // A null literal and a default expression reach nothing.
                case ILiteralOperation { ConstantValue: { HasValue: true, Value: null } }:
                case IDefaultValueOperation:
                    return ExpressionVerdict.Durable;

                // The lock-object idiom. A bare object reaches nothing, whereas the declared type 'object' cannot say
                // so, which would otherwise make every durable type that needs a lock unable to state its contract.
                case IObjectCreationOperation { Type.SpecialType: SpecialType.System_Object }:
                    return ExpressionVerdict.Durable;

                // What a delegate retains is the closure it was built from, which is visible here and is not visible
                // in the declared type.
                case IDelegateCreationOperation delegateCreation:
                    return this.GetClosureVerdict( delegateCreation.Target );

                case IAnonymousFunctionOperation or IMethodReferenceOperation:
                    return this.GetClosureVerdict( value );

                // The obligation was discharged where the value entered, so it is not imposed again where it moves.
                case IParameterReferenceOperation { Parameter: { } parameter } when HasDurableAttribute( parameter ):
                case IFieldReferenceOperation { Field: { } field } when HasDurableAttribute( field ):
                case IPropertyReferenceOperation { Property: { } property } when HasDurableAttribute( property ):
                    return ExpressionVerdict.Durable;

                // A throw expression yields no value.
                case IThrowOperation:
                    return ExpressionVerdict.Durable;

                // An expression that chooses between two operands is as durable as both of them. The idiom that makes
                // this necessary rather than merely tidy is 'value ?? throw new ArgumentNullException(...)', which is
                // how a constructor forwards a checked parameter into a durable field.
                case ICoalesceOperation coalesce:
                {
                    var valueVerdict = this.GetExpressionVerdict( coalesce.Value );

                    return valueVerdict.IsDurable ? this.GetExpressionVerdict( coalesce.WhenNull ) : valueVerdict;
                }

                case IConditionalOperation { WhenTrue: { } whenTrue, WhenFalse: { } whenFalse }:
                {
                    var whenTrueVerdict = this.GetExpressionVerdict( whenTrue );

                    return whenTrueVerdict.IsDurable ? this.GetExpressionVerdict( whenFalse ) : whenTrueVerdict;
                }

                // An anonymous object stores the values of its initializers and nothing else, and each value is visible
                // here. The values carry more evidence than the property types: a property of type object that is
                // initialized with a literal is durable, and a lambda assigned to a property is analyzed for captures.
                case IAnonymousObjectCreationOperation anonymousObjectCreation:
                    return this.GetAnonymousObjectCreationVerdict( anonymousObjectCreation );

                case IObjectCreationOperation { Initializer: { } } objectCreation:
                    return this.GetCollectionCreationVerdict( objectCreation );
            }

            return new ExpressionVerdict( this.GetVerdict( value.Type ), operand: value );
        }

        /// <summary>
        /// The names of the methods that convert a value to a durable value, in the order of preference.
        /// </summary>
        /// <remarks>
        /// <c>ToDurableRef</c> comes first because a value can support both methods: an <c>IParameter</c> is a
        /// declaration and an <c>IExpression</c>, and the durable reference keeps the declaration where the durable
        /// expression keeps only its text.
        /// </remarks>
        private static readonly string[] _durableConversionNames = { "ToDurableRef", "ToDurable" };

        /// <summary>
        /// Returns a method that converts the value of an expression to a durable value, or <c>null</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The method is looked up at the location of the expression, so an extension method is returned only when its
        /// namespace is imported there.
        /// </para>
        /// <para>
        /// The call that the suggestion proposes, <c>(value).Name()</c>, is then bound speculatively at that location.
        /// The suggestion is made only when overload resolution and type inference select a method, and when the
        /// result is not <c>void</c> and has a durable type. A method that cannot be called without arguments, or whose
        /// type arguments cannot be inferred, is therefore never suggested.
        /// </para>
        /// </remarks>
        public IMethodSymbol? FindDurableConversion( IOperation operand )
        {
            if ( operand is not { Type: { } type, SemanticModel: { } semanticModel, Syntax: ExpressionSyntax syntax }
                 || type.TypeKind == TypeKind.Error )
            {
                return null;
            }

            foreach ( var name in _durableConversionNames )
            {
                // The lookup is a cheap filter: it avoids parsing and binding a call for the types that have no method
                // of that name.
                var hasCandidate = semanticModel.LookupSymbols( syntax.SpanStart, type, name, includeReducedExtensionMethods: true )
                    .Any( s => s is IMethodSymbol { IsStatic: false } );

                if ( !hasCandidate )
                {
                    continue;
                }

                var call = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParenthesizedExpression( syntax.WithoutTrivia() ),
                        SyntaxFactory.IdentifierName( name ) ) );

                var symbolInfo = semanticModel.GetSpeculativeSymbolInfo( syntax.SpanStart, call, SpeculativeBindingOption.BindAsExpression );
                var typeInfo = semanticModel.GetSpeculativeTypeInfo( syntax.SpanStart, call, SpeculativeBindingOption.BindAsExpression );

                if ( symbolInfo.Symbol is IMethodSymbol { ReturnsVoid: false } method
                     && typeInfo.Type is { TypeKind: not TypeKind.Error } resultType
                     && this.GetVerdict( resultType ).IsDurable )
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// Evaluates an anonymous object creation expression by the value of each of its property initializers.
        /// </summary>
        private ExpressionVerdict GetAnonymousObjectCreationVerdict( IAnonymousObjectCreationOperation anonymousObjectCreation )
        {
            foreach ( var initializer in anonymousObjectCreation.Initializers )
            {
                if ( initializer is not ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property: { } property } } assignment )
                {
                    // The compiler produces no other form of initializer. The declared type is the only evidence left.
                    return new ExpressionVerdict( this.GetVerdict( anonymousObjectCreation.Type ), operand: anonymousObjectCreation );
                }

                var valueVerdict = this.GetExpressionVerdict( assignment.Value );

                if ( !valueVerdict.IsDurable )
                {
                    return Prepend( valueVerdict, "." + property.Name );
                }
            }

            return ExpressionVerdict.Durable;
        }

        /// <summary>
        /// Evaluates an object creation expression that has an initializer. When the created type is a system collection
        /// whose durability depends only on its type arguments, the expression is evaluated by its constructor arguments
        /// and by the elements of its initializer instead of by its type arguments.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This rule is sound only because the expression is the value that is stored or passed. No other code holds a
        /// reference to the new collection, so the elements of the initializer are all the elements that the collection
        /// contains when it reaches the durable member or parameter.
        /// </para>
        /// <para>
        /// The constructor arguments are evaluated because a constructor can store them, for instance a collection to
        /// copy or a comparer.
        /// </para>
        /// </remarks>
        private ExpressionVerdict GetCollectionCreationVerdict( IObjectCreationOperation objectCreation )
        {
            var typeVerdict = new ExpressionVerdict( this.GetVerdict( objectCreation.Type ), operand: objectCreation );

            if ( typeVerdict.IsDurable
                 || objectCreation is not { Type: INamedTypeSymbol createdType, Initializer: IObjectOrCollectionInitializerOperation initializer }
                 || !this.TryGetTransparentCollectionEntry( createdType, out var entry ) )
            {
                return typeVerdict;
            }

            foreach ( var argument in objectCreation.Arguments )
            {
                var argumentVerdict = this.GetExpressionVerdict( argument.Value );

                if ( !argumentVerdict.IsDurable )
                {
                    return argumentVerdict;
                }
            }

            foreach ( var element in initializer.Initializers )
            {
                switch ( element )
                {
                    // A collection initializer element, such as { 1 } or { "k", 1 }, which calls the Add method. An element
                    // whose call is bound dynamically is handled by the default case.
                    case IInvocationOperation invocation:
                        foreach ( var invocationArgument in invocation.Arguments )
                        {
                            if ( !IsKeptAlive( invocationArgument.Parameter?.OriginalDefinition.Type, entry ) )
                            {
                                continue;
                            }

                            var elementVerdict = this.GetExpressionVerdict( invocationArgument.Value );

                            if ( !elementVerdict.IsDurable )
                            {
                                return Prepend( elementVerdict, "[]" );
                            }
                        }

                        break;

                    // An index initializer element, such as ["k"] = 1, which calls the indexer.
                    case ISimpleAssignmentOperation { Target: IPropertyReferenceOperation { Property.IsIndexer: true } indexer } assignment:
                        foreach ( var indexArgument in indexer.Arguments )
                        {
                            if ( !IsKeptAlive( indexArgument.Parameter?.OriginalDefinition.Type, entry ) )
                            {
                                continue;
                            }

                            var indexVerdict = this.GetExpressionVerdict( indexArgument.Value );

                            if ( !indexVerdict.IsDurable )
                            {
                                return Prepend( indexVerdict, "[]" );
                            }
                        }

                        if ( IsKeptAlive( indexer.Property.OriginalDefinition.Type, entry ) )
                        {
                            var assignedVerdict = this.GetExpressionVerdict( assignment.Value );

                            if ( !assignedVerdict.IsDurable )
                            {
                                return Prepend( assignedVerdict, "[]" );
                            }
                        }

                        break;

                    // A member initializer, such as Capacity = 4, or a nested initializer. These forms are not analyzed,
                    // so the declared type is the evidence.
                    default:
                        return typeVerdict;
                }
            }

            return ExpressionVerdict.Durable;
        }

        /// <summary>
        /// Determines whether a collection keeps alive the value that it receives through a parameter of a given declared
        /// type, according to the mask of type arguments of its table entry.
        /// </summary>
        /// <remarks>
        /// The declared type is the one of the original definition, so that a parameter of type <c>TKey</c> is recognized
        /// as the first type parameter of <c>ConditionalWeakTable{TKey,TValue}</c>, whose key is held weakly. A parameter
        /// whose declared type is not a type parameter of the collection is always considered kept alive.
        /// </remarks>
        private static bool IsKeptAlive( ITypeSymbol? declaredType, WellKnownEntry entry )
            => entry.ArgumentMask.IsDefault
               || declaredType is not ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Type } typeParameter
               || entry.ArgumentMask.Contains( typeParameter.Ordinal );

        /// <summary>
        /// Returns a verdict identical to the given one except that a step is prepended to its chain. The captured symbol
        /// and the location are kept, so that a capture is still reported where it occurs.
        /// </summary>
        private static ExpressionVerdict Prepend( ExpressionVerdict verdict, string step )
            => new( verdict.DurabilityVerdict.Prepend( step ), verdict.CapturedSymbol, verdict.Location, verdict.Operand );

        /// <summary>
        /// Removes the conversions and parentheses that stand between an assignment or an argument and the expression
        /// that actually produces the value.
        /// </summary>
        private static IOperation Unwrap( IOperation value )
        {
            while ( true )
            {
                switch ( value )
                {
                    // Only a conversion the compiler performs. A conversion with an operator method produces an object
                    // of the target type, which the operand says nothing about: a durable operand can be converted to
                    // a type that is not durable, and the verdict has to be read from the result.
                    case IConversionOperation { OperatorMethod: null } conversion:
                        value = conversion.Operand;

                        break;

                    // A union conversion. The operator method is the creation member of the union, and the union stores
                    // the operand as the value of its case, so the operand is the value to evaluate. The union type
                    // itself would be judged by all its case types and not by the one the operand converts to.
                    case IConversionOperation { Conversion.IsUserDefined: false, OperatorMethod: not null, Type: { } unionType } conversion
                        when UnionFacts.IsUnion( unionType ):
                        value = conversion.Operand;

                        break;

                    case IParenthesizedOperation parenthesized:
                        value = parenthesized.Operand;

                        break;

                    default:
                        return value;
                }
            }
        }

        /// <summary>
        /// Evaluates what a delegate would retain.
        /// </summary>
        /// <remarks>
        /// This is the rule that matters most, because <c>design-time-memory.md</c> names the invisible capture the
        /// trap that costs the most: a lambda that mentions a compilation produces a closure object holding it, and
        /// whatever holds the delegate holds the compilation.
        /// </remarks>
        private ExpressionVerdict GetClosureVerdict( IOperation target )
        {
            switch ( Unwrap( target ) )
            {
                case IAnonymousFunctionOperation lambda:
                    return this.GetCapturedVerdict( lambda );

                case IMethodReferenceOperation methodReference:
                    // A static method group has no target to retain. An instance one retains its receiver.
                    if ( methodReference.Method.IsStatic || methodReference.Instance == null )
                    {
                        return ExpressionVerdict.Durable;
                    }

                    var instanceVerdict = this.GetVerdict( methodReference.Instance.Type );

                    return instanceVerdict.IsDurable
                        ? ExpressionVerdict.Durable
                        : new ExpressionVerdict(
                            instanceVerdict.Prepend( "receiver of " + methodReference.Method.Name ),
                            null,
                            methodReference.Syntax.GetLocation() );

                default:
                    // A delegate that arrives through a local, a field or another assembly is not visible here, so the
                    // declared type is the only evidence available and it says nothing good.
                    return new ExpressionVerdict( this.GetVerdict( target.Type ) );
            }
        }

        /// <summary>
        /// Analyses what a lambda captures.
        /// </summary>
        private ExpressionVerdict GetCapturedVerdict( IAnonymousFunctionOperation lambda )
        {
            var semanticModel = lambda.SemanticModel;

            if ( semanticModel == null || lambda.Syntax == null )
            {
                return ExpressionVerdict.Durable;
            }

            // A lambda that captures the enclosing instance holds everything that instance holds, and saying so is far
            // more useful than naming whichever field the body happened to touch.
            foreach ( var operation in Descendants( lambda ) )
            {
                if ( operation is IInstanceReferenceOperation
                    {
                        ReferenceKind: InstanceReferenceKind.ContainingTypeInstance, Type: { } containingType
                    } instanceReference )
                {
                    var thisVerdict = this.GetVerdict( containingType );

                    if ( !thisVerdict.IsDurable )
                    {
                        return new ExpressionVerdict(
                            thisVerdict.Prepend( "this" ),
                            null,
                            instanceReference.Syntax?.GetLocation() ?? lambda.Syntax.GetLocation() );
                    }

                    break;
                }
            }

            DataFlowAnalysis? dataFlow;

            try
            {
                dataFlow = semanticModel.AnalyzeDataFlow( lambda.Syntax );
            }
            catch ( System.ArgumentException )
            {
                // The region is not one the compiler can analyse, for instance in code that does not compile.
                return ExpressionVerdict.Durable;
            }

            if ( dataFlow is not { Succeeded: true } )
            {
                return ExpressionVerdict.Durable;
            }

            // Captured names every variable captured by a closure that the region takes part in, which includes the
            // closure of an enclosing local function or lambda. A lambda written inside a local function that captures
            // a variable is therefore reported as capturing that variable even when it never mentions it, and the
            // compiler gives such a lambda a cached static delegate that holds nothing. Intersecting with what the
            // region reads and writes keeps only what this lambda actually uses.
            var used = new HashSet<ISymbol>( SymbolEqualityComparer.Default );

            foreach ( var symbol in dataFlow.ReadInside )
            {
                used.Add( symbol );
            }

            foreach ( var symbol in dataFlow.WrittenInside )
            {
                used.Add( symbol );
            }

            foreach ( var captured in dataFlow.Captured )
            {
                if ( !used.Contains( captured ) )
                {
                    continue;
                }

                var capturedType = captured switch
                {
                    ILocalSymbol local => local.Type,
                    IParameterSymbol parameter => parameter.Type,
                    _ => null
                };

                if ( capturedType == null )
                {
                    continue;
                }

                // A captured variable that is itself under the contract carries an obligation already discharged.
                if ( captured is IParameterSymbol capturedParameter && HasDurableAttribute( capturedParameter ) )
                {
                    continue;
                }

                var capturedVerdict = this.GetVerdict( capturedType );

                if ( !capturedVerdict.IsDurable )
                {
                    return new ExpressionVerdict(
                        capturedVerdict.Prepend( captured.Name ),
                        captured,
                        FindUse( lambda, captured ) ?? lambda.Syntax.GetLocation() );
                }
            }

            return ExpressionVerdict.Durable;
        }

        /// <summary>
        /// Returns the location of the first use of a captured symbol inside a lambda, so that the diagnostic lands on
        /// the capture rather than on the whole lambda.
        /// </summary>
        private static Location? FindUse( IOperation lambda, ISymbol captured )
        {
            foreach ( var operation in Descendants( lambda ) )
            {
                var referenced = operation switch
                {
                    ILocalReferenceOperation local => (ISymbol) local.Local,
                    IParameterReferenceOperation parameter => parameter.Parameter,
                    _ => null
                };

                if ( referenced != null
                     && SymbolEqualityComparer.Default.Equals( referenced, captured )
                     && operation.Syntax != null )
                {
                    return operation.Syntax.GetLocation();
                }
            }

            return null;
        }

        private static IEnumerable<IOperation> Descendants( IOperation operation )
        {
            foreach ( var child in operation.ChildOperations )
            {
                yield return child;

                foreach ( var descendant in Descendants( child ) )
                {
                    yield return descendant;
                }
            }
        }
    }
}
