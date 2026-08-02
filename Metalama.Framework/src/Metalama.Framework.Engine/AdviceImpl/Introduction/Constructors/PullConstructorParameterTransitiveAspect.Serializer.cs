// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed partial class PullConstructorParameterTransitiveAspect
{
    [UsedImplicitly]
    private sealed class Serializer : ReferenceTypeSerializer<PullConstructorParameterTransitiveAspect>
    {
#pragma warning disable SA1101

        public override PullConstructorParameterTransitiveAspect CreateInstance( IArgumentsReader constructorArguments )
        {
            var pullStrategy = constructorArguments.GetValue<IPullStrategy>( nameof(_pullStrategy) );
            var parameter = constructorArguments.GetValue<IRef<IParameter>>( nameof(_parameter) ).AssertNotNull();
            var order = constructorArguments.GetValue<int>( nameof(_order) );
            var overloadingStrategy = constructorArguments.GetValue<IConstructorOverloadingStrategy>( nameof(_overloadingStrategy) );
            var declaringConstructor = constructorArguments.GetValue<IRef<IConstructor>>( nameof(_declaringConstructor) ).AssertNotNull();
            var parameterName = constructorArguments.GetValue<string>( nameof(_parameterName) ).AssertNotNull();
            var parameterType = constructorArguments.GetValue<IRef<IType>>( nameof(_parameterType) ).AssertNotNull();
            var parameterIndex = constructorArguments.GetValue<int>( nameof(_parameterIndex) );
            var parameterRefKind = constructorArguments.GetValue<RefKind>( nameof(_parameterRefKind) );

            return new PullConstructorParameterTransitiveAspect(
                pullStrategy,
                parameter,
                order,
                overloadingStrategy,
                declaringConstructor,
                parameterName,
                parameterType,
                parameterIndex,
                parameterRefKind );
        }

        public override void SerializeObject(
            PullConstructorParameterTransitiveAspect obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( nameof(_pullStrategy), obj._pullStrategy );
            constructorArguments.SetValue( nameof(_parameter), obj._parameter );
            constructorArguments.SetValue( nameof(_order), obj._order );
            constructorArguments.SetValue( nameof(_overloadingStrategy), obj._overloadingStrategy );
            constructorArguments.SetValue( nameof(_declaringConstructor), obj._declaringConstructor );
            constructorArguments.SetValue( nameof(_parameterName), obj._parameterName );
            constructorArguments.SetValue( nameof(_parameterType), obj._parameterType );
            constructorArguments.SetValue( nameof(_parameterIndex), obj._parameterIndex );
            constructorArguments.SetValue( nameof(_parameterRefKind), obj._parameterRefKind );
        }
#pragma warning restore SA1101
    }
}
