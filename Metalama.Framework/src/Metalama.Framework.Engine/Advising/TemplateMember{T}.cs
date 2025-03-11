// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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