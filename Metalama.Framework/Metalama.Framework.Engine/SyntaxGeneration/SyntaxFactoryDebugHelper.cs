// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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