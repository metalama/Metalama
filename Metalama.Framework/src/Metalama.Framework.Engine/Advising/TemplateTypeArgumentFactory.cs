// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxGeneration;

namespace Metalama.Framework.Engine.Advising;

internal sealed class TemplateTypeArgumentFactory
{
    public IType Type { get; }

    public string Name { get; }

    public TemplateTypeArgumentFactory( IType type, string name )
    {
        this.Type = type;
        this.Name = name;
    }

    public TemplateTypeArgument Create( SyntaxGenerationContext context ) => Create( this.Type, this.Name, context );

    public static TemplateTypeArgument Create( IType type, string name, SyntaxGenerationContext context )
    {
        var syntax = context.SyntaxGenerator.TypeSyntax( type ).AssertNotNull();
        var syntaxForTypeOf = context.SyntaxGenerator.TypeOfExpression( type ).Type;

        return new TemplateTypeArgument( name, type, syntax, syntaxForTypeOf );
    }
}