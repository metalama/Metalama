// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Framework.DesignTime
{
    /// <summary>
    /// Lists the public entry points types exposed to Roslyn. 
    /// This list is referenced by the *.CompilerExtensions and *.EditorExtensions projects.
    /// It is unit tested.
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
    }
}