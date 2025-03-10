// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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