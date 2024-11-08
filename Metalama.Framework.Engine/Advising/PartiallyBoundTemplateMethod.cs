// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Represents binding of the template method against template arguments at the point where the target declaration is not yet known.
/// It is used to bind type parameters of introduction templates before the final declaration is known.
/// The template may become invalid after binding to the final target declaration.
/// </summary>
internal sealed class PartiallyBoundTemplateMethod( TemplateMember<IMethod> template, object?[] typeArguments, IObjectReader? argumentReader )
{
    /// <summary>
    /// Gets the template member of the aspect.
    /// </summary>
    public TemplateMember<IMethod> TemplateMember { get; } = template;

    /// <summary>
    /// Gets the template declaration.
    /// </summary>
    public IMethod GetDeclaration( CompilationModel compilation ) => this.TemplateMember.GetDeclaration( compilation );

    /// <summary>
    /// Gets arguments of the template.
    /// </summary>
    public IObjectReader? TemplateArguments { get; } = argumentReader;

    /// <summary>
    /// Gets bound template type arguments.
    /// </summary>
    public object?[] TypeArguments { get; } = typeArguments;
}