// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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