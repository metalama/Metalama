// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime;

namespace Metalama.Framework.Engine.Advising;

internal sealed class TemplateMember<T> : TemplateMember
    where T : class, IMemberOrNamedType
{
    private readonly ISymbolRef<T> _declarationRef;

    internal override ISymbolRef<IMemberOrNamedType> GetDeclarationRef() => this._declarationRef;

    public T GetDeclaration( CompilationModel compilationModel ) => this._declarationRef.GetTarget( this.GetTemplateReflectionCompilation( compilationModel ) );

    public TemplateMember(
        ISymbolRef<T> implementation,
        TemplateClassMember templateClassMember,
        TemplateProvider templateProvider,
        IAdviceAttribute adviceAttribute,
        IObjectReader tags,
        TemplateKind selectedTemplateKind = TemplateKind.Default ) : this(
        implementation,
        templateClassMember,
        templateProvider,
        adviceAttribute,
        tags,
        selectedTemplateKind,
        selectedTemplateKind ) { }

    public TemplateMember(
        ISymbolRef<T> implementation,
        TemplateClassMember templateClassMember,
        TemplateProvider templateProvider,
        IAdviceAttribute adviceAttribute,
        IObjectReader tags,
        TemplateKind selectedTemplateKind,
        TemplateKind interpretedTemplateKind ) : base(
        implementation,
        templateClassMember,
        templateProvider,
        adviceAttribute,
        tags,
        selectedTemplateKind,
        interpretedTemplateKind )
    {
        this._declarationRef = (ISymbolRef<T>) implementation.As<T>();
    }

    public TemplateMember( TemplateMember prototype ) : base( prototype )
    {
        this._declarationRef = (ISymbolRef<T>) prototype.GetDeclarationRef().As<T>();
    }
}