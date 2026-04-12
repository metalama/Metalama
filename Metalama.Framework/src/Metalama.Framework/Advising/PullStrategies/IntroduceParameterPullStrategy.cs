// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Serialization;
using System.Linq;

namespace Metalama.Framework.Advising.PullStrategies;

internal class IntroduceParameterPullStrategy : IPullStrategy
{
    private readonly string? _parameterName;
    private readonly IRef<IType>? _parameterType;
    private readonly string? _parameterDefaultValue;
    private readonly bool _reuseExistingParameterOfSameType;
    private readonly bool _materializeOnRecord;

    internal bool MaterializeOnRecord => this._materializeOnRecord;

    public IntroduceParameterPullStrategy(
        string? parameterName,
        IRef<IType>? parameterType,
        string? parameterDefaultValue,
        bool reuseExistingParameterOfSameType = false,
        bool materializeOnRecord = false )
    {
        this._parameterName = parameterName;
        this._parameterType = parameterType;
        this._parameterDefaultValue = parameterDefaultValue;
        this._reuseExistingParameterOfSameType = reuseExistingParameterOfSameType;
        this._materializeOnRecord = materializeOnRecord;
    }

    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember )
    {
        // A forwarding constructor has a fixed signature (it keeps the pre-mutation
        // source signature). We cannot append a new parameter to it, so forward the configured default
        // value (or default(T) if none was supplied) to the mutated constructor.
        if ( targetMember is IConstructor constructor && constructor.IsSourceCompatibilityConstructor() )
        {
            return this._parameterDefaultValue != null
                ? PullAction.UseExpression( ExpressionFactory.Parse( this._parameterDefaultValue ) )
                : PullAction.UseConstant( TypedConstant.Default( pulledParameter.Type ) );
        }

        var resolvedParameterType = this._parameterType?.GetTarget( targetMember.Compilation ) ?? pulledParameter.Type;

        // Opt-in: if the target member already has a parameter of the same type, forward it rather than
        // introducing a duplicate. This is used by framework-internal flows that pull marker types
        // (e.g. InitializationContext) where two parameters of the same type would never be intentional.
        // It is off by default because for general types (int, string, ...) it would silently hijack
        // unrelated parameters.
        if ( this._reuseExistingParameterOfSameType )
        {
            var existing = targetMember.Parameters.FirstOrDefault( p => p.Type.Equals( resolvedParameterType ) );

            if ( existing != null )
            {
                return PullAction.UseExistingParameter( existing );
            }
        }

        return PullAction.IntroduceParameterAndPull(
            this._parameterName ?? pulledParameter.Name,
            resolvedParameterType,
            ExpressionFactory.Parse( this._parameterDefaultValue ),
            materializeOnRecord: this._materializeOnRecord );
    }

    [UsedImplicitly]
    private class Serializer : ReferenceTypeSerializer<IntroduceParameterPullStrategy>
    {
#pragma warning disable SA1101

        public override IntroduceParameterPullStrategy CreateInstance( IArgumentsReader constructorArguments )
        {
            var parameterName = constructorArguments.GetValue<string>( nameof(_parameterName) )!;
            var parameterType = constructorArguments.GetValue<IRef<IType>>( nameof(_parameterType) )!;
            var parameterDefaultValue = constructorArguments.GetValue<string>( nameof(_parameterDefaultValue) );
            var reuseExistingParameterOfSameType = constructorArguments.GetValue<bool>( nameof(_reuseExistingParameterOfSameType) );
            var materializeOnRecord = constructorArguments.GetValue<bool>( nameof(_materializeOnRecord) );

            return new IntroduceParameterPullStrategy(
                parameterName,
                parameterType,
                parameterDefaultValue,
                reuseExistingParameterOfSameType,
                materializeOnRecord );
        }

        public override void SerializeObject(
            IntroduceParameterPullStrategy obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( nameof(_parameterName), obj._parameterName );
            constructorArguments.SetValue( nameof(_parameterType), obj._parameterType );
            constructorArguments.SetValue( nameof(_parameterDefaultValue), obj._parameterDefaultValue );
            constructorArguments.SetValue( nameof(_reuseExistingParameterOfSameType), obj._reuseExistingParameterOfSameType );
            constructorArguments.SetValue( nameof(_materializeOnRecord), obj._materializeOnRecord );
        }
#pragma warning restore SA1101
    }
}