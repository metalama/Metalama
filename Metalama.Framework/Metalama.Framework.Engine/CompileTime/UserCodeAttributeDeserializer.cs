// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class UserCodeAttributeDeserializer : AttributeDeserializer
{
    private UserCodeAttributeDeserializer(
        in ProjectServiceProvider serviceProvider,
        CompileTimeTypeResolver resolver,
        CompilationContext compilationContext ) : base(
        serviceProvider,
        resolver,
        compilationContext ) { }

    public sealed class Provider : CompilationServiceProvider<UserCodeAttributeDeserializer>
    {
        public Provider( in ProjectServiceProvider serviceProvider ) : base( in serviceProvider ) { }

        protected override UserCodeAttributeDeserializer Create( CompilationContext compilationContext )
            => new(
                this.ServiceProvider,
                this.ServiceProvider.GetRequiredService<CompilationServiceProvider<ProjectSpecificCompileTimeTypeResolver>>().Get( compilationContext ),
                compilationContext );
    }
}