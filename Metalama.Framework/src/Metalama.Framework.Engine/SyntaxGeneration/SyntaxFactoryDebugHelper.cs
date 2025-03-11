// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.SyntaxGeneration;

public static partial class SyntaxFactoryDebugHelper
{
    /// <summary>
    /// Generates a string that contains C# code that instantiates the given node
    /// using SyntaxFactory. Used for debugging.
    /// </summary>
    public static string ToSyntaxFactoryDebug( this SyntaxNode node, Compilation compilation )
    {
        MetaSyntaxRewriter rewriter = new( compilation.GetCompilationContext(), RoslynApiVersion.Current );

        try
        {
            var normalized = new NormalizeRewriter().Visit( node );
            var transformedNode = rewriter.Visit( normalized )!;

            return transformedNode.ToFullString();
        }
        catch ( Exception ex )
        {
            return ex.ToString();
        }
    }
}