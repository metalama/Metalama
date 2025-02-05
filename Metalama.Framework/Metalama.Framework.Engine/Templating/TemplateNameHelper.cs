// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using K4os.Hash.xxHash;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Templating
{
    internal static class TemplateNameHelper
    {
        public static string GetCompiledTemplateName( ISymbol symbol )
            => symbol switch
            {
                IMethodSymbol { MethodKind: MethodKind.PropertyGet } method => GetCompiledTemplateName(
                    $"Get{method.AssociatedSymbol!.Name}",
                    method,
                    method.Parameters ),
                IMethodSymbol { MethodKind: MethodKind.PropertySet } method => GetCompiledTemplateName(
                    $"Set{method.AssociatedSymbol!.Name}",
                    method,
                    method.Parameters,
                    true ),
                IMethodSymbol { MethodKind: MethodKind.EventAdd } method => GetCompiledTemplateName(
                    $"Add{method.AssociatedSymbol!.Name}",
                    method,
                    method.Parameters,
                    true ),
                IMethodSymbol { MethodKind: MethodKind.EventRemove } method => GetCompiledTemplateName(
                    $"Remove{method.AssociatedSymbol!.Name}",
                    method,
                    method.Parameters,
                    true ),
                IMethodSymbol method => GetCompiledTemplateName( method.Name, method.OriginalDefinition, method.Parameters ),

                // Initializer templates.
                IFieldSymbol field => GetCompiledTemplateName( field.Name, field ),
                IPropertySymbol property => GetCompiledTemplateName( property.Name, property, property.Parameters ),
                IEventSymbol @event => GetCompiledTemplateName( @event.Name, @event ),
                _ => throw new AssertionFailedException( $"Unexpected symbol: '{symbol}'." )
            };

        private static string GetCompiledTemplateName(
            string templateMemberName,
            ISymbol symbol,
            ImmutableArray<IParameterSymbol> parameters = default,
            bool ignoredLastParameter = false )
        {
            symbol = symbol.GetOverriddenMember() ?? symbol;

            var principal = "__" + templateMemberName;

            if ( parameters.IsDefaultOrEmpty || (ignoredLastParameter && parameters.Length == 1) )
            {
                return principal;
            }

            // If we have parameters, we need to add a unique hash of the symbol to differentiate symbols
            // of the same name. It is essential that this hash is consistent across runtimes and versions of Roslyn and Metalama.
            var hashCode = new XXH64();
            hashCode.Update( symbol.GetDocumentationCommentId().AssertNotNull() );

            return $"{principal}_{hashCode.Digest():x}";
        }
    }
}