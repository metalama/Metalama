// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using System;
using System.ComponentModel;

namespace Metalama.Framework.CompilerExtensions
{
    // ReSharper disable UnusedType.Global

    [Transformer]
    [DisplayName( "Metalama.Framework" )] // This name is used in telemetry. Changing it causes inconsistent data.
    public sealed class MetalamaSourceTransformer : ISourceTransformerWithServices
    {
        private readonly ISourceTransformerWithServices _impl = (ISourceTransformerWithServices) ResourceExtractor.CreateInstance(
            "Metalama.Framework.Engine",
            "Metalama.Framework.Engine.Pipeline.SourceTransformer" );

        public IServiceProvider? InitializeServices( InitializeServicesContext context ) => this._impl.InitializeServices( context );

        public void Execute( TransformerContext context ) => this._impl.Execute( context );
    }
}