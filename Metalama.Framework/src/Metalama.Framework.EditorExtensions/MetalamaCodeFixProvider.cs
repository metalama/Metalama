// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
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

                case ProcessKind.Format:
                    // Not tested.
                    break;

                case ProcessKind.RoslynCodeAnalysisService:
                case ProcessKind.DevEnv:
                    ResourceExtractor.TryCreateInstance<CodeFixProvider>(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.VsCodeFixProvider,
                        out this._impl );

                    break;

                case ProcessKind.Rider:
                    ResourceExtractor.TryCreateInstance<CodeFixProvider>(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.RiderCodeFixProvider,
                        out this._impl );

                    break;

                case ProcessKind.LanguageServer:
                    // The language server of the Visual Studio Code C# Dev Kit. It is named explicitly, and not
                    // left to the default arm, because section 6 of DECISIONS.md names the C# Dev Kit as one of
                    // the hosts on which the design-time result and the build-time result diverge, and this
                    // assembly can already distinguish Rider.
                    ResourceExtractor.TryCreateInstance<CodeFixProvider>(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.TheCodeFixProvider,
                        out this._impl );

                    break;

                default:
                    ResourceExtractor.TryCreateInstance<CodeFixProvider>(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.TheCodeFixProvider,
                        out this._impl );

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