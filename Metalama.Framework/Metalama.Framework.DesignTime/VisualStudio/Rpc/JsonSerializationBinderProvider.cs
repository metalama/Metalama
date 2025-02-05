// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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