// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using System.Composition;
using System.Threading.Tasks;

namespace Metalama.Framework.CompilerExtensions
{
    // ReSharper disable UnusedType.Global

    [ExportCodeRefactoringProvider( LanguageNames.CSharp, Name = nameof(MetalamaCodeRefactoringProvider) )]
    [Shared]
    public class MetalamaCodeRefactoringProvider : CodeRefactoringProvider
    {
        private readonly CodeRefactoringProvider? _impl;

        public MetalamaCodeRefactoringProvider()
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
                    ResourceExtractor.TryCreateInstance<CodeRefactoringProvider>(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.VsCodeRefactoringProvider,
                        out this._impl );

                    break;

                case ProcessKind.Rider:
                    ResourceExtractor.TryCreateInstance<CodeRefactoringProvider>(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.RiderCodeRefactoringProvider,
                        out this._impl );

                    break;

                default:
                    ResourceExtractor.TryCreateInstance<CodeRefactoringProvider>(
                        RoslynEntryPointTypeNames.DesignTimeAssemblyName,
                        RoslynEntryPointTypeNames.TheCodeRefactoringProvider,
                        out this._impl );

                    break;
            }
        }

#pragma warning disable VSTHRD110 // Observe the awaitable result of this method call by awaiting it, assigning to a variable, or passing it to another method
        public override Task ComputeRefactoringsAsync( CodeRefactoringContext context )
            => this._impl?.ComputeRefactoringsAsync( context ) ?? Task.CompletedTask;
#pragma warning restore VSTHRD110
    }
}