// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// The rules that apply where a value is stored or passed, rather than where a type is declared.
    /// </summary>
    public partial class DurableContractAnalyzer
    {
        internal static readonly DiagnosticDescriptor AssignedValueIsNotDurable = new(
            "LAMA0871",
            "A durable member is assigned a value that is not durable",
            "'{0}' is marked [Durable] but is assigned a value of type '{1}', which is not durable. "
            + "Retention path: {2}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        internal static readonly DiagnosticDescriptor ArgumentIsNotDurable = new(
            "LAMA0872",
            "A durable parameter receives an argument that is not durable",
            "The parameter '{0}' is marked [Durable] but the argument is of type '{1}', which is not durable. "
            + "Retention path: {2}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        /// <remarks>
        /// The rule that the design document calls the trap that costs the most, because the captured object is
        /// invisible in the source: a lambda that mentions a compilation produces a closure object holding it, and
        /// whatever holds the delegate holds the compilation.
        /// </remarks>
        internal static readonly DiagnosticDescriptor CapturedValueIsNotDurable = new(
            "LAMA0878",
            "A lambda in a durable position captures a value that is not durable",
            "This lambda is used where a durable value is required, and it captures '{0}', which is not durable. "
            + "Retention path: {1}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        /// <remarks>
        /// A weak reference is durable whatever it refers to, so nothing else in this analyzer would say anything
        /// about it. The convention that names such a member <c>Dangerous</c> exists so that a reader knows the value
        /// may be absent and that the caller is responsible for establishing that it is not, and a convention an
        /// analyzer can enforce for nothing should be enforced.
        /// </remarks>
        internal static readonly DiagnosticDescriptor WeakReferenceShouldBeNamedDangerous = new(
            "LAMA0875",
            "A weak reference held by a durable type should be named Dangerous",
            "'{0}' holds a weak reference, so its name should end in 'Dangerous' to show that the value may be absent",
            _category,
            DiagnosticSeverity.Info,
            true );

        internal static readonly DiagnosticDescriptor UnknownDeclaredTypeName = new(
            "LAMA0879",
            "A declared durable type name matches no type",
            "The name '{0}', declared in {1}, matches no type in this compilation. A generic type must be written "
            + "with its arity, as in System.Collections.Generic.List`1.",
            _category,
            DiagnosticSeverity.Warning,
            true,
            customTags: WellKnownDiagnosticTags.CompilationEnd );

        private static void RegisterUseSiteActions( CompilationStartAnalysisContext context, DurabilityContext durabilityContext )
        {
            context.RegisterCompilationEndAction( c => AnalyzeDeclaredTypeNames( c, durabilityContext ) );

            // A compound assignment is registered for the sake of a member of delegate type, where 'handler += lambda'
            // is the idiomatic way to store one and retains the closure exactly as a simple assignment does. For every
            // other operator the right operand has the type of the member itself, whose verdict is already known, so
            // this widens the rule without widening what it reports.
            context.RegisterOperationAction(
                c => AnalyzeAssignment( c, durabilityContext ),
                OperationKind.SimpleAssignment,
                OperationKind.CompoundAssignment,
                OperationKind.CoalesceAssignment,
                OperationKind.DeconstructionAssignment );

            context.RegisterOperationAction(
                c => AnalyzeInitializer( c, durabilityContext ),
                OperationKind.FieldInitializer,
                OperationKind.PropertyInitializer );

            context.RegisterOperationAction( c => AnalyzeArgument( c, durabilityContext ), OperationKind.Argument );
        }

        /// <summary>
        /// Reports a name declared in <c>MetalamaDurableType</c> or <c>MetalamaNonDurableType</c> that matches no type,
        /// so that a typo is not silently a rule that never applies.
        /// </summary>
        private static void AnalyzeDeclaredTypeNames( CompilationAnalysisContext context, DurabilityContext durabilityContext )
        {
            foreach ( var name in durabilityContext.DurableTypeNames )
            {
                ReportIfUnknown( name, "MetalamaDurableType" );
            }

            foreach ( var name in durabilityContext.NonDurableTypeNames )
            {
                ReportIfUnknown( name, "MetalamaNonDurableType" );
            }

            void ReportIfUnknown( string name, string itemName )
            {
                if ( context.Compilation.GetTypeByMetadataName( name ) == null )
                {
                    context.ReportDiagnostic( Diagnostic.Create( UnknownDeclaredTypeName, Location.None, name, itemName ) );
                }
            }
        }

        private static void AnalyzeAssignment( OperationAnalysisContext context, DurabilityContext durabilityContext )
        {
            var assignment = (IAssignmentOperation) context.Operation;

            // A deconstruction assigns every element of a tuple target, so the elements have to be paired with the
            // elements of the value. Without this, (this._value, _) = (tree, 0) stores a syntax tree in a durable
            // member and reports nothing.
            if ( assignment is IDeconstructionAssignmentOperation
                 && assignment.Target is ITupleOperation targetTuple )
            {
                AnalyzeDeconstruction( context, durabilityContext, targetTuple, assignment.Value );

                return;
            }

            var target = assignment.Target switch
            {
                IFieldReferenceOperation field => (ISymbol) field.Field,
                IPropertyReferenceOperation property => property.Property,
                _ => null
            };

            // The cheapest possible test first: this action runs on every assignment of every project that references
            // Metalama, so it must cost nothing when the target does not carry the attribute.
            if ( target == null || !DurabilityContext.HasDurableAttribute( target ) )
            {
                return;
            }

            ReportIfNotDurable( context, durabilityContext, assignment.Value, AssignedValueIsNotDurable, target.Name );
        }

        /// <summary>
        /// Checks each element of a deconstruction target against the element of the value that supplies it.
        /// </summary>
        /// <remarks>
        /// The elements are paired positionally, which is what a deconstruction into a tuple literal does. When the
        /// value is not a tuple literal of the same arity, as when it is a call to a Deconstruct method, the elements
        /// cannot be paired and the durability of each target is judged from the type of the whole value instead,
        /// which is the conservative reading.
        /// </remarks>
        private static void AnalyzeDeconstruction(
            OperationAnalysisContext context,
            DurabilityContext durabilityContext,
            ITupleOperation targetTuple,
            IOperation value )
        {
            var valueTuple = value as ITupleOperation;

            if ( valueTuple != null && valueTuple.Elements.Length != targetTuple.Elements.Length )
            {
                valueTuple = null;
            }

            for ( var i = 0; i < targetTuple.Elements.Length; i++ )
            {
                var element = targetTuple.Elements[i];

                if ( element is ITupleOperation nestedTuple )
                {
                    AnalyzeDeconstruction(
                        context,
                        durabilityContext,
                        nestedTuple,
                        valueTuple != null ? valueTuple.Elements[i] : value );

                    continue;
                }

                var target = element switch
                {
                    IFieldReferenceOperation field => (ISymbol) field.Field,
                    IPropertyReferenceOperation property => property.Property,
                    _ => null
                };

                if ( target == null || !DurabilityContext.HasDurableAttribute( target ) )
                {
                    continue;
                }

                ReportIfNotDurable(
                    context,
                    durabilityContext,
                    valueTuple != null ? valueTuple.Elements[i] : value,
                    AssignedValueIsNotDurable,
                    target.Name );
            }
        }

        private static void AnalyzeInitializer( OperationAnalysisContext context, DurabilityContext durabilityContext )
        {
            ISymbol? target = context.Operation switch
            {
                IFieldInitializerOperation { InitializedFields.Length: > 0 } field => field.InitializedFields[0],
                IPropertyInitializerOperation { InitializedProperties.Length: > 0 } property => property.InitializedProperties[0],
                _ => null
            };

            if ( target == null || !DurabilityContext.HasDurableAttribute( target ) )
            {
                return;
            }

            var value = ((ISymbolInitializerOperation) context.Operation).Value;

            ReportIfNotDurable( context, durabilityContext, value, AssignedValueIsNotDurable, target.Name );
        }

        private static void AnalyzeArgument( OperationAnalysisContext context, DurabilityContext durabilityContext )
        {
            var argument = (IArgumentOperation) context.Operation;

            // IArgumentOperation.Parameter resolves the target of a named, optional or params argument, and covers an
            // object creation as well as an invocation, which is why the rule is registered on the argument rather
            // than on each of the operations that can carry one.
            if ( argument.Parameter is not { } parameter )
            {
                return;
            }

            if ( DurabilityContext.HasDurableAttribute( parameter ) )
            {
                ReportIfNotDurable( context, durabilityContext, argument.Value, ArgumentIsNotDurable, parameter.Name );

                return;
            }

            // An out argument writes the member it is given, so Set( out this._value ) stores whatever the callee
            // produces into a durable member without any assignment appearing here. The declared type of the
            // parameter is what the member receives, so it is what has to be durable.
            //
            // A ref argument is deliberately not reported. It may only read: Volatile.Read( ref this._factory ) in
            // DurableLazy passes a durable member by reference and stores nothing, and judging it by the parameter
            // type reported it as though it did. Distinguishing Volatile.Read from Interlocked.Exchange is beyond
            // what a declared signature says, so the choice here is silence rather than a wrong finding.
            if ( parameter.RefKind == RefKind.Out
                 && GetDurableMember( argument.Value ) is { } member )
            {
                var verdict = durabilityContext.GetVerdict( parameter.Type );

                if ( !verdict.IsDurable )
                {
                    var location = argument.Value.Syntax.GetLocation();

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            AssignedValueIsNotDurable,
                            location,
                            member.Name,
                            DurabilityContext.GetDisplayName( parameter.Type ),
                            verdict.FormatChain() ) );
                }
            }
        }

        /// <summary>
        /// Returns the field or automatically implemented property that an expression refers to, when it carries the
        /// attribute, or <c>null</c>.
        /// </summary>
        private static ISymbol? GetDurableMember( IOperation value )
            => value switch
            {
                IFieldReferenceOperation field when DurabilityContext.HasDurableAttribute( field.Field )
                    => field.Field,
                IPropertyReferenceOperation property when DurabilityContext.HasDurableAttribute( property.Property )
                    => property.Property,
                _ => null
            };

        private static void ReportIfNotDurable(
            OperationAnalysisContext context,
            DurabilityContext durabilityContext,
            IOperation? value,
            DiagnosticDescriptor descriptor,
            string targetName )
        {
            if ( value == null )
            {
                return;
            }

            var verdict = durabilityContext.GetExpressionVerdict( value );

            if ( verdict.IsDurable )
            {
                return;
            }

            // A verdict that carries a location came from a closure, so the diagnostic is the one about captures and
            // is reported at the capture rather than at the assignment.
            if ( verdict.Location != null )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        CapturedValueIsNotDurable,
                        verdict.Location,
                        verdict.CapturedSymbol?.Name ?? "this",
                        verdict.DurabilityVerdict.FormatChain() ) );

                return;
            }

            var location = value.Syntax?.GetLocation();

            if ( location == null )
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    descriptor,
                    location,
                    targetName,
                    value.Type == null ? "?" : DurabilityContext.GetDisplayName( value.Type ),
                    verdict.DurabilityVerdict.FormatChain() ) );
        }
    }
}
