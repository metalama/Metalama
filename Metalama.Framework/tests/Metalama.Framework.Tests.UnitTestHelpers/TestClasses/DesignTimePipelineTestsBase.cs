// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit.Abstractions;

#pragma warning disable IDE0079   // Remove unnecessary suppression.
#pragma warning disable CA1307    // Specify StringComparison for clarity
#pragma warning disable VSTHRD200 // Warning VSTHRD200 : Use "Async" suffix in names of methods that return an awaitable type.

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

public abstract class DesignTimePipelineTestsBase : UnitTestClass
{
    protected DesignTimePipelineTestsBase( ITestOutputHelper logger ) : base( logger ) { }

    private static void DumpSyntaxTreeResult( SyntaxTree? syntaxTree, SyntaxTreePipelineResult syntaxTreeResult, StringBuilder stringBuilder )
    {
        string GetTextUnderDiagnostic( Diagnostic diagnostic )
        {
            var syntaxTreeOfDiagnostic = diagnostic.Location.SourceTree ?? syntaxTree;

            return syntaxTreeOfDiagnostic?.GetText().GetSubText( diagnostic.Location.SourceSpan ).ToString() ?? "";
        }

        stringBuilder.AppendLine( syntaxTreeResult.SyntaxTreePath + ":" );

        // Diagnostics
        stringBuilder.AppendLineInvariant( $"{syntaxTreeResult.Diagnostics.Length} diagnostic(s):" );

        foreach ( var diagnostic in syntaxTreeResult.Diagnostics )
        {
            stringBuilder.AppendLineInvariant(
                $"   {diagnostic.Severity} {diagnostic.Id} on `{GetTextUnderDiagnostic( diagnostic )}`: `{diagnostic.GetMessage( CultureInfo.CurrentCulture )}`" );
        }

        // Suppressions
        stringBuilder.AppendLineInvariant( $"{syntaxTreeResult.Suppressions.Length} suppression(s):" );

        foreach ( var suppression in syntaxTreeResult.Suppressions )
        {
            stringBuilder.AppendLineInvariant( $"   {suppression}" );
        }

        // Introductions
        stringBuilder.AppendLineInvariant( $"{syntaxTreeResult.Introductions.Length} introductions(s):" );

        foreach ( var introduction in syntaxTreeResult.Introductions.OrderBy( i => i.Name ) )
        {
            stringBuilder.AppendLine( introduction.GeneratedSyntaxTree.ToString() );
        }
    }

    protected static string DumpResults( DesignTimeAspectPipelineResultAndState results )
    {
        StringBuilder stringBuilder = new();

        var i = 0;

        foreach ( var syntaxTreeResult in results.Result.SyntaxTreeResults.Values.OrderBy( t => t.SyntaxTreePath ) )
        {
            if ( i > 0 )
            {
                stringBuilder.AppendLine( "----------------------------------------------------------" );
            }

            i++;

            var syntaxTree = syntaxTreeResult.SyntaxTreePath != null ? results.ProjectVersion.SyntaxTrees[syntaxTreeResult.SyntaxTreePath].SyntaxTree : null;

            DumpSyntaxTreeResult( syntaxTree, syntaxTreeResult, stringBuilder );
        }

        return stringBuilder.ToString().Trim();
    }
}