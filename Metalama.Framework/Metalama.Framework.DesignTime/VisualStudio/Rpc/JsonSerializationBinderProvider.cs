// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.Rpc;

internal sealed class JsonSerializationBinderProvider : IJsonSerializationBinderProvider
{
    public JsonSerializationBinder Binder { get; } = new(
        configuration =>
        {
            configuration.AddAssemblyOfType( typeof(IAspect) );
#if ROSLYN_4_4_0
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.DesignTime.4.4.0" );
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.Engine.4.4.0" );
#endif

#if ROSLYN_4_8_0
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.DesignTime.4.8.0" );
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.Engine.4.8.0" );
#endif

#if ROSLYN_4_12_0
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.DesignTime.4.12.0" );
            configuration.AddAssemblyWithSameVersionThanType( typeof(ProjectKey), "Metalama.Framework.Engine.4.12.0" );
#endif
        } );
}