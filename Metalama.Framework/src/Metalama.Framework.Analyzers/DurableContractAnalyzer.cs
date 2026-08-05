// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// Verifies the contract stated by the <c>Durable</c> attribute: that a type so marked is safe to be held across
    /// compilations, because every instance field and automatically implemented property of that type is durable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At design time the analysis process lives for as long as the solution is open and Roslyn produces a new
    /// compilation on essentially every keystroke, so an object that outlives a single request must not reach a
    /// compilation. The rule is documented in <c>Metalama.Framework/docs/design-time-memory.md</c>.
    /// </para>
    /// <para>
    /// This analyzer is the static counterpart of <c>UserCodeRetentionAnalyzer</c>, which walks the live object graph
    /// during a build when <c>MetalamaDiagnoseMemoryLeaks</c> is set. The two are complementary: the walker finds what
    /// a lambda actually captured but needs a build and a full pipeline execution, whereas this analyzer finds what a
    /// declared type permits, in the editor, before anything runs.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public class DurableContractAnalyzer : DiagnosticAnalyzer
    {
        private const string _category = "Metalama";

        // Range: 0870-0879.
        internal static readonly DiagnosticDescriptor MemberIsNotDurable = new(
            "LAMA0870",
            "A member of a durable type is not durable",
            "'{0}' does not satisfy the [Durable] contract because '{1}' is not durable. Retention path: {2}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        internal static readonly DiagnosticDescriptor BaseTypeIsNotDurable = new(
            "LAMA0873",
            "A durable type derives from a type that is not durable",
            "'{0}' is marked [Durable] but its base type '{1}' is not durable. Retention path: {2}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        internal static readonly DiagnosticDescriptor DurabilityCannotBeEstablished = new(
            "LAMA0876",
            "The durability of a type cannot be established",
            "'{0}' does not satisfy the [Durable] contract because the durability of '{1}' cannot be established. "
            + "Mark that type [Durable], or use a type that is durable. Retention path: {2}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create( MemberIsNotDurable, BaseTypeIsNotDurable, DurabilityCannotBeEstablished );

        public override void Initialize( AnalysisContext context )
        {
            context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction( InitializeCompilation );
        }

        private static void InitializeCompilation( CompilationStartAnalysisContext context )
        {
            // The compilation does not know the attribute, so nothing in it can be bound by the contract. This is the
            // gate that makes the analyzer free for a project that does not reference Metalama.
            var durabilityContext = DurabilityContext.TryCreate( context.Compilation, context.Options );

            if ( durabilityContext == null )
            {
                return;
            }

            context.RegisterSymbolAction( c => AnalyzeNamedType( c, durabilityContext ), SymbolKind.NamedType );
        }

        /// <remarks>
        /// The action is registered on the type rather than on the member so that the question "is this type bound by
        /// the contract?" is asked once per type instead of once per member, and so that the base type and the members
        /// are examined in one place.
        /// </remarks>
        private static void AnalyzeNamedType( SymbolAnalysisContext context, DurabilityContext durabilityContext )
        {
            var type = (INamedTypeSymbol) context.Symbol;

            if ( !durabilityContext.IsSubjectToContract( type ) )
            {
                return;
            }

            AnalyzeBaseType( context, durabilityContext, type );
            AnalyzeMembers( context, durabilityContext, type );
        }

        private static void AnalyzeBaseType( SymbolAnalysisContext context, DurabilityContext durabilityContext, INamedTypeSymbol type )
        {
            var baseType = type.BaseType;

            if ( baseType == null || baseType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType )
            {
                return;
            }

            var verdict = durabilityContext.GetVerdict( baseType );

            if ( verdict.IsDurable )
            {
                return;
            }

            // Reported on the type rather than on each inherited member, because the author of the derived type
            // cannot fix a member that the base type declares, and one diagnostic naming the base type is actionable
            // where a dozen naming its fields are not.
            var location = type.Locations.FirstOrDefault( l => l.IsInSource );

            if ( location == null )
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    BaseTypeIsNotDurable,
                    location,
                    DurabilityContext.GetDisplayName( type ),
                    DurabilityContext.GetDisplayName( baseType ),
                    verdict.Prepend( DurabilityContext.GetDisplayName( type ) ).FormatChain() ) );
        }

        private static void AnalyzeMembers( SymbolAnalysisContext context, DurabilityContext durabilityContext, INamedTypeSymbol type )
        {
            var reportedProperties = new HashSet<ISymbol>( SymbolEqualityComparer.Default );

            foreach ( var member in type.GetMembers() )
            {
                if ( member is not IFieldSymbol { IsStatic: false, IsConst: false } field )
                {
                    continue;
                }

                // An automatically implemented property is analyzed through its backing field, but the diagnostic is
                // reported on the property, which is what the author declared and what a positional record shows as a
                // parameter.
                var declaredMember = field.AssociatedSymbol ?? field;

                if ( declaredMember is IPropertySymbol )
                {
                    reportedProperties.Add( declaredMember );
                }

                AnalyzeMember( context, durabilityContext, type, declaredMember, field.Type );
            }

            // Roslyn exposes the backing field of an automatically implemented property of a source type, so the loop
            // above covers them. This one is a safety net for the shapes where it does not, and skips whatever the
            // loop above has already examined.
            foreach ( var member in type.GetMembers() )
            {
                if ( member is not IPropertySymbol { IsStatic: false, IsAbstract: false, IsExtern: false } property
                     || reportedProperties.Contains( property )
                     || !IsAutomaticallyImplemented( property ) )
                {
                    continue;
                }

                AnalyzeMember( context, durabilityContext, type, property, property.Type );
            }
        }

        private static void AnalyzeMember(
            SymbolAnalysisContext context,
            DurabilityContext durabilityContext,
            INamedTypeSymbol type,
            ISymbol declaredMember,
            ITypeSymbol memberType )
        {
            // The attribute on the member waives the check on the declared type and requires instead that every value
            // assigned to it be durable, which is the form to use for a member typed as an interface or as object.
            if ( DurabilityContext.HasDurableAttribute( declaredMember ) )
            {
                return;
            }

            var verdict = durabilityContext.GetVerdict( memberType );

            if ( verdict.IsDurable )
            {
                return;
            }

            var location = declaredMember.Locations.FirstOrDefault( l => l.IsInSource );

            if ( location == null )
            {
                return;
            }

            var descriptor = verdict.Kind == DurabilityKind.Unprovable ? DurabilityCannotBeEstablished : MemberIsNotDurable;

            var chain = verdict
                .Prepend( declaredMember.Name )
                .Prepend( DurabilityContext.GetDisplayName( type ) )
                .FormatChain();

            context.ReportDiagnostic(
                Diagnostic.Create(
                    descriptor,
                    location,
                    DurabilityContext.GetDisplayName( type ),
                    DurabilityContext.GetDisplayName( memberType ),
                    chain ) );
        }

        /// <summary>
        /// Determines whether a property is automatically implemented, that is, whether its accessors have no body.
        /// </summary>
        private static bool IsAutomaticallyImplemented( IPropertySymbol property )
        {
            var accessor = property.GetMethod ?? property.SetMethod;

            if ( accessor == null || accessor.DeclaringSyntaxReferences.IsDefaultOrEmpty )
            {
                return false;
            }

            foreach ( var reference in accessor.DeclaringSyntaxReferences )
            {
                if ( reference.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.AccessorDeclarationSyntax
                    { Body: null, ExpressionBody: null } )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
