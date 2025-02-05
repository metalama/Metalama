// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;

namespace Metalama.Framework.Engine.Advising;

internal interface IAdviceFactoryImpl : IAdviceFactory
{
    ScopedDiagnosticSink Diagnostics { get; }

    AdviceFactory<TNewDeclaration> WithDeclaration<TNewDeclaration>( TNewDeclaration declaration )
        where TNewDeclaration : class, IDeclaration;

    IAdviceFactoryImpl WithTemplateClassInstance( TemplateClassInstance templateClassInstance );

    IAdviceFactoryImpl WithExplicitInterfaceImplementation( INamedType explicitlyImplementedInterfaceType );
}