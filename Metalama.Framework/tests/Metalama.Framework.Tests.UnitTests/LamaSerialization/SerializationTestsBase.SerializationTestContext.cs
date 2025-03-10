// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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