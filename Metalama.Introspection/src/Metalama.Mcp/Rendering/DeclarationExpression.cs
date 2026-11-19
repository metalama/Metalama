// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Introspection.Navigation.Links;
using Metalama.Introspection.Navigation.Results;
using System.Text;

namespace Metalama.Mcp.Rendering;

/// <summary>
/// Builds the C# expression that resolves a declaration in a query, such as <c>D&lt;IMethod&gt;("M:MyApp.Customer.Save")</c>.
/// </summary>
internal static class DeclarationExpression
{
    /// <summary>
    /// Gets the expression of a declaration, or <c>null</c> when the object is not a declaration with an identifier of its
    /// own. Types that are not declarations, such as arrays, have a <c>Y:</c> identifier that <c>D&lt;T&gt;</c> does not accept.
    /// </summary>
    public static string? Get( ObjectNode obj, LinkBase? linkBase )
    {
        if ( !obj.IsDeclaration || obj.Link is not { Steps.IsEmpty: true, IsIdentifierAnchor: true } link )
        {
            return null;
        }

        return $"D<{obj.InterfaceName ?? "IDeclaration"}>(\"{link.Anchor}\"{GetScopeArguments( link, linkBase )})";
    }

    /// <summary>
    /// Gets the arguments that <c>D</c> needs when the declaration is not in the default scope of the base: its project and
    /// target framework, and whether it is in the transformed code.
    /// </summary>
    private static string GetScopeArguments( Link link, LinkBase? linkBase )
    {
        var arguments = new StringBuilder();

        if ( linkBase?.Scope != link.Scope && link.Scope != Link.WholeWorkspaceScope )
        {
            var at = link.Scope.LastIndexOf( '@' );

            arguments.Append( at < 0 ? $", project: \"{link.Scope}\"" : $", project: \"{link.Scope.Substring( 0, at )}\", tfm: \"{link.Scope.Substring( at + 1 )}\"" );
        }

        if ( link.IsTransformed )
        {
            arguments.Append( ", transformed: true" );
        }

        return arguments.ToString();
    }
}
