// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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