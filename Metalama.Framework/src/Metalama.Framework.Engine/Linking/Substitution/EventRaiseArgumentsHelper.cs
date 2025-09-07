// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Helper class for extracting invoke arguments from event raise substitution patterns.
/// </summary>
internal static class EventRaiseArgumentsHelper
{
    /// <summary>
    /// Extracts invoke arguments from the arguments array in event raise substitution patterns.
    /// </summary>
    /// <param name="arguments">The arguments array from the event raise invocation.</param>
    /// <returns>A separated list of arguments for the invoke call.</returns>
    public static SeparatedSyntaxList<ArgumentSyntax> ExtractInvokeArguments( SeparatedSyntaxList<ArgumentSyntax> arguments )
    {
        return arguments switch
        {
            [_] => SeparatedList<ArgumentSyntax>(),
            [_, { Expression: TupleExpressionSyntax argsTuple }] =>
                argsTuple.Arguments,
            _ => throw new AssertionFailedException( $"Unexpected arguments: {arguments}" )
        };
    }
}