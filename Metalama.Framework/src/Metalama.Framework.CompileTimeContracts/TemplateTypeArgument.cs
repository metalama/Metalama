// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.CompileTimeContracts
{
    /// <summary>
    /// Value of a template type argument. 
    /// </summary>
    [PublicAPI]
    public sealed class TemplateTypeArgument
    {
        public string Name { get; }

        public IType Type { get; }

        public TypeSyntax Syntax { get; }

        public TypeSyntax SyntaxWithoutNullabilityAnnotations { get; }

        internal TemplateTypeArgument( string name, IType type, TypeSyntax syntax, TypeSyntax syntaxWithoutNullabilityAnnotations )
        {
            this.Name = name;
            this.Type = type;
            this.Syntax = syntax;
            this.SyntaxWithoutNullabilityAnnotations = syntaxWithoutNullabilityAnnotations;
        }
    }
}