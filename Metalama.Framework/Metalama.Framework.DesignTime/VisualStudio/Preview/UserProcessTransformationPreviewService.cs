// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Preview;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.DesignTime;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynProject = Microsoft.CodeAnalysis.Project;

namespace Metalama.Framework.DesignTime.VisualStudio.Preview
{
    internal sealed class UserProcessTransformationPreviewService : ITransformationPreviewService2
    {
        private readonly ServiceHubRpcService _serviceHub;

        public UserProcessTransformationPreviewService( GlobalServiceProvider serviceProvider )
        {
            this._serviceHub = serviceProvider.GetRequiredService<IServiceHubRpcServiceProvider>().ServiceHub;
        }

        public async Task PreviewTransformationAsync(
            Document document,
            IPreviewTransformationResult[] result,
            CancellationToken cancellationToken )
        {
            var syntaxTree = await document.GetSyntaxTreeAsync( cancellationToken );

            if ( syntaxTree == null )
            {
                // This should never happen.
                result[0] = PreviewTransformationResult.Failure( "Cannot get the syntax tree." );

                return;
            }

            var projectKey = ProjectKeyFactory.FromProject( document.Project );

            if ( projectKey == null || !projectKey.IsMetalamaEnabled )
            {
                result[0] = PreviewTransformationResult.Failure( "Metalama is not enabled for this project." );

                return;
            }

            var analysisProcessApi = await this._serviceHub.GetApiForProjectAsync<IPreviewTransformationRpcApi>(
                projectKey,
                cancellationToken );

            var unformattedResult = await analysisProcessApi.PreviewTransformationAsync( projectKey, syntaxTree.FilePath, cancellationToken );

            if ( !unformattedResult.IsSuccessful )
            {
                result[0] = PreviewTransformationResult.Failure( unformattedResult.ErrorMessages ?? [] );

                return;
            }

            var formattedSyntaxTree = await FormatOutputAsync( document, unformattedResult, cancellationToken );

            result[0] = PreviewTransformationResult.Success( formattedSyntaxTree.AssertNotNull(), unformattedResult.ErrorMessages );
        }

        public Task PreviewGeneratedFileAsync(
            RoslynProject project,
            string filePath,
            IPreviewTransformationResult[] result,
            CancellationToken cancellationToken )
        {
            var emptyDocument = project.AddDocument( Path.GetFileName( filePath ), SyntaxFactory.CompilationUnit(), filePath: filePath );

            return this.PreviewTransformationAsync( emptyDocument, result, cancellationToken );
        }

        internal static async Task<SyntaxTree?> FormatOutputAsync(
            Document document,
            SerializablePreviewTransformationResult unformattedResult,
            CancellationToken cancellationToken )
        {
            var syntaxTree = await document.GetSyntaxTreeAsync( cancellationToken );

            if ( syntaxTree == null )
            {
                return null;
            }

            var newSyntaxTree = unformattedResult.TransformedSyntaxTree!;

            var newDocument = document.WithSyntaxRoot(
                await newSyntaxTree.ToSyntaxTree( (CSharpParseOptions) syntaxTree.Options, cancellationToken ).GetRootAsync( cancellationToken ) );

            // Disable the Metalama source generator: it shouldn't run on transformed code.
            var newProject = newDocument.Project.WithAnalyzerReferences( [] );
            newDocument = newProject.GetDocument( document.Id )!;

            var formattedDocument = await new CodeFormatter().FormatAsync( newDocument, cancellationToken: cancellationToken, reformatAll: false );
            var formattedSyntaxTree = await formattedDocument.GetSyntaxTreeAsync( cancellationToken );

            return formattedSyntaxTree;
        }
    }
}