// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Advising.PullStrategies;

internal class IntroduceParameterPullStrategy : IPullStrategy
{
    private readonly string? _parameterName;
    private readonly IRef<IType>? _parameterType;
    private readonly string? _parameterDefaultValue;

    public IntroduceParameterPullStrategy(
        string? parameterName,
        IRef<IType>? parameterType,
        string? parameterDefaultValue )
    {
        this._parameterName = parameterName;
        this._parameterType = parameterType;
        this._parameterDefaultValue = parameterDefaultValue;
    }

    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember )
        => PullAction.IntroduceParameterAndPull(
            this._parameterName ?? pulledParameter.Name,
            this._parameterType?.GetTarget( targetMember.Compilation ) ?? pulledParameter.Type,
            ExpressionFactory.Parse( this._parameterDefaultValue ) );

    private class Serializer : ReferenceTypeSerializer<IntroduceParameterPullStrategy>
    {
#pragma warning disable SA1101

        public override IntroduceParameterPullStrategy CreateInstance( IArgumentsReader constructorArguments )
        {
            var parameterName = constructorArguments.GetValue<string>( nameof(_parameterName) )!;
            var parameterType = constructorArguments.GetValue<IRef<IType>>( nameof(_parameterType) )!;
            var parameterDefaultValue = constructorArguments.GetValue<string>( nameof(_parameterDefaultValue) );

            return new IntroduceParameterPullStrategy( parameterName, parameterType, parameterDefaultValue );
        }

        public override void SerializeObject(
            IntroduceParameterPullStrategy obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( nameof(_parameterName), obj._parameterName );
            constructorArguments.SetValue( nameof(_parameterType), obj._parameterType );
            constructorArguments.SetValue( nameof(_parameterDefaultValue), obj._parameterDefaultValue );
        }
#pragma warning restore SA1101
    }
}