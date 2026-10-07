// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Describes the renaming of a parameter of a lambda or local function, or of a local variable, that hides a parameter of the member that contains a
/// redirected call site.
/// </summary>
/// <remarks>
/// <para>
/// A redirected call site can pass a parameter of the member that contains it, which is written as the name of the parameter. Since C# 8, a lambda or
/// a local function can declare a parameter or a local variable with the same name, and the name then designates this other variable at the call
/// site. The linker renames this other variable, its declaration and all its references, so that the name designates the parameter of the member.
/// </para>
/// <para>
/// The compiled code then no longer matches the source code, so the linker reports the warning
/// <see cref="AspectLinkerDiagnosticDescriptors.HidingVariableRenamed"/>.
/// </para>
/// </remarks>
internal sealed class CallSiteVariableRename
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallSiteVariableRename"/> class.
    /// </summary>
    public CallSiteVariableRename(
        SyntaxNode declarationNode,
        ImmutableArray<IdentifierNameSyntax> references,
        string oldName,
        string newName,
        string variableKind,
        ISymbol? member,
        string description )
    {
        this.DeclarationNode = declarationNode;
        this.References = references;
        this.OldName = oldName;
        this.NewName = newName;
        this.VariableKind = variableKind;
        this.Member = member;
        this.Description = description;
    }

    /// <summary>
    /// Gets the source node that declares the variable. It is a <see cref="ParameterSyntax"/>, a <see cref="VariableDeclaratorSyntax"/>, a
    /// <see cref="SingleVariableDesignationSyntax"/>, a <see cref="ForEachStatementSyntax"/> or a <see cref="CatchDeclarationSyntax"/>.
    /// </summary>
    public SyntaxNode DeclarationNode { get; }

    /// <summary>
    /// Gets the source nodes that reference the variable.
    /// </summary>
    public ImmutableArray<IdentifierNameSyntax> References { get; }

    /// <summary>
    /// Gets the name of the variable in the source code, which is also the name of the hidden parameter.
    /// </summary>
    public string OldName { get; }

    /// <summary>
    /// Gets the name of the variable in the compiled code.
    /// </summary>
    public string NewName { get; }

    /// <summary>
    /// Gets the kind of the variable, as it is written in the warning: <c>lambda parameter</c>, <c>local function parameter</c> or
    /// <c>local variable</c>.
    /// </summary>
    public string VariableKind { get; }

    /// <summary>
    /// Gets the member whose parameter the variable hides, or <c>null</c> when the symbol of the member is not available.
    /// </summary>
    public ISymbol? Member { get; }

    /// <summary>
    /// Gets the description of the first redirection that requires the renaming, which is written in the warning.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the location of the name of the variable in its declaration, where the warning is reported.
    /// </summary>
    public Location GetLocation()
        => this.DeclarationNode.Kind() switch
        {
            SyntaxKind.Parameter => ((ParameterSyntax) this.DeclarationNode).Identifier.GetLocation(),
            SyntaxKind.VariableDeclarator => ((VariableDeclaratorSyntax) this.DeclarationNode).Identifier.GetLocation(),
            SyntaxKind.SingleVariableDesignation => ((SingleVariableDesignationSyntax) this.DeclarationNode).Identifier.GetLocation(),
            SyntaxKind.ForEachStatement => ((ForEachStatementSyntax) this.DeclarationNode).Identifier.GetLocation(),
            SyntaxKind.CatchDeclaration => ((CatchDeclarationSyntax) this.DeclarationNode).Identifier.GetLocation(),
            _ => this.DeclarationNode.GetLocation()
        };
}
