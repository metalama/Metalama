// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using LINQPad;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.LinqPad
{
    /// <summary>
    /// Represents a hyperlink to an <see cref="IDeclaration"/>. When clicking on a hyperlink, a new query is opened, starting at
    /// the declaration.
    /// </summary>
    internal sealed class Permalink
    {
        private readonly GetCompilationInfo _getCompilationInfo;
        private readonly IDeclaration _declaration;

        public Permalink( GetCompilationInfo getCompilationInfo, IDeclaration declaration )
        {
            this._getCompilationInfo = getCompilationInfo;
            this._declaration = declaration;
        }

        public object? Format()
        {
            string? serializedReference;

            try
            {
                serializedReference = this._declaration.ToRef().ToSerializableId().ToString();
            }
            catch
            {
                // This is not implemented everywhere, so skip exceptions. 
                serializedReference = null;
            }

            if ( serializedReference == null )
            {
                return null;
            }
            else
            {
                var project = this._declaration.Compilation.Project;

                var projectNameLiteral = SyntaxFactory.Literal( project.Name ).Text;
                var targetFrameworkLiteral = SyntaxFactory.Literal( project.TargetFramework ?? "" ).Text;

                return new Hyperlinq(
                    QueryLanguage.Expression,
                    $@"{this._getCompilationInfo.WorkspaceExpression}.GetDeclaration({projectNameLiteral}, {targetFrameworkLiteral}, ""{serializedReference}"", {this._getCompilationInfo.IsMetalamaOutput.ToString().ToLowerInvariant()})",
                    "(open)" );
            }
        }
    }
}