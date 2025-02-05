// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Metalama.Testing.AspectTesting;

internal static class SyntaxTreeStructureVerifier
{
    /// <summary>
    /// Checks for "hidden" problems in a <see cref="SyntaxTree"/>, i.e. problems where the _text_
    /// of the source code is valid, but the semantic syntax tree is not.
    /// </summary>
    [UsedImplicitly]
    public static bool VerifyMetaSyntax( Compilation compilation, in ProjectServiceProvider serviceProvider )
    {
        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            var actualSyntaxFactory = syntaxTree.GetRoot().ToSyntaxFactoryDebug( compilation );

            var parsedFromText = CSharpSyntaxTree.ParseText(
                    syntaxTree.GetRoot().ToString(),
                    encoding: Encoding.UTF8,
                    options: SupportedCSharpVersions.DefaultParseOptions )
                .GetRoot()
                .ToSyntaxFactoryDebug( compilation );

            if ( parsedFromText != actualSyntaxFactory )
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks for "hidden" problems in a <see cref="SyntaxTree"/>, i.e. problems where the _text_
    /// of the source code is valid, but the semantic syntax tree is not.
    /// </summary>
    public static bool Verify( Compilation compilation, [NotNullWhen( false )] out DiagnosticBag? diagnostics )
    {
        diagnostics = null;

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            var parsedFromText = CSharpSyntaxTree.ParseText(
                    syntaxTree.GetRoot().ToString(),
                    path: syntaxTree.FilePath,
                    encoding: Encoding.UTF8,
                    options: (CSharpParseOptions) syntaxTree.Options )
                .GetRoot();

            foreach ( var diagnostic in parsedFromText.GetDiagnostics() )
            {
                if ( diagnostic.Severity == DiagnosticSeverity.Error )
                {
                    (diagnostics ??= new DiagnosticBag()).Report( diagnostic );
                }
            }
        }

        if ( diagnostics == null )
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}