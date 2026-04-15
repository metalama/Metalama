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
    private readonly string? _forwarderExpression;
    private readonly bool _reuseExistingParameterOfCompatibleType;
    private readonly bool _materializeOnRecord;

    internal bool MaterializeOnRecord => this._materializeOnRecord;

    public IntroduceParameterPullStrategy(
        string? parameterName,
        IRef<IType>? parameterType,
        string? parameterDefaultValue,
        string? forwarderExpression = null,
        bool reuseExistingParameterOfCompatibleType = false,
        bool materializeOnRecord = false )
    {
        this._parameterName = parameterName;
        this._parameterType = parameterType;
        this._parameterDefaultValue = parameterDefaultValue;
        this._forwarderExpression = forwarderExpression;
        this._reuseExistingParameterOfCompatibleType = reuseExistingParameterOfCompatibleType;
        this._materializeOnRecord = materializeOnRecord;
    }

    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember )
    {
        var resolvedParameterType = this._parameterType?.GetTarget( targetMember.Compilation ) ?? pulledParameter.Type;

        // Opt-in: if the target member already has a parameter whose type is convertible to the pulled
        // parameter type, forward it rather than introducing a duplicate. This is used by framework-
        // internal flows that pull marker types (e.g. InitializationContext) and by dependency-injection
        // scenarios where two parameters of the same service type would never be intentional.
        // It is off by default because for general types (int, string, ...) it would silently hijack
        // unrelated parameters. IsConvertibleTo (rather than Equals) is used so that covariant interfaces
        // such as ILogger<out T> are handled correctly.
        if ( this._reuseExistingParameterOfCompatibleType )
        {
            // 1. More-specific or equal existing parameter → forward it directly.
            var existing = targetMember.Parameters.FirstOrDefault( p => p.Type.IsConvertibleTo( resolvedParameterType ) );

            if ( existing != null )
            {
                return PullAction.UseExistingParameter( existing );
            }

            // 2. Less-specific INTRODUCED parameter → replace its type with the more specific one
            //    and continue pulling recursively so that further-derived constructors also get updated.
            //    Only introduced parameters can be replaced; source-defined parameters must not be mutated.
            var lessSpecific = targetMember.Parameters.FirstOrDefault(
                p => resolvedParameterType.IsConvertibleTo( p.Type ) && p.Origin.Kind == DeclarationOriginKind.Aspect );

            if ( lessSpecific != null )
            {
                return PullAction.ReplaceExistingParameterTypeAndPull( lessSpecific, resolvedParameterType );
            }
        }

        return PullAction.IntroduceParameterAndPull(
            this._parameterName ?? pulledParameter.Name,
            resolvedParameterType,
            parameterDefaultValue: this._parameterDefaultValue != null ? ExpressionFactory.Parse( this._parameterDefaultValue ) : null,
            forwarderExpression: this._forwarderExpression != null ? ExpressionFactory.Parse( this._forwarderExpression ) : null,
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
            var forwarderExpression = constructorArguments.GetValue<string>( nameof(_forwarderExpression) );
            var reuseExistingParameterOfCompatibleType = constructorArguments.GetValue<bool>( nameof(_reuseExistingParameterOfCompatibleType) );
            var materializeOnRecord = constructorArguments.GetValue<bool>( nameof(_materializeOnRecord) );

            return new IntroduceParameterPullStrategy(
                parameterName,
                parameterType,
                parameterDefaultValue,
                forwarderExpression,
                reuseExistingParameterOfCompatibleType,
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
            constructorArguments.SetValue( nameof(_forwarderExpression), obj._forwarderExpression );
            constructorArguments.SetValue( nameof(_reuseExistingParameterOfCompatibleType), obj._reuseExistingParameterOfCompatibleType );
            constructorArguments.SetValue( nameof(_materializeOnRecord), obj._materializeOnRecord );
        }
#pragma warning restore SA1101
    }
}
