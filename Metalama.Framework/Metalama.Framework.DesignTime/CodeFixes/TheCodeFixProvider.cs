// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.CodeFixes
{
    // ReSharper disable UnusedType.Global

    /// <summary>
    /// Our implementation of <see cref="CodeFixProvider"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [UsedImplicitly]
    public class TheCodeFixProvider : CodeFixProvider
    {
        static TheCodeFixProvider()
        {
            DesignTimeServices.Initialize();
        }

        private const string _makePartialKey = "Metalama.MakePartial";

        private readonly ILogger _logger;
        private readonly LocalWorkspaceProvider? _localWorkspaceProvider;
        private readonly IProjectOptionsFactory _projectOptionsFactory;
        private readonly DesignTimeExceptionHandler _exceptionHandler;
        private readonly DesignTimeExtensionManager _extensionManager;

        public TheCodeFixProvider() : this( DesignTimeServiceProviderFactory.GetSharedServiceProvider() ) { }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }

        public TheCodeFixProvider( GlobalServiceProvider serviceProvider )
        {
            this._logger = serviceProvider.GetLoggerFactory().GetLogger( "CodeFix" );
            var designTimeDiagnosticDefinitions = serviceProvider.GetRequiredService<IUserDiagnosticRegistrationService>().DiagnosticDefinitions;

            var fixableDiagnosticIds = ImmutableArray.Create( GeneralDiagnosticDescriptors.TypeNotPartial.Id )
                .Add( CodeFixHelper.SuggestionDiagnostic.Id )
                .AddRange( designTimeDiagnosticDefinitions.UserDiagnosticDescriptors.Keys );

            this.FixableDiagnosticIds = fixableDiagnosticIds;

            this._logger.Trace?.Log( $"Registered {fixableDiagnosticIds.Length} fixable diagnostic ids : {string.Join( ", ", fixableDiagnosticIds )}." );

            this._localWorkspaceProvider = serviceProvider.GetService<LocalWorkspaceProvider>();
            this._projectOptionsFactory = serviceProvider.GetRequiredService<IProjectOptionsFactory>();
            this._exceptionHandler = serviceProvider.GetRequiredService<DesignTimeExceptionHandler>();
            this._extensionManager = serviceProvider.GetRequiredService<DesignTimeExtensionManager>();
        }

        public override Task RegisterCodeFixesAsync( CodeFixContext context ) => this.RegisterCodeFixesAsync( new CodeFixContextAdapter( context ) );

        private protected virtual ICodeFixContext WrapContext( ICodeFixContext context ) => context;

        public Task RegisterCodeFixesAsync( ICodeFixContext context )
        {
            try
            {
                // Rider needs to modify the context.
                context = this.WrapContext( context );

                this._localWorkspaceProvider?.TrySetWorkspace( context.Document.Project.Solution.Workspace );

                this._logger.Trace?.Log( $"TheCodeFixProvider.RegisterCodeFixesAsync( project='{context.Document.Project.Name}' )" );

                this._logger.Trace?.Log(
                    $"TheCodeFixProvider.RegisterCodeFixesAsync( project='{context.Document.Project.Name}' ): input diagnostics = {context.Diagnostics.Select( x => x.Id ).Distinct()}" );

                var projectKey = ProjectKeyFactory.FromProject( context.Document.Project );

                if ( projectKey == null || !projectKey.IsMetalamaEnabled )
                {
                    this._logger.Trace?.Log(
                        $"TheCodeFixProvider.RegisterCodeFixesAsync( project='{context.Document.Project.Name}' ): not a Metalama project." );

                    return Task.CompletedTask;
                }

                var projectOptions = this._projectOptionsFactory.GetProjectOptions( context.Document.Project );

                if ( !projectOptions.IsFrameworkEnabled )
                {
                    this._logger.Trace?.Log(
                        $"TheCodeFixProvider.RegisterCodeFixesAsync( project='{context.Document.Project.Name}' ): not a Metalama project." );

                    return Task.CompletedTask;
                }

                if ( context.Diagnostics.Any( d => d.Id == GeneralDiagnosticDescriptors.TypeNotPartial.Id ) )
                {
                    // This is a hard-coded code fix. It may need to be refactored with our framework.

                    this._logger.Trace?.Log(
                        $"TheCodeFixProvider.RegisterCodeFixesAsync( project='{context.Document.Project.Name}' ): registering 'make partial'" );

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Make partial",
                            cancellationToken => this.GetFixedDocumentAsync( context.Document, context.Span, cancellationToken.IgnoreIfDebugging() ),
                            _makePartialKey ),
                        context.Diagnostics );
                }

                this._extensionManager.OnProjectDiscovered( projectOptions );

                return Task.WhenAll( this._extensionManager.CodeFixProviderExtensions.Select( e => e.ProvideCodeFixesAsync( projectKey, context ) ) );
            }
            catch ( Exception e ) when ( DesignTimeExceptionHandler.MustHandle( e ) )
            {
                this._exceptionHandler.ReportException( e, this._logger );

                return Task.CompletedTask;
            }
        }

        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private async Task<Document> GetFixedDocumentAsync( Document document, TextSpan span, CancellationToken cancellationToken )
        {
            try
            {
                var syntaxRoot = await document.GetSyntaxRootAsync( cancellationToken );

                if ( syntaxRoot == null )
                {
                    this._logger.Trace?.Log(
                        $"TheCodeFixProvider.GetFixedDocumentAsync( project='{document.Project.Name}' ): no syntax root for '{document.Name}'." );

                    return document;
                }

                if ( !syntaxRoot.Span.Contains( span ) )
                {
                    this._logger.Trace?.Log(
                        $"TheCodeFixProvider.GetFixedDocumentAsync( project='{document.Project.Name}' ): requested span out-of-bounds in '{document.Name}'." );

                    return document;
                }

                var node = syntaxRoot.FindNode( span );
                var typeDeclaration = GetTypeDeclaration( node );

                if ( typeDeclaration == null )
                {
                    this._logger.Trace?.Log( $"TheCodeFixProvider.GetFixedDocumentAsync( project='{document.Project.Name}' ): not in a type declaration." );

                    return document;
                }

                var newTypeDeclaration = typeDeclaration.AddModifiers( SyntaxFactory.Token( SyntaxKind.PartialKeyword ) );
                var newSyntaxRoot = syntaxRoot.ReplaceNode( typeDeclaration, newTypeDeclaration );
                var newDocument = document.WithSyntaxRoot( newSyntaxRoot );

                return newDocument;
            }
            catch ( Exception e )
            {
                this._exceptionHandler.ReportException( e, this._logger );

                return document;
            }
        }

        private static BaseTypeDeclarationSyntax? GetTypeDeclaration( SyntaxNode node )
            => node switch
            {
                BaseTypeDeclarationSyntax typeDeclaration => typeDeclaration,
                { Parent: { } parent } => GetTypeDeclaration( parent ),
                _ => null
            };
    }
}