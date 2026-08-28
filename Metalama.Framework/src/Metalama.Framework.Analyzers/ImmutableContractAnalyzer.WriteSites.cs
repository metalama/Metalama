// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Metalama.Framework.Analyzers.Immutability
{
    /// <summary>
    /// The rules that verify where a member is written, for the members whose every write the analyzer can see.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>readonly</c> is the strongest way to state that a field is set once, because the compiler checks it. It is
    /// not the only way, and demanding it rejects two shapes that are perfectly immutable in practice: a private
    /// field assigned from more than one place in a constructor chain, and a property with a private setter, which is
    /// also what a hand-written <c>ReferenceTypeSerializer</c> needs in order to restore an object after
    /// <c>CreateInstance</c>.
    /// </para>
    /// <para>
    /// So when the write access of a member is private, the declaration is accepted and these rules check the
    /// assignments instead. That is sound precisely because private write access confines every assignment to the
    /// declaring type, which is in this compilation, so the analyzer sees all of them. Where write access is wider
    /// the analyzer cannot see the calls, and the declaration rules report instead.
    /// </para>
    /// <para>
    /// The diagnostic is reported at the assignment rather than at the declaration, because that is the line that has
    /// to change.
    /// </para>
    /// </remarks>
    public partial class ImmutableContractAnalyzer
    {
        internal static readonly DiagnosticDescriptor MemberIsWrittenOutsideConstructor = new(
            "LAMA0887",
            "A member of a type that must be immutable is written outside a constructor",
            _because + ", but '{1}' is assigned here, outside a constructor. Move the assignment into a constructor "
            + "or an 'init' accessor.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        /// <remarks>
        /// A <c>ref</c> or <c>out</c> argument is a write, and one that is easy to miss because it does not look like
        /// an assignment. <c>out</c> always writes; <c>ref</c> may.
        /// </remarks>
        internal static readonly DiagnosticDescriptor MemberIsPassedByReference = new(
            "LAMA0888",
            "A member of a type that must be immutable is passed as a 'ref' or 'out' argument",
            _because + ", but '{1}' is passed as a '{2}' argument here, which writes it. Pass a local instead, and "
            + "assign the member in a constructor.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        private static void RegisterWriteSiteActions( CompilationStartAnalysisContext context, ImmutabilityContext immutabilityContext )
        {
            context.RegisterOperationAction(
                c => AnalyzeWrite( c, immutabilityContext ),
                OperationKind.SimpleAssignment,
                OperationKind.CompoundAssignment,
                OperationKind.CoalesceAssignment,
                OperationKind.Increment,
                OperationKind.Decrement );

            context.RegisterOperationAction( c => AnalyzeArgument( c, immutabilityContext ), OperationKind.Argument );
        }

        private static void AnalyzeWrite( OperationAnalysisContext context, ImmutabilityContext immutabilityContext )
        {
            var target = context.Operation switch
            {
                IAssignmentOperation assignment => assignment.Target,
                IIncrementOrDecrementOperation incrementOrDecrement => incrementOrDecrement.Target,
                _ => null
            };

            if ( target == null || IsInitializerAssignment( context.Operation ) )
            {
                return;
            }

            Report( context, immutabilityContext, target, MemberIsWrittenOutsideConstructor, null );
        }

        private static void AnalyzeArgument( OperationAnalysisContext context, ImmutabilityContext immutabilityContext )
        {
            var argument = (IArgumentOperation) context.Operation;

            // The cheapest possible test first: this action runs on every argument of every project that references
            // Metalama, and all but a few are passed by value.
            if ( argument.Parameter is not { RefKind: RefKind.Ref or RefKind.Out } parameter )
            {
                return;
            }

            Report(
                context,
                immutabilityContext,
                argument.Value,
                MemberIsPassedByReference,
                parameter.RefKind == RefKind.Out ? "out" : "ref" );
        }

        private static void Report(
            OperationAnalysisContext context,
            ImmutabilityContext immutabilityContext,
            IOperation target,
            DiagnosticDescriptor descriptor,
            string? refKind )
        {
            var member = target switch
            {
                IFieldReferenceOperation field => (ISymbol) field.Field,

                // Only an automatically implemented property, so that these rules examine exactly the members the
                // declaration rules examine. A property with a body is not state of its own: whatever it assigns is a
                // field, and the assignment inside its accessor is reported there instead, which is the better place.
                IPropertyReferenceOperation property when SymbolFacts.IsAutomaticallyImplemented( property.Property )
                    => property.Property,

                _ => null
            };

            if ( member is not { IsStatic: false } || member is IFieldSymbol { IsConst: true } )
            {
                return;
            }

            var declaringType = member.ContainingType;

            if ( declaringType == null || !immutabilityContext.IsSubjectToContract( declaringType ) )
            {
                return;
            }

            // An advice member is code injected into the target, not state of the aspect, so writing it is not a
            // defect. The declaration rules waive it for the same reason.
            var declaredMember = member is IFieldSymbol { AssociatedSymbol: { } associated } ? associated : member;

            if ( IsAdviceMember( declaredMember ) )
            {
                return;
            }

            if ( IsInConstructorOrInitAccessor( context.Operation, context.ContainingSymbol, declaringType ) )
            {
                return;
            }

            var location = target.Syntax.GetLocation();

            context.ReportDiagnostic(
                refKind == null
                    ? Diagnostic.Create( descriptor, location, SymbolFacts.GetDisplayName( declaringType ), declaredMember.Name )
                    : Diagnostic.Create(
                        descriptor,
                        location,
                        SymbolFacts.GetDisplayName( declaringType ),
                        declaredMember.Name,
                        refKind ) );
        }

        /// <summary>
        /// Determines whether an assignment is part of an object initializer or of a <c>with</c> expression.
        /// </summary>
        /// <remarks>
        /// Neither is a mutation. <c>new C { X = 1 }</c> and <c>options with { X = 1 }</c> both assign a freshly
        /// created object, which is exactly what an <c>init</c> accessor exists to allow, and the compiler already
        /// restricts them to that. Without this the rule reports every <c>with</c> expression over an immutable
        /// record, which is the idiomatic way to write one.
        /// </remarks>
        private static bool IsInitializerAssignment( IOperation operation )
            => operation.Parent is IObjectOrCollectionInitializerOperation;

        /// <summary>
        /// Determines whether an operation is in a place where writing a member of the type is legitimate, that is,
        /// in a constructor or an <c>init</c> accessor of the type that declares it.
        /// </summary>
        /// <remarks>
        /// A lambda or a local function is excluded even when it is written inside a constructor, because it does not
        /// run there: the delegate may be stored and invoked at any later time, so an assignment in its body is not
        /// part of construction. Field and property initializers are not assignments and never reach this rule.
        /// </remarks>
        private static bool IsInConstructorOrInitAccessor( IOperation operation, ISymbol containingSymbol, INamedTypeSymbol declaringType )
        {
            for ( var current = operation.Parent; current != null; current = current.Parent )
            {
                if ( current is IAnonymousFunctionOperation or ILocalFunctionOperation )
                {
                    return false;
                }
            }

            return containingSymbol is IMethodSymbol method
                   && SymbolEqualityComparer.Default.Equals( method.ContainingType, declaringType )
                   && (method.MethodKind == MethodKind.Constructor
                       || (method.MethodKind == MethodKind.PropertySet && method.IsInitOnly));
        }

        /// <summary>
        /// Determines whether the analyzer can see every assignment to a member, which is the case exactly when the
        /// member can only be written from the type that declares it.
        /// </summary>
        /// <remarks>
        /// Private write access confines every assignment to the declaring type, and therefore to this compilation,
        /// including from a nested type and from another part of a partial declaration. Anything wider — internal,
        /// protected, public — can be written by code the analyzer never sees, so for those the declaration rules
        /// still demand <c>readonly</c> or <c>init</c>.
        /// </remarks>
        private static bool CanVerifyWrites( ISymbol declaredMember )
            => declaredMember switch
            {
                IPropertySymbol { SetMethod: { } setMethod } => setMethod.DeclaredAccessibility == Accessibility.Private,
                IPropertySymbol => true,
                IFieldSymbol field => field.DeclaredAccessibility == Accessibility.Private,
                _ => false
            };
    }
}
