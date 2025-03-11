// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.CompilerExtensions
{
    // ReSharper disable UnusedType.Global

    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public class MetalamaDiagnosticSuppressor : DiagnosticSuppressor
    {
        private readonly DiagnosticSuppressor? _impl;

        public MetalamaDiagnosticSuppressor()
        {
            switch ( ProcessKindHelper.CurrentProcessKind )
            {
                case ProcessKind.Compiler:
                    break;

                case ProcessKind.RoslynCodeAnalysisService:
                    this._impl = (DiagnosticSuppressor) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.VsDiagnosticSuppressor );

                    break;

                case ProcessKind.DevEnv:
                    break;

                default:
                    this._impl = (DiagnosticSuppressor) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.TheDiagnosticSuppressor );

                    break;
            }
        }

        public override void ReportSuppressions( SuppressionAnalysisContext context ) => this._impl?.ReportSuppressions( context );

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
            => this._impl?.SupportedSuppressions ?? ImmutableArray<SuppressionDescriptor>.Empty;
    }
}