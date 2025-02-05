// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

                case ProcessKind.RoslynCodeAnalysisService:
                case ProcessKind.DevEnv:
                    this._impl = (CodeRefactoringProvider) ResourceExtractor.CreateInstance(
                        "Metalama.Framework.DesignTime",
                        "Metalama.Framework.DesignTime.VisualStudio.CodeFixes.VsCodeRefactoringProvider" );

                    break;

                case ProcessKind.Rider:
                    this._impl = (CodeRefactoringProvider) ResourceExtractor.CreateInstance(
                        "Metalama.Framework.DesignTime",
                        "Metalama.Framework.DesignTime.Rider.RiderCodeRefactoringProvider" );

                    break;

                default:
                    this._impl = (CodeRefactoringProvider) ResourceExtractor.CreateInstance(
                        "Metalama.Framework.DesignTime",
                        "Metalama.Framework.DesignTime.CodeFixes.TheCodeRefactoringProvider" );

                    break;
            }
        }

#pragma warning disable VSTHRD110
        public override Task ComputeRefactoringsAsync( CodeRefactoringContext context )
#pragma warning disable VSTHRD110 // Observe the awaitable result of this method call by awaiting it, assigning to a variable, or passing it to another method
            => this._impl?.ComputeRefactoringsAsync( context ) ?? Task.CompletedTask;
#pragma warning restore VSTHRD110
    }
}