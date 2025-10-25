// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;

namespace Metalama.Framework.Engine.Advising;

internal interface IAdviceFactoryImpl : IAdviceFactory
{
    new CompilationModel Compilation { get; }

    new CompilationModel MutableCompilation { get; }

    ScopedDiagnosticSink Diagnostics { get; }

    AdviceFactory<TNewDeclaration> WithDeclaration<TNewDeclaration>( TNewDeclaration declaration )
        where TNewDeclaration : class, IDeclaration;

    IAdviceFactoryImpl WithTemplateClassInstance( TemplateClassInstance templateClassInstance );

    IAdviceFactoryImpl WithExplicitInterfaceImplementation( INamedType explicitlyImplementedInterfaceType );
}