// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;
using System.Linq;

namespace Metalama.Framework.Advising.OverloadingStrategies;

internal sealed class ForwardDefaultConstructorStrategy : IConstructorOverloadingStrategy
{
    public bool ShouldGenerateForwarder( IConstructor mutatedConstructor, IParameter introducedParameter )
    {
        // Only preserve the parameterless source-origin constructor: the typical `Activator.CreateInstance<T>()`
        // / `new T()` scenario. Parameters are only ever appended, so counting the source-origin parameters gives
        // the pre-mutation arity.
        if ( mutatedConstructor.Origin.Kind != DeclarationOriginKind.Source )
        {
            return false;
        }

        return !mutatedConstructor.Parameters.Any( p => p.Origin.Kind == DeclarationOriginKind.Source );
    }

    [UsedImplicitly]
    private class Serializer : ReferenceTypeSerializer<ForwardDefaultConstructorStrategy>
    {
        public override ForwardDefaultConstructorStrategy CreateInstance( IArgumentsReader constructorArguments ) => new();

        public override void SerializeObject(
            ForwardDefaultConstructorStrategy obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments ) { }
    }
}
