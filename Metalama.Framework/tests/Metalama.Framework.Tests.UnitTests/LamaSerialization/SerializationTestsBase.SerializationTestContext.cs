// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Testing.UnitTesting;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

public abstract partial class SerializationTestsBase
{
    protected sealed class SerializationTestContext : TestContext
    {
        private readonly DisposeAction _disposeAction;

        public SerializationTestContext( TestContextOptions contextOptions, IAdditionalServiceCollection? additionalServices = null )
            : base( contextOptions, additionalServices )
        {
            var specializedOptions = contextOptions as SerializationTestContextOptions;

            var compilation = this.CreateCompilationModel( specializedOptions?.Code ?? "" );
            this.Compilation = compilation;
            this._disposeAction = UserCodeExecutionContext.WithContext( this.ServiceProvider, compilation );
            this.Serializer = CompileTimeSerializer.CreateInstance( this.ServiceProvider, compilation.CompilationContext );
        }

        public CompilationModel Compilation { get; }

        internal CompileTimeSerializer Serializer { get; }

        protected override void Dispose( bool disposing )
        {
            base.Dispose( disposing );
            this._disposeAction.Dispose();
        }
    }
}