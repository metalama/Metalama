// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime
{
    /// <summary>
    /// Lists the name of the design-time assembly and the names of the public entry point types that this assembly exposes to Roslyn.
    /// The *.CompilerExtensions and *.EditorExtensions projects must name every entry point that they load by a constant of this class,
    /// and never by a string literal.
    /// The TestRoslynEntryPointTypeNames unit test compares each type name constant with the full name of the real type, and verifies
    /// that every type name constant is covered by a test case.
    /// </summary>
    [UsedImplicitly( ImplicitUseTargetFlags.Members )]
    internal static class RoslynEntryPointTypeNames
    {
        public const string DesignTimeAssemblyName = "Metalama.Framework.DesignTime";

        public const string VsUserProcessSourceGenerator = "Metalama.Framework.DesignTime.VisualStudio.SourceGenerating.VsUserProcessSourceGenerator";
        public const string VsAnalysisProcessSourceGenerator = "Metalama.Framework.DesignTime.VisualStudio.SourceGenerating.VsAnalysisProcessSourceGenerator";
        public const string AnalysisProcessSourceGenerator = "Metalama.Framework.DesignTime.SourceGeneration.AnalysisProcessSourceGenerator";
        public const string VsUserProcessDiagnosticAnalyzer = "Metalama.Framework.DesignTime.VisualStudio.SourceGenerating.VsUserProcessDiagnosticAnalyzer";

        public const string VsAnalysisProcessDiagnosticAnalyzer =
            "Metalama.Framework.DesignTime.VisualStudio.DiagnosticAnalysis.VsAnalysisProcessDiagnosticAnalyzer";

        public const string TheDiagnosticAnalyzer = "Metalama.Framework.DesignTime.DiagnosticAnalysis.TheDiagnosticAnalyzer";
        public const string VsDiagnosticSuppressor = "Metalama.Framework.DesignTime.VisualStudio.DiagnosticSuppressing.VsDiagnosticSuppressor";
        public const string TheDiagnosticSuppressor = "Metalama.Framework.DesignTime.DiagnosticSuppressing.TheDiagnosticSuppressor";
        public const string VsCodeFixProvider = "Metalama.Framework.DesignTime.VisualStudio.CodeFixes.VsCodeFixProvider";
        public const string RiderCodeFixProvider = "Metalama.Framework.DesignTime.Rider.RiderCodeFixProvider";
        public const string TheCodeFixProvider = "Metalama.Framework.DesignTime.CodeFixes.TheCodeFixProvider";
        public const string VsCodeRefactoringProvider = "Metalama.Framework.DesignTime.VisualStudio.CodeFixes.VsCodeRefactoringProvider";
        public const string RiderCodeRefactoringProvider = "Metalama.Framework.DesignTime.Rider.RiderCodeRefactoringProvider";
        public const string TheCodeRefactoringProvider = "Metalama.Framework.DesignTime.CodeFixes.TheCodeRefactoringProvider";
    }
}