// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.CodeFixes;
using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.DiagnosticSuppressing;
using Metalama.Framework.DesignTime.Rider;
using Metalama.Framework.DesignTime.SourceGeneration;
using Metalama.Framework.DesignTime.VisualStudio.CodeFixes;
using Metalama.Framework.DesignTime.VisualStudio.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.VisualStudio.DiagnosticSuppressing;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

/// <summary>
/// Tests that the entry point type names of <see cref="RoslynEntryPointTypeNames"/> (which must be constants and are used by the
/// facade types in CompilerExtensions and EditorExtensions) are correct.
/// </summary>
public sealed class TestRoslynEntryPointTypeNames
{
    [Theory]
    [InlineData( RoslynEntryPointTypeNames.TheDiagnosticAnalyzer, typeof(TheDiagnosticAnalyzer) )]
    [InlineData( RoslynEntryPointTypeNames.TheDiagnosticSuppressor, typeof(TheDiagnosticSuppressor) )]
    [InlineData( RoslynEntryPointTypeNames.VsAnalysisProcessDiagnosticAnalyzer, typeof(VsAnalysisProcessDiagnosticAnalyzer) )]
    [InlineData( RoslynEntryPointTypeNames.VsDiagnosticSuppressor, typeof(VsDiagnosticSuppressor) )]
    [InlineData( RoslynEntryPointTypeNames.VsAnalysisProcessSourceGenerator, typeof(VsAnalysisProcessSourceGenerator) )]
    [InlineData( RoslynEntryPointTypeNames.VsUserProcessDiagnosticAnalyzer, typeof(VsUserProcessDiagnosticAnalyzer) )]
    [InlineData( RoslynEntryPointTypeNames.VsUserProcessSourceGenerator, typeof(VsUserProcessSourceGenerator) )]
    [InlineData( RoslynEntryPointTypeNames.AnalysisProcessSourceGenerator, typeof(AnalysisProcessSourceGenerator) )]
    [InlineData( RoslynEntryPointTypeNames.VsCodeFixProvider, typeof(VsCodeFixProvider) )]
    [InlineData( RoslynEntryPointTypeNames.RiderCodeFixProvider, typeof(RiderCodeFixProvider) )]
    [InlineData( RoslynEntryPointTypeNames.TheCodeFixProvider, typeof(TheCodeFixProvider) )]
    [InlineData( RoslynEntryPointTypeNames.VsCodeRefactoringProvider, typeof(VsCodeRefactoringProvider) )]
    [InlineData( RoslynEntryPointTypeNames.RiderCodeRefactoringProvider, typeof(RiderCodeRefactoringProvider) )]
    [InlineData( RoslynEntryPointTypeNames.TheCodeRefactoringProvider, typeof(TheCodeRefactoringProvider) )]
    public void TestConstant( string constantValue, Type type )
    {
        Assert.Equal( type.FullName, constantValue );
    }

    /// <summary>
    /// Tests that every type name constant of <see cref="RoslynEntryPointTypeNames"/> is covered by a row of <see cref="TestConstant"/>,
    /// so that a new entry point cannot be added without being compared with the full name of the real type.
    /// </summary>
    [Fact]
    public void TestAllConstantsAreCovered()
    {
        var constants = typeof(RoslynEntryPointTypeNames)
            .GetFields( BindingFlags.Public | BindingFlags.Static )
            .Where( f => f.IsLiteral && f.FieldType == typeof(string) )
            .Where( f => f.Name != nameof(RoslynEntryPointTypeNames.DesignTimeAssemblyName) )
            .Select( f => (string) f.GetRawConstantValue()! )
            .OrderBy( n => n, StringComparer.Ordinal )
            .ToArray();

        var testMethod = typeof(TestRoslynEntryPointTypeNames).GetMethod( nameof(this.TestConstant) )!;

        var testedConstants = testMethod.GetCustomAttributes<InlineDataAttribute>()
            .Select( a => (string) a.GetData( testMethod ).Single()[0]! )
            .OrderBy( n => n, StringComparer.Ordinal )
            .ToArray();

        Assert.Equal( constants, testedConstants );
    }
}
