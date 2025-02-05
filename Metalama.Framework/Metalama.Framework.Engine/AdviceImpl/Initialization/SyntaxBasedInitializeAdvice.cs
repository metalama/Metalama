// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Templating.Statements;
using Metalama.Framework.Engine.Transformations;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

internal sealed class SyntaxBasedInitializeAdvice : InitializeAdvice
{
    private readonly IStatement _statement;

    public SyntaxBasedInitializeAdvice( AdviceConstructorParameters<IMemberOrNamedType> parameters, IStatement statement, InitializerKind kind )
        : base( parameters, kind )
    {
        this._statement = statement;
    }

    protected override void AddTransformation( IMemberOrNamedType targetDeclaration, IConstructor targetCtor, Action<ITransformation> addTransformation )
    {
        // TODO: The statement can now be more complex, including invoking a template. For this we need to pass a TemplateSyntaxFactoryImpl.
        addTransformation(
            new SyntaxBasedInitializationTransformation(
                this.AspectLayerInstance,
                targetDeclaration.ToRef(),
                targetCtor.ToFullRef(),
                _ => ((IStatementImpl) this._statement).GetSyntax( null ) ) );
    }
}