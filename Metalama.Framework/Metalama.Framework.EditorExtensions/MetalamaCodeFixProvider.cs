// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace Metalama.Framework.CompilerExtensions
{
    // ReSharper disable UnusedType.Global

    [ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof(MetalamaCodeFixProvider) )]
    [Shared]
    public class MetalamaCodeFixProvider : CodeFixProvider
    {
        private readonly CodeFixProvider? _impl;

        public MetalamaCodeFixProvider()
        {
            switch ( ProcessKindHelper.CurrentProcessKind )
            {
                case ProcessKind.Compiler:
                    break;

                case ProcessKind.RoslynCodeAnalysisService:
                case ProcessKind.DevEnv:
                    this._impl = (CodeFixProvider) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.VsCodeFixProvider );

                    break;

                case ProcessKind.Rider:
                    this._impl = (CodeFixProvider) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.RiderCodeFixProvider );

                    break;

                default:
                    this._impl = (CodeFixProvider) ResourceExtractor.CreateInstance(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.TheCodeFixProvider );

                    break;
            }
        }

#pragma warning disable VSTHRD110
        public override Task RegisterCodeFixesAsync( CodeFixContext context ) => this._impl?.RegisterCodeFixesAsync( context ) ?? Task.CompletedTask;
#pragma warning restore VSTHRD110

        public override ImmutableArray<string> FixableDiagnosticIds => this._impl?.FixableDiagnosticIds ?? ImmutableArray<string>.Empty;

        public override FixAllProvider? GetFixAllProvider() => this._impl?.GetFixAllProvider();
    }
}