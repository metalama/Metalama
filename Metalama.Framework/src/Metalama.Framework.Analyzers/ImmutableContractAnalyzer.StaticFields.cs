// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Analyzers.Immutability
{
    public partial class ImmutableContractAnalyzer
    {
        private const string _staticBecause =
            "A static field of a compile-time type is shared by every aspect instance, every target and every compilation, "
            + "and at design time it keeps its value for as long as the analysis process runs.";

        internal static readonly DiagnosticDescriptor StaticFieldIsNotReadOnly = new(
            "LAMA0889",
            "A static field or automatic property of a compile-time type is not read-only",
            "'{0}' is a compile-time type, but the static member '{1}' is not read-only. " + _staticBecause
            + " Add the 'readonly' modifier to the field, or remove the setter of the property.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        internal static readonly DiagnosticDescriptor StaticFieldIsNotImmutable = new(
            "LAMA0890",
            "A static field or automatic property of a compile-time type is of a mutable type",
            "'{0}' is a compile-time type, but the static member '{1}' is of type '{2}', which is not immutable. "
            + _staticBecause + " Path: {3}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        /// <remarks>
        /// One descriptor covers both causes, a member that is not read-only and a member of a mutable type, because
        /// the remedy is the same: the <c>[RunTime]</c> attribute removes the member from the compile-time assembly, so
        /// that the state exists only at run time. The remedy is not offered for a compile-time type, where the engine
        /// rejects a run-time member.
        /// </remarks>
        internal static readonly DiagnosticDescriptor StaticFieldMustBeRunTime = new(
            "LAMA0891",
            "A mutable static field or automatic property of a run-time-or-compile-time type is not marked [RunTime]",
            "'{0}' is a run-time-or-compile-time type, but the static member '{1}' {2}. " + _staticBecause
            + " Mark the member with [RunTime] so that it exists only at run time, or make it read-only and of an immutable type.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        /// <summary>
        /// Reports the static fields and static automatic properties of a compile-time or run-time-or-compile-time type
        /// that are not read-only or are of a mutable type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This rule does not depend on the <c>[ImmutableType]</c> contract. It applies to every type that is loaded in
        /// the compiler process, because a static field of such a type is retained for as long as its assembly is
        /// loaded, whatever the role of the type.
        /// </para>
        /// <para>
        /// A static member that is not read-only is reported at its declaration, whatever its accessibility. The
        /// write-site rules, which report the assignments of a private instance member instead of its declaration, do
        /// not apply to static members: a static constructor is not a point after which the value is fixed for one
        /// aspect instance.
        /// </para>
        /// </remarks>
        private static void AnalyzeStaticMembers( SymbolAnalysisContext context, ImmutabilityContext immutabilityContext, INamedTypeSymbol type )
        {
            var scope = immutabilityContext.GetExecutionScope( type );

            if ( scope == ExecutionScope.RunTime )
            {
                return;
            }

            var examinedProperties = new HashSet<ISymbol>( SymbolEqualityComparer.Default );

            foreach ( var member in type.GetMembers() )
            {
                if ( member is not IFieldSymbol { IsStatic: true, IsConst: false } field )
                {
                    continue;
                }

                var declaredMember = field.AssociatedSymbol ?? field;

                if ( field.IsImplicitlyDeclared && field.AssociatedSymbol == null )
                {
                    continue;
                }

                if ( declaredMember is IPropertySymbol )
                {
                    examinedProperties.Add( declaredMember );
                }

                AnalyzeStaticMember( context, immutabilityContext, type, scope, declaredMember, field.IsReadOnly, field.Type );
            }

            // The same safety net as for instance members, for the shapes where Roslyn does not expose the backing field.
            foreach ( var member in type.GetMembers() )
            {
                if ( member is not IPropertySymbol { IsStatic: true, IsAbstract: false, IsExtern: false } property
                     || examinedProperties.Contains( property )
                     || !SymbolFacts.IsAutomaticallyImplemented( property ) )
                {
                    continue;
                }

                AnalyzeStaticMember( context, immutabilityContext, type, scope, property, property.SetMethod == null, property.Type );
            }
        }

        private static void AnalyzeStaticMember(
            SymbolAnalysisContext context,
            ImmutabilityContext immutabilityContext,
            INamedTypeSymbol type,
            ExecutionScope scope,
            ISymbol declaredMember,
            bool isReadOnly,
            ITypeSymbol memberType )
        {
            if ( IsAdviceMember( declaredMember ) )
            {
                return;
            }

            if ( scope == ExecutionScope.RunTimeOrCompileTime && HasRunTimeAttribute( declaredMember ) )
            {
                return;
            }

            var location = declaredMember.Locations.FirstOrDefault( l => l.IsInSource );

            if ( location == null )
            {
                return;
            }

            var typeName = SymbolFacts.GetDisplayName( type );

            // One diagnostic per member: a member that is not read-only must be made read-only before the question of
            // its type is worth asking.
            if ( !isReadOnly )
            {
                context.ReportDiagnostic(
                    scope == ExecutionScope.CompileTime
                        ? Diagnostic.Create( StaticFieldIsNotReadOnly, location, typeName, declaredMember.Name )
                        : Diagnostic.Create( StaticFieldMustBeRunTime, location, typeName, declaredMember.Name, "is not read-only" ) );

                return;
            }

            var verdict = immutabilityContext.GetVerdict( memberType );

            if ( verdict.IsImmutable )
            {
                return;
            }

            var memberTypeName = SymbolFacts.GetDisplayName( memberType );
            var chain = verdict.Prepend( declaredMember.Name ).Prepend( typeName ).FormatChain();

            context.ReportDiagnostic(
                scope == ExecutionScope.CompileTime
                    ? Diagnostic.Create( StaticFieldIsNotImmutable, location, typeName, declaredMember.Name, memberTypeName, chain )
                    : Diagnostic.Create(
                        StaticFieldMustBeRunTime,
                        location,
                        typeName,
                        declaredMember.Name,
                        $"is of type '{memberTypeName}', which is not immutable (path: {chain})" ) );
        }

        /// <summary>
        /// Determines whether a member carries the <c>[RunTime]</c> attribute, which removes it from the compile-time
        /// assembly.
        /// </summary>
        private static bool HasRunTimeAttribute( ISymbol member )
        {
            foreach ( var attribute in member.GetAttributes() )
            {
                if ( attribute.AttributeClass is { Name: "RunTimeAttribute" } attributeClass
                     && SymbolFacts.GetFullMetadataName( attributeClass ) == "Metalama.Framework.Aspects.RunTimeAttribute" )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
