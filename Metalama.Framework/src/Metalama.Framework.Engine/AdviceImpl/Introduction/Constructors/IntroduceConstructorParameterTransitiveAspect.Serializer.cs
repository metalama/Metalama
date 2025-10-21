// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed partial class IntroduceConstructorParameterTransitiveAspect
{
    private sealed class Serializer : ReferenceTypeSerializer<IntroduceConstructorParameterTransitiveAspect>
    {
#pragma warning disable SA1101

        public override IntroduceConstructorParameterTransitiveAspect CreateInstance( IArgumentsReader constructorArguments )
        {
            var pullStrategy = constructorArguments.GetValue<IPullStrategy>( nameof(_pullStrategy) );
            var parameter = constructorArguments.GetValue<IRef<IParameter>>( nameof(_parameter) ).AssertNotNull();
            var order = constructorArguments.GetValue<int>( nameof(_order) );

            return new IntroduceConstructorParameterTransitiveAspect( pullStrategy, parameter, order );
        }

        public override void SerializeObject(
            IntroduceConstructorParameterTransitiveAspect obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( nameof(_pullStrategy), obj._pullStrategy );
            constructorArguments.SetValue( nameof(_parameter), obj._parameter );
            constructorArguments.SetValue( nameof(_order), obj._order );
        }
#pragma warning restore SA1101
    }
}