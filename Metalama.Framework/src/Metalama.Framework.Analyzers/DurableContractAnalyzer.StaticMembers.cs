// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// The rule that reports a static field or a static automatically implemented property of compile-time code whose
    /// declared type is not durable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A static field of a type that is loaded in the compiler process keeps its value for as long as the assembly is
    /// loaded. At design time, this is longer than the life of any compilation. The value of the field must therefore
    /// be durable, whether or not the type is marked <c>[Durable]</c>.
    /// </para>
    /// <para>
    /// The immutability of the same members is verified by <c>ImmutabilityValidator</c> in
    /// <c>Metalama.Framework.Engine</c> (LAMA0889, LAMA0890 and LAMA0891).
    /// </para>
    /// </remarks>
    public partial class DurableContractAnalyzer
    {
        internal static readonly DiagnosticDescriptor StaticMemberIsNotDurable = new(
            "LAMA0892",
            "A static field or automatic property of compile-time code is not durable",
            "'{0}' is loaded in the compiler process, but its static member '{1}' is of type '{2}', which is not durable. "
            + "A static member outlives every compilation, so it must not reference a compilation, a syntax tree, a symbol "
            + "or a code model declaration. Retention path: {3}.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        private const string _adviceAttributeMetadataName = "Metalama.Framework.Advising.IAdviceAttribute";
        private const string _templateAttributeMetadataName = "Metalama.Framework.Aspects.TemplateAttribute";

        /// <summary>
        /// Reports the static fields and the static automatically implemented properties of a compile-time or
        /// run-time-or-compile-time type whose declared type is not durable.
        /// </summary>
        /// <remarks>
        /// In a run-time-or-compile-time type, a member that exists only at run time is not reported, because it is
        /// removed from the compile-time assembly. Such a member is marked <c>[RunTime]</c>, or its declared type is a
        /// run-time type of the current project.
        /// </remarks>
        private static void AnalyzeStaticMembers( SymbolAnalysisContext context, DurabilityContext durabilityContext, INamedTypeSymbol type )
        {
            var typeScope = ExecutionScopeFacts.GetScope( type );

            if ( typeScope is not (ExecutionScope.CompileTime or ExecutionScope.RunTimeOrCompileTime) )
            {
                return;
            }

            var isRunTimeOrCompileTime = typeScope == ExecutionScope.RunTimeOrCompileTime;
            var analyzedMembers = new HashSet<ISymbol>( SymbolEqualityComparer.Default );

            foreach ( var member in type.GetMembers() )
            {
                ISymbol declaredMember;
                ITypeSymbol memberType;

                switch ( member )
                {
                    // The backing field of an automatically implemented property or of a field-like event is examined
                    // as the property or the event, which is what the author declared.
                    case IFieldSymbol { IsStatic: true, IsConst: false } field:
                        declaredMember = field.AssociatedSymbol ?? field;
                        memberType = field.Type;

                        break;

                    // A safety net for an automatically implemented property whose backing field Roslyn does not
                    // expose.
                    case IPropertySymbol { IsStatic: true, IsAbstract: false, IsExtern: false } property
                        when SymbolFacts.IsAutomaticallyImplemented( property ):
                        declaredMember = property;
                        memberType = property.Type;

                        break;

                    default:
                        continue;
                }

                if ( !analyzedMembers.Add( declaredMember ) )
                {
                    continue;
                }

                if ( isRunTimeOrCompileTime && IsRunTimeMember( context.Compilation, declaredMember, memberType ) )
                {
                    continue;
                }

                if ( IsAdviceMember( declaredMember ) )
                {
                    continue;
                }

                AnalyzeStaticMember( context, durabilityContext, type, declaredMember, memberType );
            }
        }

        private static void AnalyzeStaticMember(
            SymbolAnalysisContext context,
            DurabilityContext durabilityContext,
            INamedTypeSymbol type,
            ISymbol declaredMember,
            ITypeSymbol memberType )
        {
            // The attribute on the member waives the check on the declared type and requires instead that every value
            // assigned to it be durable, which the rules on assignments verify.
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

            var chain = verdict
                .Prepend( declaredMember.Name )
                .Prepend( DurabilityContext.GetDisplayName( type ) )
                .FormatChain();

            context.ReportDiagnostic(
                Diagnostic.Create(
                    StaticMemberIsNotDurable,
                    location,
                    DurabilityContext.GetDisplayName( type ),
                    declaredMember.Name,
                    DurabilityContext.GetDisplayName( memberType ),
                    chain ) );
        }

        /// <summary>
        /// Determines whether a member of a run-time-or-compile-time type exists only at run time.
        /// </summary>
        private static bool IsRunTimeMember( Compilation compilation, ISymbol member, ITypeSymbol memberType )
            => ExecutionScopeFacts.GetScopeFromAttributes( member ) == ExecutionScope.RunTime
               || ReferencesRunTimeType( compilation, memberType, 0 );

        /// <summary>
        /// Determines whether a type is or contains a type of the current project that is not compile-time code.
        /// </summary>
        /// <remarks>
        /// Only the types of the current project are considered, because a type of a referenced assembly that has no
        /// scope attribute, for instance <c>List&lt;T&gt;</c>, is available at compile time.
        /// </remarks>
        private static bool ReferencesRunTimeType( Compilation compilation, ITypeSymbol type, int depth )
        {
            if ( depth > 8 )
            {
                return false;
            }

            switch ( type )
            {
                case IArrayTypeSymbol arrayType:
                    return ReferencesRunTimeType( compilation, arrayType.ElementType, depth + 1 );

                case INamedTypeSymbol namedType:
                    if ( SymbolEqualityComparer.Default.Equals( namedType.ContainingAssembly, compilation.Assembly )
                         && ExecutionScopeFacts.GetScope( namedType ) is not (ExecutionScope.CompileTime or ExecutionScope.RunTimeOrCompileTime) )
                    {
                        return true;
                    }

                    return namedType.TypeArguments.Any( t => ReferencesRunTimeType( compilation, t, depth + 1 ) );

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether a member is a template or an introduced member, which the aspect framework handles as
        /// run-time code.
        /// </summary>
        private static bool IsAdviceMember( ISymbol member )
        {
            foreach ( var attribute in member.GetAttributes() )
            {
                for ( var attributeClass = attribute.AttributeClass; attributeClass != null; attributeClass = attributeClass.BaseType )
                {
                    if ( SymbolFacts.GetFullMetadataName( attributeClass ) == _templateAttributeMetadataName )
                    {
                        return true;
                    }
                }

                if ( attribute.AttributeClass != null
                     && attribute.AttributeClass.AllInterfaces.Any( i => SymbolFacts.GetFullMetadataName( i ) == _adviceAttributeMetadataName ) )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
