// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Queries;

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

    /// <summary>
    /// Returns a copy of the factory whose contributions are attributed to a given owner instead of the default owner of the state.
    /// </summary>
    IAdviceFactoryImpl WithQueryOwner( IQueryOwner owner );

    /// <summary>
    /// Creates the extension context of an adviser that uses this factory.
    /// </summary>
    /// <param name="adviserOwner">The adviser when it is itself an owner, or <c>null</c>, in which case the owner of the factory is used.</param>
    AdviserExtensionContext CreateExtensionContext( IQueryOwner? adviserOwner );
}