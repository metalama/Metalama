// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Represents a template fully bound to a target method and template arguments.
/// </summary>
internal sealed class BoundTemplateMethod
{
    /// <summary>
    /// Gets the template member of the aspect.
    /// </summary>
    public TemplateMember<IMethod> TemplateMember { get; }

    public TemplateProvider TemplateProvider => this.TemplateMember.TemplateProvider;

    public BoundTemplateMethod( TemplateMember<IMethod> template, object?[] templateArguments )
    {
        this.TemplateMember = template;
        this.TemplateArguments = templateArguments;

#if DEBUG
        if ( ((IMethodSymbol) template.Symbol).MethodKind is MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove
             && templateArguments.Length != 1 )
        {
            throw new AssertionFailedException( $"'{template.Symbol}' is an accessor the the template has '{templateArguments.Length}' arguments." );
        }
#endif
    }

    /// <summary>
    /// Gets bound template arguments. This array consists of all parameters followed all type parameters.
    /// </summary>
    public object?[] TemplateArguments { get; }

    public object?[] GetTemplateArgumentsForMethod( IHasParameters signature )
    {
        Invariant.Assert(
            this.TemplateMember.TemplateClassMember.RunTimeParameters.Length == 0 ||
            this.TemplateMember.TemplateClassMember.RunTimeParameters.Length == signature.Parameters.Count );

        var newArguments = (object?[]) this.TemplateArguments.Clone();

        for ( var index = 0; index < this.TemplateMember.TemplateClassMember.RunTimeParameters.Length; index++ )
        {
            var runTimeParameter = this.TemplateMember.TemplateClassMember.RunTimeParameters[index];
            var parameter = signature.Parameters[index];

            newArguments[runTimeParameter.SourceIndex] = TypeAnnotationMapper.AddExpressionTypeAnnotation( SyntaxFactory.IdentifierName( parameter.Name ), parameter.Type );
        }

        return newArguments;
    }
}