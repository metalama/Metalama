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
    /// Verifies that aspects, fabrics and validators are written in an immutable style: that every instance field is
    /// read-only and of an immutable type, and that every automatically implemented property has no setter and is of
    /// an immutable type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An aspect is instantiated once and reused for every target it applies to, and at design time across
    /// compilations. State kept on it therefore leaks from one target to the next, in an order the author does not
    /// control. <c>IAspect{T}.BuildAspect</c> already says so in its documentation; this analyzer checks it.
    /// </para>
    /// <para>
    /// The contract is declared with <c>ImmutableTypeAttribute</c> and propagates to every
    /// type that derives from or implements a type carrying it, which is what makes marking <c>IAspect</c> enough to
    /// check every aspect anyone writes. <c>[ImmutableType( false )]</c> on a class is the per-class opt-out.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public partial class ImmutableContractAnalyzer : DiagnosticAnalyzer
    {
        private const string _category = "Metalama";

        private const string _because =
            "'{0}' must be written in an immutable style because it is an aspect, a fabric or a validator";

        // Range: 0880-0889.

        /// <remarks>
        /// Reported only when the field is writeable from outside the type that declares it. A private field that is
        /// not read-only is left to <see cref="MemberIsWrittenOutsideConstructor"/> instead, which reports the
        /// assignment rather than the declaration. See the remark on <c>CanVerifyWrites</c>.
        /// </remarks>
        internal static readonly DiagnosticDescriptor FieldIsNotReadOnly = new(
            "LAMA0880",
            "A field of a type that must be immutable is writeable from outside the type",
            _because + ", but the field '{1}' is neither read-only nor private, so not every assignment to it can be "
            + "seen. Add the 'readonly' modifier, or make the field private.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        /// <remarks>
        /// Separate from <see cref="FieldIsNotReadOnly"/> although one predicate produces both, because the remedy is
        /// written differently. Note that a positional <c>record struct</c> falls here, because its generated setters
        /// are public, and that the location is then the primary constructor parameter, which is why the message
        /// names <c>readonly record struct</c>.
        /// </remarks>
        internal static readonly DiagnosticDescriptor PropertyHasSetter = new(
            "LAMA0881",
            "An automatic property of a type that must be immutable has a setter that is not private",
            _because + ", but the automatic property '{1}' has a setter that is not private, so not every assignment "
            + "to it can be seen. Replace 'set' with 'init', make the setter private, or, for a positional record "
            + "struct, declare it as a 'readonly record struct'.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        internal static readonly DiagnosticDescriptor MemberIsNotImmutable = new(
            "LAMA0882",
            "A member of a type that must be immutable is of a mutable type",
            _because + ", but '{1}' is not immutable. Path: {2}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        internal static readonly DiagnosticDescriptor BaseTypeIsNotImmutable = new(
            "LAMA0883",
            "A type that must be immutable derives from a mutable type",
            _because + ", but its base type '{1}' is not immutable. Path: {2}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        /// <remarks>
        /// Separate from <see cref="MemberIsNotImmutable"/> because the remedy differs in kind, not because the case
        /// is undecidable. Marking a class is verified where it is declared, against its own members. Marking an
        /// interface instead exports an obligation to every implementation, which the analyzer verifies in turn, but
        /// the guarantee reaches only the implementations it sees, so a project may reasonably weigh this rule
        /// differently from the others.
        /// </remarks>
        internal static readonly DiagnosticDescriptor InterfaceIsNotImmutable = new(
            "LAMA0884",
            "A member of a type that must be immutable is typed as an interface that is not marked immutable",
            _because + ", but '{1}' is not marked [ImmutableType]. Marking it requires every implementation "
            + "to be immutable, which this analyzer verifies. Path: {2}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        internal static readonly DiagnosticDescriptor UnknownDeclaredTypeName = new(
            "LAMA0885",
            "A declared immutable type name matches no type",
            "The name '{0}', declared in {1}, matches no type in this compilation. A generic type must be written "
            + "with its arity, as in System.Collections.Immutable.ImmutableArray`1.",
            _category,
            DiagnosticSeverity.Warning,
            true,
            customTags: WellKnownDiagnosticTags.CompilationEnd );

        /// <remarks>
        /// Reported so that every waiver is visible to a review without reading every file, which is the property
        /// that makes the attribute a better escape hatch than a <c>#pragma</c>.
        /// </remarks>
        internal static readonly DiagnosticDescriptor ContractIsWaived = new(
            "LAMA0886",
            "The immutable-style requirement is waived",
            "'{0}' waives the immutable-style requirement with [ImmutableType( false )]",
            _category,
            DiagnosticSeverity.Info,
            true );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                FieldIsNotReadOnly,
                PropertyHasSetter,
                MemberIsNotImmutable,
                BaseTypeIsNotImmutable,
                InterfaceIsNotImmutable,
                UnknownDeclaredTypeName,
                ContractIsWaived,
                MemberIsWrittenOutsideConstructor,
                MemberIsPassedByReference );

        public override void Initialize( AnalysisContext context )
        {
            context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction( InitializeCompilation );
        }

        private static void InitializeCompilation( CompilationStartAnalysisContext context )
        {
            // The compilation does not know IAspect, so nothing in it can be bound by the contract. This is the gate
            // that makes the analyzer free for a project that does not reference Metalama.
            var immutabilityContext = ImmutabilityContext.TryCreate( context.Compilation, context.Options );

            if ( immutabilityContext == null )
            {
                return;
            }

            context.RegisterSymbolAction( c => AnalyzeNamedType( c, immutabilityContext ), SymbolKind.NamedType );
            context.RegisterCompilationEndAction( c => AnalyzeDeclaredTypeNames( c, immutabilityContext ) );
            RegisterWriteSiteActions( context, immutabilityContext );
        }

        /// <remarks>
        /// The action is registered on the type rather than on the member so that the question "is this type bound by
        /// the contract?" is asked once per type instead of once per member, and so that the base type and the members
        /// are examined in one place. Unlike the sibling durability rules, this analyzer registers no operation
        /// action, so nothing of it runs per assignment or per argument.
        /// </remarks>
        private static void AnalyzeNamedType( SymbolAnalysisContext context, ImmutabilityContext immutabilityContext )
        {
            var type = (INamedTypeSymbol) context.Symbol;

            if ( !immutabilityContext.IsSubjectToContract( type ) )
            {
                ReportWaiver( context, immutabilityContext, type );

                return;
            }

            AnalyzeBaseType( context, immutabilityContext, type );
            AnalyzeMembers( context, immutabilityContext, type );
        }

        /// <summary>
        /// Reports a type that waives the contract, but only when it would otherwise have been bound by it, so that
        /// the attribute on an ordinary type that no rule reaches is not reported.
        /// </summary>
        private static void ReportWaiver( SymbolAnalysisContext context, ImmutabilityContext immutabilityContext, INamedTypeSymbol type )
        {
            if ( !ImmutabilityContext.HasWaiver( type ) )
            {
                return;
            }

            var wouldBeBound = false;

            for ( var baseType = type.BaseType; baseType != null && !wouldBeBound; baseType = baseType.BaseType )
            {
                wouldBeBound = immutabilityContext.IsSubjectToContract( baseType );
            }

            if ( !wouldBeBound )
            {
                foreach ( var interfaceType in type.AllInterfaces )
                {
                    if ( immutabilityContext.IsSubjectToContract( interfaceType ) )
                    {
                        wouldBeBound = true;

                        break;
                    }
                }
            }

            if ( !wouldBeBound )
            {
                return;
            }

            var location = type.Locations.FirstOrDefault( l => l.IsInSource );

            if ( location != null )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create( ContractIsWaived, location, SymbolFacts.GetDisplayName( type ) ) );
            }
        }

        private static void AnalyzeBaseType( SymbolAnalysisContext context, ImmutabilityContext immutabilityContext, INamedTypeSymbol type )
        {
            var baseType = type.BaseType;

            if ( baseType == null || baseType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType )
            {
                return;
            }

            var verdict = immutabilityContext.GetVerdict( baseType );

            if ( verdict.IsImmutable )
            {
                return;
            }

            // Reported on the type rather than on each inherited member, because the author of the derived type
            // cannot fix a member that the base type declares.
            var location = type.Locations.FirstOrDefault( l => l.IsInSource );

            if ( location == null )
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    BaseTypeIsNotImmutable,
                    location,
                    SymbolFacts.GetDisplayName( type ),
                    SymbolFacts.GetDisplayName( baseType ),
                    verdict.Prepend( SymbolFacts.GetDisplayName( type ) ).FormatChain() ) );
        }

        /// <remarks>
        /// One predicate expresses both stated rules. The backing field of <c>{ get; set; }</c> is not read-only and
        /// the backing field of <c>{ get; init; }</c> is, so
        /// <c>IFieldSymbol { IsStatic: false, IsConst: false, IsReadOnly: false }</c> catches a writeable field and a
        /// property with a setter at once. A positional <c>record class</c> and a <c>readonly record struct</c> pass;
        /// a positional <c>record struct</c> does not.
        /// </remarks>
        private static void AnalyzeMembers( SymbolAnalysisContext context, ImmutabilityContext immutabilityContext, INamedTypeSymbol type )
        {
            var examinedProperties = new HashSet<ISymbol>( SymbolEqualityComparer.Default );

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
                    examinedProperties.Add( declaredMember );
                }

                if ( IsAdviceMember( declaredMember ) )
                {
                    continue;
                }

                // A member that is not read-only is reported here only when the analyzer cannot see every assignment
                // to it. When it can, the assignment itself is reported instead, by the write-site rules, which is
                // both more precise and where the author can act.
                if ( !field.IsReadOnly && !CanVerifyWrites( declaredMember ) )
                {
                    // One diagnostic per member: a writeable member must be made read-only before the question of its
                    // type is worth asking, and reporting both at once would name two remedies for one edit.
                    Report( context, declaredMember, declaredMember is IPropertySymbol ? PropertyHasSetter : FieldIsNotReadOnly, type );

                    continue;
                }

                AnalyzeMemberType( context, immutabilityContext, type, declaredMember, field.Type );
            }

            // Roslyn exposes the backing field of an automatically implemented property of a source type, so the loop
            // above covers them. This one is a safety net for the shapes where it does not, and skips whatever the
            // loop above has already examined.
            foreach ( var member in type.GetMembers() )
            {
                if ( member is not IPropertySymbol { IsStatic: false, IsAbstract: false, IsExtern: false } property
                     || examinedProperties.Contains( property )
                     || !SymbolFacts.IsAutomaticallyImplemented( property )
                     || IsAdviceMember( property ) )
                {
                    continue;
                }

                if ( property.SetMethod is { IsInitOnly: false } && !CanVerifyWrites( property ) )
                {
                    Report( context, property, PropertyHasSetter, type );

                    continue;
                }

                AnalyzeMemberType( context, immutabilityContext, type, property, property.Type );
            }
        }

        private static void Report( SymbolAnalysisContext context, ISymbol declaredMember, DiagnosticDescriptor descriptor, INamedTypeSymbol type )
        {
            var location = declaredMember.Locations.FirstOrDefault( l => l.IsInSource );

            if ( location != null )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create( descriptor, location, SymbolFacts.GetDisplayName( type ), declaredMember.Name ) );
            }
        }

        private static void AnalyzeMemberType(
            SymbolAnalysisContext context,
            ImmutabilityContext immutabilityContext,
            INamedTypeSymbol type,
            ISymbol declaredMember,
            ITypeSymbol memberType )
        {
            var verdict = immutabilityContext.GetVerdict( memberType );

            if ( verdict.IsImmutable )
            {
                return;
            }

            var location = declaredMember.Locations.FirstOrDefault( l => l.IsInSource );

            if ( location == null )
            {
                return;
            }

            var descriptor = verdict.Kind == ImmutabilityKind.NotAnnotated ? InterfaceIsNotImmutable : MemberIsNotImmutable;

            var chain = verdict
                .Prepend( declaredMember.Name )
                .Prepend( SymbolFacts.GetDisplayName( type ) )
                .FormatChain();

            context.ReportDiagnostic(
                Diagnostic.Create(
                    descriptor,
                    location,
                    SymbolFacts.GetDisplayName( type ),
                    SymbolFacts.GetDisplayName( memberType ),
                    chain ) );
        }

        /// <summary>
        /// Reports a name declared in one of the MSBuild items, or in the built-in table of contract types, that
        /// matches no type, so that a typo is not silently a rule that never applies.
        /// </summary>
        /// <remarks>
        /// The built-in contract names are checked too, and not only the items, because those names refer to types of
        /// Metalama.Premium that this repository cannot see. A stale one would otherwise be a rule that never fires
        /// and that nothing reports.
        /// </remarks>
        private static void AnalyzeDeclaredTypeNames( CompilationAnalysisContext context, ImmutabilityContext immutabilityContext )
        {
            foreach ( var name in immutabilityContext.ImmutableTypeNames )
            {
                ReportIfUnknown( name, "MetalamaImmutableType" );
            }

            foreach ( var name in immutabilityContext.MutableTypeNames )
            {
                ReportIfUnknown( name, "MetalamaMutableType" );
            }

            foreach ( var name in immutabilityContext.ContractTypeNames )
            {
                ReportIfUnknown( name, "MetalamaImmutableContractType" );
            }

            void ReportIfUnknown( string name, string itemName )
            {
                if ( context.Compilation.GetTypeByMetadataName( name ) == null )
                {
                    context.ReportDiagnostic( Diagnostic.Create( UnknownDeclaredTypeName, Location.None, name, itemName ) );
                }
            }
        }
    }
}
