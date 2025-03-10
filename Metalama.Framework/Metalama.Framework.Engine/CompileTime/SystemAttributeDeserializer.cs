// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class SystemAttributeDeserializer : AttributeDeserializer
{
    private SystemAttributeDeserializer(
        in ProjectServiceProvider serviceProvider,
        SystemTypeResolver systemTypeResolver,
        CompilationContext compilationContext ) : base(
        serviceProvider,
        systemTypeResolver,
        compilationContext ) { }

    public sealed class Provider : CompilationServiceProvider<SystemAttributeDeserializer>
    {
        public Provider( in ProjectServiceProvider serviceProvider ) : base( in serviceProvider ) { }

        protected override SystemAttributeDeserializer Create( CompilationContext compilationContext )
        {
            var resolver = this.ServiceProvider.GetRequiredService<SystemTypeResolver.Provider>().Get( compilationContext );

            return new SystemAttributeDeserializer( this.ServiceProvider, resolver, compilationContext );
        }
    }
}