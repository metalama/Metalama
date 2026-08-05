// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Analyzers
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
            public Verdict Verdict { get; }

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

            public bool IsDurable => this.Verdict.IsDurable;

            public ExpressionVerdict( Verdict verdict, ISymbol? capturedSymbol = null, Location? location = null )
            {
                this.Verdict = verdict;
                this.CapturedSymbol = capturedSymbol;
                this.Location = location;
            }

            public static readonly ExpressionVerdict Durable = new( Verdict.Durable );
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
            }

            return new ExpressionVerdict( this.GetVerdict( value.Type ) );
        }

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
                    case IConversionOperation conversion:
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

            foreach ( var captured in dataFlow.Captured )
            {
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
