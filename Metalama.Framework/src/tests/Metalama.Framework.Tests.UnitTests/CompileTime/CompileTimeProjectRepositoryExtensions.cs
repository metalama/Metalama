// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

internal static class CompileTimeProjectRepositoryExtensions
{
    public static AttributeDeserializer CreateAttributeDeserializer(
        this CompileTimeProjectRepository repo,
        ProjectServiceProvider serviceProvider,
        CompilationContext compilationContext )
    {
        serviceProvider = serviceProvider.WithService( repo );
        serviceProvider = serviceProvider.WithService( new ProjectSpecificCompileTimeTypeResolver.Provider( serviceProvider ) );

        return new UserCodeAttributeDeserializer.Provider( serviceProvider ).Get( compilationContext );
    }
}