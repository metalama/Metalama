// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Framework.Engine.Templating;

#pragma warning disable IDE0051 // Remove unused private members

internal sealed partial class RoslynVersionSyntaxVerifier : SafeSyntaxWalker
{
    private readonly IDiagnosticAdder _diagnostics;

    [UsedImplicitly]
    public LanguageVersion MaximalAcceptableLanguageVersion { get; }

    public RoslynApiVersion MaximalUsedVersion { get; private set; } = RoslynApiVersion.Lowest;

    public RoslynVersionSyntaxVerifier( IDiagnosticAdder diagnostics, LanguageVersion maximalAcceptableLanguageVersion )
    {
        this._diagnostics = diagnostics;
        this.MaximalAcceptableLanguageVersion = maximalAcceptableLanguageVersion;
    }

    private void OnForbiddenSyntaxUsed( in SyntaxNodeOrToken node )
    {
        this._diagnostics.Report(
            TemplatingDiagnosticDescriptors.TemplateUsesUnsupportedLanguageVersion.CreateRoslynDiagnostic(
                node.GetLocation(),
                this.MaximalAcceptableLanguageVersion.ToDisplayString() ) );
    }

    // ReSharper disable once UnusedMember.Local
    private void VisitVersionSpecificNode( SyntaxNode node, RoslynApiVersion version )
    {
        if ( version.ToLanguageVersion() > this.MaximalAcceptableLanguageVersion )
        {
            this.OnForbiddenSyntaxUsed( node );
        }

        if ( version > this.MaximalUsedVersion )
        {
            this.MaximalUsedVersion = version;
        }
    }

    // ReSharper disable once UnusedMember.Local
    private void VisitVersionSpecificField( in SyntaxNodeOrToken nodeOrToken, RoslynApiVersion version )
    {
        // TODO: A field can be added in a new version of Roslyn that returns a concrete value for old code,
        // when the new field is a generalization of an old field.
        // For example, in Roslyn 4.8, the new field UsingDirectiveSyntax.NamespaceOrType (a generalization of UsingDirectiveSyntax.Name) is always not null.
        // Though this is not a problem at the moment and I'm not certain how to fix it (check whether the new field is optional?),
        // I'm keeping this as is for now.

        if ( !nodeOrToken.IsKind( SyntaxKind.None ) )
        {
            if ( version.ToLanguageVersion() > this.MaximalAcceptableLanguageVersion )
            {
                this.OnForbiddenSyntaxUsed( nodeOrToken );
            }

            if ( version > this.MaximalUsedVersion )
            {
                this.MaximalUsedVersion = version;
            }
        }
    }

    // ReSharper disable once UnusedMember.Local
    private void VisitVersionSpecificFieldKind( in SyntaxNodeOrToken nodeOrToken, RoslynApiVersion version )
    {
        if ( version.ToLanguageVersion() > this.MaximalAcceptableLanguageVersion )
        {
            this.OnForbiddenSyntaxUsed( nodeOrToken );
        }

        if ( version > this.MaximalUsedVersion )
        {
            this.MaximalUsedVersion = version;
        }
    }
}