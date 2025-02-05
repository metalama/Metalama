// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.DiagnosticAnalysis;

namespace Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;

#pragma warning disable RS1001
#pragma warning disable RS1022 // Change diagnostic analyzer type to remove all direct or indirect accesses to type 'UserProcessTransformationPreviewService', which accesses types 'Microsoft.CodeAnalysis.Document, Microsoft.CodeAnalysis.Project'

[UsedImplicitly]
public class VsUserProcessDiagnosticAnalyzer : DefinitionOnlyDiagnosticAnalyzer
{
    // This class exists only because the this.SupportedDiagnostics member is called.
    // It is required for code fixes. If this implementation does not run in devenv, the CodeFixProvider is not called.
}