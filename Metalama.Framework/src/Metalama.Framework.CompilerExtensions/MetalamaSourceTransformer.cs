// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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