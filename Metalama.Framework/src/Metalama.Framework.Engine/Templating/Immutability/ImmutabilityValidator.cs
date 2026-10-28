// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Templating.Immutability
{
    /// <summary>
    /// Verifies that compile-time code is written in an immutable style: that the types bound by the
    /// <c>[ImmutableType]</c> contract, which include every aspect, fabric and validator, have only read-only instance
    /// members of immutable types, and that compile-time types keep no mutable static state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An aspect is instantiated once and reused for every target it applies to, and at design time across
    /// compilations. State kept on it therefore leaks from one target to the next, in an order the author does not
    /// control. A static field of a compile-time type is shared even more widely.
    /// </para>
    /// <para>
    /// The contract is declared with <c>ImmutableTypeAttribute</c> and propagates to every type that derives from or
    /// implements a type carrying it, which is what makes marking <c>IAspect</c> enough to check every aspect. There is
    /// no per-type waiver: where the contract is not wanted on one declaration, the ordinary suppression mechanisms
    /// apply.
    /// </para>
    /// <para>
    /// This class is called by <see cref="TemplatingCodeValidator"/> for each declaration of one syntax tree, which
    /// provides the execution scope of the declaration. Only compile-time and run-time-or-compile-time types are
    /// examined. Every diagnostic is reported on a declaration or an expression of the current syntax tree, so that a
    /// partial type that spans several trees is not reported twice.
    /// </para>
    /// </remarks>
    internal sealed partial class ImmutabilityValidator
    {
        private readonly ImmutabilityContext _context;
        private readonly IProjectOptions? _projectOptions;
        private readonly ISymbolClassifier _classifier;
        private readonly SemanticModel _semanticModel;
        private readonly Action<Diagnostic> _reportDiagnostic;

        /// <summary>
        /// The types whose declaration-wide rules have been applied in the current syntax tree. A partial type can have
        /// several declarations in one tree, and these rules examine all the members of the type at once.
        /// </summary>
        private readonly HashSet<INamedTypeSymbol> _validatedTypes = new( SymbolEqualityComparer.Default );

        private ImmutabilityValidator(
            ImmutabilityContext context,
            IProjectOptions? projectOptions,
            ISymbolClassifier classifier,
            SemanticModel semanticModel,
            Action<Diagnostic> reportDiagnostic )
        {
            this._context = context;
            this._projectOptions = projectOptions;
            this._classifier = classifier;
            this._semanticModel = semanticModel;
            this._reportDiagnostic = reportDiagnostic;
        }

        /// <summary>
        /// Creates a validator for one syntax tree, or returns <c>null</c> when the project turns the contract off.
        /// </summary>
        public static ImmutabilityValidator? Create(
            IProjectOptions? projectOptions,
            ISymbolClassifier classifier,
            SemanticModel semanticModel,
            Action<Diagnostic> reportDiagnostic )
        {
            var context = ImmutabilityContext.Create( projectOptions );

            return context == null ? null : new ImmutabilityValidator( context, projectOptions, classifier, semanticModel, reportDiagnostic );
        }

        private SyntaxTree SyntaxTree => this._semanticModel.SyntaxTree;

        /// <summary>
        /// Validates a compile-time or run-time-or-compile-time type declared in the current syntax tree.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="declaration">The declaration of the type in the current syntax tree.</param>
        public void ValidateType( INamedTypeSymbol type, SyntaxNode declaration )
        {
            if ( this._validatedTypes.Add( type ) && this._context.IsSubjectToContract( type ) )
            {
                this.ValidateRedundantAttribute( type );
                this.ValidateBaseType( type );
                this.ValidateInstanceMembers( type );
            }

            this.ValidateWriteSites( declaration );
        }

        private bool IsInCurrentTree( Location? location ) => location != null && location.SourceTree == this.SyntaxTree;

        private static Location? GetSourceLocation( ISymbol symbol ) => symbol.Locations.FirstOrDefault( l => l.IsInSource );

        /// <summary>
        /// Determines whether a member is advice rather than state.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A member marked with <c>[Template]</c>, <c>[Introduce]</c>, <c>[InterfaceMember]</c> or another advice attribute
        /// is code that the aspect injects into its target. It is not state of the aspect, and it is routinely written as
        /// <c>{ get; set; }</c> or as a writeable field.
        /// </para>
        /// <para>
        /// <see cref="ISymbolClassifier.GetTemplateInfo"/> follows the overridden members but not the interface members
        /// that a property implements, explicitly or implicitly, so these cases are resolved here.
        /// </para>
        /// </remarks>
        private bool IsAdviceMember( ISymbol member )
        {
            if ( !this._classifier.GetTemplateInfo( member ).IsNone )
            {
                return true;
            }

            if ( member.Kind != SymbolKind.Property || member is not IPropertySymbol { ContainingType: { } containingType } property )
            {
                return false;
            }

            // An explicit implementation has a qualified name, so the lookup by name below cannot find it.
            foreach ( var implementedProperty in property.ExplicitInterfaceImplementations )
            {
                if ( !this._classifier.GetTemplateInfo( implementedProperty ).IsNone )
                {
                    return true;
                }
            }

            foreach ( var interfaceType in containingType.AllInterfaces )
            {
                foreach ( var interfaceMember in interfaceType.GetMembers( member.Name ) )
                {
                    if ( interfaceMember.Kind == SymbolKind.Property
                         && SymbolEqualityComparer.Default.Equals( containingType.FindImplementationForInterfaceMember( interfaceMember ), member )
                         && !this._classifier.GetTemplateInfo( interfaceMember ).IsNone )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ValidateBaseType( INamedTypeSymbol type )
        {
            var baseType = type.BaseType;

            if ( baseType == null || baseType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType )
            {
                return;
            }

            var verdict = this._context.GetVerdict( baseType );

            if ( verdict.IsImmutable )
            {
                return;
            }

            // Reported on the type rather than on each inherited member, because the author of the derived type cannot
            // fix a member that the base type declares. The first declaration of the type is used, so that a partial
            // type is reported once.
            var location = GetSourceLocation( type );

            if ( !this.IsInCurrentTree( location ) )
            {
                return;
            }

            this._reportDiagnostic(
                ImmutabilityDiagnosticDescriptors.BaseTypeIsNotImmutable.CreateRoslynDiagnostic(
                    location,
                    (ImmutabilitySymbolFacts.GetDisplayName( type ), ImmutabilitySymbolFacts.GetDisplayName( baseType ),
                     verdict.Prepend( ImmutabilitySymbolFacts.GetDisplayName( type ) ).FormatChain()) ) );
        }

        /// <remarks>
        /// One predicate expresses both stated rules. The backing field of <c>{ get; set; }</c> is not read-only and the
        /// backing field of <c>{ get; init; }</c> is, so <c>IFieldSymbol { IsStatic: false, IsConst: false, IsReadOnly: false }</c>
        /// catches a writeable field and a property with a setter at once.
        /// </remarks>
        private void ValidateInstanceMembers( INamedTypeSymbol type )
        {
            var examinedProperties = new HashSet<ISymbol>( SymbolEqualityComparer.Default );

            foreach ( var member in type.GetMembers() )
            {
                if ( member is not IFieldSymbol { IsStatic: false, IsConst: false } field )
                {
                    continue;
                }

                // An automatically implemented property is analyzed through its backing field, but the diagnostic is
                // reported on the property, which is what the author declared.
                var declaredMember = field.AssociatedSymbol ?? field;

                if ( declaredMember is IPropertySymbol )
                {
                    examinedProperties.Add( declaredMember );
                }

                if ( this.IsAdviceMember( declaredMember ) )
                {
                    continue;
                }

                // A member that is not read-only is reported here only when not every assignment to it can be seen.
                // Otherwise, the assignments are reported instead by the write-site rules.
                if ( !field.IsReadOnly && !CanVerifyWrites( declaredMember ) )
                {
                    this.ReportMember(
                        declaredMember,
                        declaredMember is IPropertySymbol
                            ? ImmutabilityDiagnosticDescriptors.PropertyHasSetter
                            : ImmutabilityDiagnosticDescriptors.FieldIsNotReadOnly,
                        type );

                    continue;
                }

                this.ValidateMemberType( type, declaredMember, field.Type );
            }

            // Roslyn exposes the backing field of an automatically implemented property of a source type, so the loop
            // above covers them. This one is a safety net for the shapes where it does not.
            foreach ( var member in type.GetMembers() )
            {
                if ( member is not IPropertySymbol { IsStatic: false, IsAbstract: false, IsExtern: false } property
                     || examinedProperties.Contains( property )
                     || property.IsAutoProperty() != true
                     || this.IsAdviceMember( property ) )
                {
                    continue;
                }

                if ( property.SetMethod is { IsInitOnly: false } && !CanVerifyWrites( property ) )
                {
                    this.ReportMember( property, ImmutabilityDiagnosticDescriptors.PropertyHasSetter, type );

                    continue;
                }

                this.ValidateMemberType( type, property, property.Type );
            }
        }

        private void ReportMember(
            ISymbol declaredMember,
            Framework.Diagnostics.DiagnosticDefinition<(string Type, string Member)> definition,
            INamedTypeSymbol type )
        {
            var location = GetSourceLocation( declaredMember );

            if ( this.IsInCurrentTree( location ) )
            {
                this._reportDiagnostic( definition.CreateRoslynDiagnostic( location, (ImmutabilitySymbolFacts.GetDisplayName( type ), declaredMember.Name) ) );
            }
        }

        private void ValidateMemberType( INamedTypeSymbol type, ISymbol declaredMember, ITypeSymbol memberType )
        {
            var verdict = this._context.GetVerdict( memberType );

            if ( verdict.IsImmutable || IsArrayOfAttributeClass( type, memberType ) )
            {
                return;
            }

            var location = GetSourceLocation( declaredMember );

            if ( !this.IsInCurrentTree( location ) )
            {
                return;
            }

            var definition = verdict.Kind == ImmutabilityKind.NotAnnotated
                ? ImmutabilityDiagnosticDescriptors.InterfaceIsNotImmutable
                : ImmutabilityDiagnosticDescriptors.MemberIsNotImmutable;

            var chain = verdict
                .Prepend( declaredMember.Name )
                .Prepend( ImmutabilitySymbolFacts.GetDisplayName( type ) )
                .FormatChain();

            this._reportDiagnostic(
                definition.CreateRoslynDiagnostic(
                    location,
                    (ImmutabilitySymbolFacts.GetDisplayName( type ), ImmutabilitySymbolFacts.GetDisplayName( memberType ), chain) ) );
        }

        /// <summary>
        /// Determines whether a member is an array declared by an attribute class.
        /// </summary>
        /// <remarks>
        /// The language allows an attribute argument to be a primitive, a string, a <c>Type</c>, an enumeration,
        /// <c>object</c>, or a single-dimensional array of those. A collection-valued property of an attribute therefore
        /// has to be an array, and <c>ImmutableArray</c> does not compile in that position. The author cannot act on the
        /// report, so it is not made.
        /// </remarks>
        private static bool IsArrayOfAttributeClass( INamedTypeSymbol declaringType, ITypeSymbol memberType )
        {
            if ( memberType is not IArrayTypeSymbol )
            {
                return false;
            }

            for ( var baseType = declaringType.BaseType; baseType != null; baseType = baseType.BaseType )
            {
                if ( ImmutabilitySymbolFacts.GetFullMetadataName( baseType ) == "System.Attribute" )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether every assignment to a member can be seen, which is the case exactly when the member can only
        /// be written from the type that declares it.
        /// </summary>
        private static bool CanVerifyWrites( ISymbol declaredMember )
            => declaredMember is IFieldSymbol or IPropertySymbol
               && !ImmutabilitySymbolFacts.IsWritableFromOutsideDeclaringType( declaredMember );

        /// <summary>
        /// Reports a name declared in one of the MSBuild items that matches no type, so that a typo does not silently
        /// produce a rule that never applies.
        /// </summary>
        /// <remarks>
        /// Only the names that a project declares are checked. The built-in contract names refer to types of
        /// Metalama.Premium, which most compilations do not reference.
        /// </remarks>
        public static void ReportUnknownDeclaredTypeNames( Compilation compilation, IProjectOptions? projectOptions, Action<Diagnostic> reportDiagnostic )
        {
            var context = ImmutabilityContext.Create( projectOptions );

            if ( context == null )
            {
                return;
            }

            foreach ( var name in context.ImmutableTypeNames )
            {
                ReportIfUnknown( name, "MetalamaImmutableType" );
            }

            foreach ( var name in context.ContractTypeNames )
            {
                ReportIfUnknown( name, "MetalamaImmutableContractType" );
            }

            void ReportIfUnknown( string name, string itemName )
            {
                // GetTypeByMetadataName returns null when several referenced assemblies define the name, so it cannot
                // tell an ambiguous name from an unknown one.
                if ( compilation.GetTypesByMetadataName( name ).IsEmpty )
                {
                    reportDiagnostic( ImmutabilityDiagnosticDescriptors.UnknownDeclaredTypeName.CreateRoslynDiagnostic( Location.None, (name, itemName) ) );
                }
            }
        }
    }
}
