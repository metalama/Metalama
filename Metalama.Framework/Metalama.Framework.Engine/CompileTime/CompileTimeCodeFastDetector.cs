// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// Implements a fast way to detect whether a syntax tree contains compile-time code, just by looking at namespace imports.
    /// This way is imprecise, but it is enforced by an analyzer <see cref="TemplatingCodeValidator"/>. 
    /// </summary>
    public static class CompileTimeCodeFastDetector
    {
        internal const string FrameworkNamespace = "Metalama.Framework";

        private static readonly ImmutableHashSet<string> _frameworkSubNamespaces = ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "Aspects",
            "Code",
            "Diagnostics",
            "Engine",
            "Fabrics",
            "Project",
            "Services",
            "Options",
            "Metrics",
            "Eligibility",
            "Advising",
            "Serialization" );

        public static bool HasCompileTimeCode( SyntaxNode node ) => DetectCompileTimeVisitor.Instance.Visit( node );

        private sealed class DetectCompileTimeVisitor : SafeSyntaxVisitor<bool>
        {
            public static readonly DetectCompileTimeVisitor Instance = new();

            public override bool VisitUsingDirective( UsingDirectiveSyntax node )
            {
                if ( node.GlobalKeyword.IsKind( SyntaxKind.GlobalKeyword ) )
                {
                    // Any tree containing a global using must be included in the set of compile-time trees because they need to be scanned.
                    return true;
                }
                else
                {
                    // Any tree containing a using directive for Metalama.Framework needs to be included.
                    if ( node.Name != null && IsMetalamaFramework( node.Name ) )
                    {
                        return true;
                    }

                    if ( node.Name is QualifiedNameSyntax qualifiedName &&
                        _frameworkSubNamespaces.Contains( qualifiedName.Right.Identifier.ValueText ) &&
                        IsMetalamaFramework( qualifiedName.Left ) )
                    {
                        return true;
                    }

                    return false;

                    static bool IsMetalamaFramework( NameSyntax nameSyntax )
                        => nameSyntax is QualifiedNameSyntax
                        {
                            Right.Identifier.ValueText: "Framework", Left: IdentifierNameSyntax { Identifier.ValueText: "Metalama" }
                        };
                }
            }

            public override bool VisitNamespaceDeclaration( NamespaceDeclarationSyntax node ) => node.ChildNodes().Any( this.Visit );

            public override bool VisitFileScopedNamespaceDeclaration( FileScopedNamespaceDeclarationSyntax node ) => node.ChildNodes().Any( this.Visit );

            public override bool VisitCompilationUnit( CompilationUnitSyntax node ) => node.ChildNodes().Any( this.Visit );

            public override bool DefaultVisit( SyntaxNode node ) => false;
        }
    }
}