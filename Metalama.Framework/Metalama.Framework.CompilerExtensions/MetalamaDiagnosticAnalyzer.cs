// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

#pragma warning disable RS1026 // Enable concurrent execution
#pragma warning disable RS1025 // Configure generated code analysis

namespace Metalama.Framework.CompilerExtensions
{
    // ReSharper disable UnusedType.Global

    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public class MetalamaDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private readonly DiagnosticAnalyzer? _impl;

        public MetalamaDiagnosticAnalyzer()
        {
            switch ( ProcessKindHelper.CurrentProcessKind )
            {
                case ProcessKind.Compiler:
                    //The service is not required.
                    break;

                case ProcessKind.DevEnv:
                    this._impl = (DiagnosticAnalyzer) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.VsUserProcessDiagnosticAnalyzer );

                    break;

                case ProcessKind.RoslynCodeAnalysisService:
                    this._impl = (DiagnosticAnalyzer) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.VsAnalysisProcessDiagnosticAnalyzer );

                    break;

                default:
                    this._impl = (DiagnosticAnalyzer) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.TheDiagnosticAnalyzer );

                    break;
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => this._impl?.SupportedDiagnostics ?? ImmutableArray<DiagnosticDescriptor>.Empty;

        public override void Initialize( AnalysisContext context ) => this._impl?.Initialize( context );
    }
}