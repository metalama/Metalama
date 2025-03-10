// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.CodeFixes
{
    // ReSharper disable UnusedType.Global

    /// <summary>
    /// Our implementation of <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [UsedImplicitly]
    public class TheCodeRefactoringProvider : CodeRefactoringProvider
    {
        static TheCodeRefactoringProvider()
        {
            DesignTimeServices.Initialize();
        }

        private readonly ILogger _logger;
        private readonly LocalWorkspaceProvider? _localWorkspaceProvider;
        private readonly IProjectOptionsFactory _projectOptionsFactory;
        private readonly DesignTimeExceptionHandler _exceptionHandler;
        private readonly DesignTimeExtensionManager _extensionManager;

        public TheCodeRefactoringProvider() : this( DesignTimeServiceProviderFactory.GetSharedServiceProvider() ) { }

        public TheCodeRefactoringProvider( GlobalServiceProvider serviceProvider )
        {
            this._logger = serviceProvider.GetLoggerFactory().GetLogger( "CodeRefactoring" );
            this._extensionManager = serviceProvider.GetRequiredService<DesignTimeExtensionManager>();
            this._localWorkspaceProvider = serviceProvider.GetService<LocalWorkspaceProvider>();
            this._projectOptionsFactory = serviceProvider.GetRequiredService<IProjectOptionsFactory>();
            this._exceptionHandler = serviceProvider.GetRequiredService<DesignTimeExceptionHandler>();
        }

        public sealed override Task ComputeRefactoringsAsync( CodeRefactoringContext context )
            => this.ComputeRefactoringsAsync( new CodeRefactoringContextAdapter( context ) );

        public virtual async Task ComputeRefactoringsAsync( ICodeRefactoringContext context )
        {
            this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}')" );

            this._localWorkspaceProvider?.TrySetWorkspace( context.Document.Project.Solution.Workspace );

            try
            {
                var projectKey = ProjectKeyFactory.FromProject( context.Document.Project );

                if ( projectKey == null || !projectKey.IsMetalamaEnabled )
                {
                    this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}'): not a Metalama project." );

                    return;
                }

                var projectOptions = this._projectOptionsFactory.GetProjectOptions( context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider );

                if ( !projectOptions.IsFrameworkEnabled )
                {
                    this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}'): not a Metalama project." );

                    return;
                }

                if ( !context.Document.SupportsSemanticModel )
                {
                    this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}'): no semantic model." );

                    return;
                }

                // Find the declaring node.
                var syntaxRoot = await context.Document.GetSyntaxRootAsync( context.CancellationToken );

                if ( syntaxRoot == null )
                {
                    this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}'): no syntax root." );

                    return;
                }

                if ( !syntaxRoot.Span.Contains( context.Span ) )
                {
                    this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}'): requested span out-of-bounds." );

                    return;
                }

                var node = syntaxRoot.FindNode( context.Span );

                // Do not provide refactorings on the method body, only on the declaration.
                if ( node.AncestorsAndSelf().Any( x => x is ExpressionSyntax or StatementSyntax ) )
                {
                    this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}'): caret is in a method body or expression body ({node.Kind()})." );

                    return;
                }

                // Do not attempt a remote call if we cannot get the declared symbol.
                var semanticModel = await context.Document.GetSemanticModelAsync( context.CancellationToken );

                if ( semanticModel == null )
                {
                    this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}'): no semantic model." );

                    return;
                }

                // Get the symbol.
                var declaredSymbol = semanticModel.GetDeclaredSymbol( node );

                if ( declaredSymbol == null )
                {
                    this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}'): no symbol for node '{node.Kind()}'." );

                    return;
                }

                this._logger.Trace?.Log( $"ComputeRefactorings('{context.Document.Name}'): we are on symbol '{declaredSymbol}'." );

                this._extensionManager.OnProjectDiscovered( projectOptions );

                await Task.WhenAll(
                    this._extensionManager.CodeRefactoringProviderExtensions.Select( e => e.ProvideRefactoringsAsync( context, projectKey, node ) ) );
            }
            catch ( Exception e ) when ( DesignTimeExceptionHandler.MustHandle( e ) )
            {
                this._exceptionHandler.ReportException( e );
            }
        }
    }
}