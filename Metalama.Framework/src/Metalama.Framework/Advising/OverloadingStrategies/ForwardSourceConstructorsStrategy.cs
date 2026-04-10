// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Advising.OverloadingStrategies;

internal sealed class ForwardSourceConstructorsStrategy : IConstructorOverloadingStrategy
{
    public bool ShouldGenerateForwarder( IConstructor mutatedConstructor, IParameter introducedParameter )
        => mutatedConstructor.Origin.Kind == DeclarationOriginKind.Source;

    [UsedImplicitly]
    private class Serializer : ReferenceTypeSerializer<ForwardSourceConstructorsStrategy>
    {
        public override ForwardSourceConstructorsStrategy CreateInstance( IArgumentsReader constructorArguments ) => new();

        public override void SerializeObject(
            ForwardSourceConstructorsStrategy obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments ) { }
    }
}
