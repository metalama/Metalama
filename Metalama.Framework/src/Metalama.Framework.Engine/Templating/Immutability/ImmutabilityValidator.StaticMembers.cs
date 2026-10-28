// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers.Durability;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.Templating.Immutability
{
    internal sealed partial class ImmutabilityValidator
    {
        private DurabilityContext? _durabilityContext;

        /// <summary>
        /// Determines whether a type is loaded in the compiler process, that is, whether it is compile-time or
        /// run-time-or-compile-time.
        /// </summary>
        private bool IsCompileTimeType( INamedTypeSymbol type )
            => this._classifier.GetTemplatingScope( type ) is TemplatingScope.CompileTimeOnly or TemplatingScope.RunTimeOrCompileTime;

        /// <summary>
        /// Reports a static field, a static automatic property or a static field-like event of a compile-time or
        /// run-time-or-compile-time type that is not read-only, is of a mutable type, or is of a type that is not durable.
        /// </summary>
        /// <param name="member">The member, declared in the current syntax tree.</param>
        /// <param name="memberScope">The execution scope of the member.</param>
        /// <param name="typeScope">The execution scope of the declaring type.</param>
        /// <remarks>
        /// <para>
        /// This rule does not depend on the <c>[ImmutableType]</c> contract. It applies to every type that is loaded in
        /// the compiler process, because a static field of such a type keeps its value for as long as the assembly is
        /// loaded, whatever the role of the type.
        /// </para>
        /// <para>
        /// In a run-time-or-compile-time type, a member whose own scope is run-time is accepted. Such a member is removed
        /// from the compile-time assembly, whether its scope comes from a <c>[RunTime]</c> attribute or from its run-time
        /// type. In a compile-time type, a run-time member is already an error (LAMA0292).
        /// </para>
        /// <para>
        /// A static member that is not read-only is reported at its declaration, whatever its accessibility, because a
        /// static constructor is not a point after which the value is fixed for one aspect instance.
        /// </para>
        /// </remarks>
        public void ValidateStaticMember( ISymbol member, TemplatingScope memberScope, TemplatingScope typeScope )
        {
            if ( !member.IsStatic )
            {
                return;
            }

            bool isReadOnly;
            ITypeSymbol memberType;

            switch ( member.Kind )
            {
                case SymbolKind.Field when member is IFieldSymbol { IsConst: false } field:
                    isReadOnly = field.IsReadOnly;
                    memberType = field.Type;

                    break;

                case SymbolKind.Property when member is IPropertySymbol property && property.IsAutoProperty() == true:
                    isReadOnly = property.SetMethod == null;
                    memberType = property.Type;

                    break;

                // A field-like event has a backing field that is assigned by every subscription.
                case SymbolKind.Event when member is IEventSymbol { AddMethod.IsImplicitlyDeclared: true } eventSymbol:
                    isReadOnly = false;
                    memberType = eventSymbol.Type;

                    break;

                default:
                    return;
            }

            var isCompileTime = typeScope == TemplatingScope.CompileTimeOnly;

            if ( !isCompileTime && (typeScope != TemplatingScope.RunTimeOrCompileTime || memberScope == TemplatingScope.RunTimeOnly) )
            {
                return;
            }

            if ( this.IsAdviceMember( member ) )
            {
                return;
            }

            var location = member.Locations.FirstOrDefault( l => l.IsInSource );

            if ( !this.IsInCurrentTree( location ) )
            {
                return;
            }

            var typeName = ImmutabilitySymbolFacts.GetDisplayName( member.ContainingType );

            // One diagnostic per member: a member that is not read-only must be made read-only before the question of
            // its type is worth asking.
            if ( !isReadOnly )
            {
                this._reportDiagnostic(
                    isCompileTime
                        ? ImmutabilityDiagnosticDescriptors.StaticFieldIsNotReadOnly.CreateRoslynDiagnostic( location, (typeName, member.Name) )
                        : ImmutabilityDiagnosticDescriptors.StaticFieldMustBeRunTime.CreateRoslynDiagnostic(
                            location,
                            (typeName, member.Name, "is not read-only") ) );

                return;
            }

            var verdict = this._context.GetVerdict( memberType );

            if ( verdict.IsImmutable )
            {
                this.ValidateStaticMemberDurability( member, memberType, location, typeName );

                return;
            }

            var memberTypeName = ImmutabilitySymbolFacts.GetDisplayName( memberType );
            var chain = verdict.Prepend( member.Name ).Prepend( typeName ).FormatChain();

            this._reportDiagnostic(
                isCompileTime
                    ? ImmutabilityDiagnosticDescriptors.StaticFieldIsNotImmutable.CreateRoslynDiagnostic(
                        location,
                        (typeName, member.Name, memberTypeName, chain) )
                    : ImmutabilityDiagnosticDescriptors.StaticFieldMustBeRunTime.CreateRoslynDiagnostic(
                        location,
                        (typeName, member.Name, $"is of type '{memberTypeName}', which is not immutable (path: {chain})") ) );
        }

        /// <summary>
        /// Reports a static member that is read-only and of an immutable type, but whose type is not durable (LAMA0892).
        /// </summary>
        /// <remarks>
        /// <para>
        /// A static member outlives every compilation, so at design time a value that references a compilation, a syntax
        /// tree, a symbol or a code model declaration keeps that compilation alive. Such a value is often immutable, which
        /// is why this rule follows the immutability rules rather than replacing them.
        /// </para>
        /// <para>
        /// The <c>[Durable]</c> attribute on the member waives the check on its declared type. The analyzer of
        /// <c>Metalama.Framework.Analyzers</c> then verifies instead that every value assigned to the member is durable.
        /// </para>
        /// </remarks>
        private void ValidateStaticMemberDurability( ISymbol member, ITypeSymbol memberType, Location? location, string typeName )
        {
            if ( DurabilityContext.HasDurableAttribute( member ) )
            {
                return;
            }

            // The context caches verdicts keyed by symbols, so it lives no longer than this validator, that is, than the
            // validation of one syntax tree. It is created on first use because most trees have no static member to check.
            this._durabilityContext ??= DurabilityContext.Create(
                this._projectOptions?.DurableTypes ?? [],
                this._projectOptions?.NonDurableTypes ?? [] );

            var verdict = this._durabilityContext.GetVerdict( memberType );

            if ( verdict.IsDurable )
            {
                return;
            }

            var chain = verdict.Prepend( member.Name ).Prepend( typeName ).FormatChain();

            this._reportDiagnostic(
                ImmutabilityDiagnosticDescriptors.StaticFieldIsNotDurable.CreateRoslynDiagnostic(
                    location,
                    (typeName, member.Name, DurabilityContext.GetDisplayName( memberType ), chain) ) );
        }
    }
}
