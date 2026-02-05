// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Templating
{
    internal static class TemplateNameHelper
    {
        public static string GetCompiledTemplateName( ISymbol symbol )
            => symbol.Kind switch
            {
                SymbolKind.Method when symbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet } method => GetCompiledTemplateName(
                    $"Get{method.AssociatedSymbol!.Name}",
                    method,
                    method.Parameters ),
                SymbolKind.Method when symbol is IMethodSymbol { MethodKind: MethodKind.PropertySet } method => GetCompiledTemplateName(
                    $"Set{method.AssociatedSymbol!.Name}",
                    method,
                    method.Parameters,
                    true ),
                SymbolKind.Method when symbol is IMethodSymbol { MethodKind: MethodKind.EventAdd } method => GetCompiledTemplateName(
                    $"Add{method.AssociatedSymbol!.Name}",
                    method,
                    method.Parameters,
                    true ),
                SymbolKind.Method when symbol is IMethodSymbol { MethodKind: MethodKind.EventRemove } method => GetCompiledTemplateName(
                    $"Remove{method.AssociatedSymbol!.Name}",
                    method,
                    method.Parameters,
                    true ),
                SymbolKind.Method when symbol is IMethodSymbol method => GetCompiledTemplateName( method.Name, method.OriginalDefinition, method.Parameters ),

                // Initializer templates.
                SymbolKind.Field when symbol is IFieldSymbol field => GetCompiledTemplateName( field.Name, field ),
                SymbolKind.Property when symbol is IPropertySymbol property => GetCompiledTemplateName( property.Name, property, property.Parameters ),
                SymbolKind.Event when symbol is IEventSymbol @event => GetCompiledTemplateName( @event.Name, @event ),
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
            using var hashHandle = HashUtilities.AllocateHasher();
            var hashCode = hashHandle.Value;
            hashCode.Append( symbol.GetDocumentationCommentId().AssertNotNull() );

            return $"{principal}_{hashCode.GetCurrentHashAsUInt64():x}";
        }
    }
}