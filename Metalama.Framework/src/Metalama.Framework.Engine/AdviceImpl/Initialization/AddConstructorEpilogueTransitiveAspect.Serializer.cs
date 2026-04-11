// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

internal sealed partial class AddConstructorEpilogueTransitiveAspect
{
    [UsedImplicitly]
    private sealed class Serializer : ReferenceTypeSerializer<AddConstructorEpilogueTransitiveAspect>
    {
#pragma warning disable SA1101

        public override AddConstructorEpilogueTransitiveAspect CreateInstance( IArgumentsReader constructorArguments )
        {
            var aspectOrder = constructorArguments.GetValue<int>( nameof(_aspectOrder) );

            return new AddConstructorEpilogueTransitiveAspect( aspectOrder );
        }

        public override void SerializeObject(
            AddConstructorEpilogueTransitiveAspect obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( nameof(_aspectOrder), obj._aspectOrder );
        }
#pragma warning restore SA1101
    }
}
