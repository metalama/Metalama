// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.CodeFixes;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Rider;

[UsedImplicitly]
public sealed class RiderCodeFixProvider : TheCodeFixProvider
{
    public RiderCodeFixProvider() : this( DesignTimeServiceProviderFactory.GetSharedServiceProvider() ) { }

    public RiderCodeFixProvider( GlobalServiceProvider serviceProvider ) : base( serviceProvider ) { }

    private protected override ICodeFixContext WrapContext( ICodeFixContext context ) => new ContextWrapper( base.WrapContext( context ) );

    private sealed class ContextWrapper : ICodeFixContext
    {
        private readonly ICodeFixContext _wrapped;

        public ContextWrapper( ICodeFixContext wrapped )
        {
            this._wrapped = wrapped;
            this.Diagnostics = wrapped.Diagnostics.Where( d => d.Severity != DiagnosticSeverity.Hidden ).ToImmutableArray();
        }

        public Document Document => this._wrapped.Document;

        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public TextSpan Span => this._wrapped.Span;

        public CancellationToken CancellationToken => this._wrapped.CancellationToken;

        public void RegisterCodeFix( CodeAction action, ImmutableArray<Diagnostic> diagnostics ) => this._wrapped.RegisterCodeFix( action, diagnostics );
    }
}