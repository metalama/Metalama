// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating.Immutability
{
    internal sealed partial class ImmutabilityValidator
    {
        /// <summary>
        /// Reports an <c>[ImmutableType]</c> attribute that states a contract by which the type is bound anyway.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The contract propagates to every type that derives from or implements a type that requires it. A type that
        /// both inherits the obligation and restates it is checked identically with the attribute deleted, so the
        /// attribute suggests to a reader that removing it would relax something, when nothing would change.
        /// </para>
        /// <para>
        /// The type named in the message is not always a marked type. A type of the built-in table of contract types,
        /// and a type named by the <c>MetalamaImmutableContractType</c> item, bind their implementations without
        /// carrying the attribute.
        /// </para>
        /// </remarks>
        private void ValidateRedundantAttribute( INamedTypeSymbol type )
        {
            if ( !ImmutabilityContext.TryGetImmutableTypeAttribute( type, out var attribute ) )
            {
                return;
            }

            var source = this._context.GetInheritedContractSource( type );

            if ( source == null )
            {
                return;
            }

            var location = ImmutabilitySymbolFacts.GetApplicationLocation( attribute );

            if ( !this.IsInCurrentTree( location ) )
            {
                return;
            }

            this._reportDiagnostic(
                ImmutabilityDiagnosticDescriptors.RedundantImmutableTypeAttribute.CreateRoslynDiagnostic(
                    location,
                    (ImmutabilitySymbolFacts.GetDisplayName( type ), ImmutabilitySymbolFacts.GetDisplayName( source )) ) );
        }
    }
}
