// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;
using System.Linq;

namespace Metalama.Framework.Advising;

/// <summary>
/// A standard <see cref="IConstructorOverloadingStrategy"/> implementation that generates a forwarding
/// constructor for matching source constructors, optionally decorated with
/// <see cref="System.ObsoleteAttribute"/>. Obtain instances via
/// <see cref="ConstructorOverloadingStrategy.ForwardSourceConstructors"/> or
/// <see cref="ConstructorOverloadingStrategy.ForwardDefaultConstructor"/>, and add the obsolete decoration
/// by calling <see cref="WithObsoleteAttribute"/>.
/// </summary>
/// <seealso cref="ConstructorOverloadingStrategy"/>
/// <seealso cref="IConstructorOverloadingStrategy"/>
[CompileTime]
[PublicAPI]
public sealed class ForwardConstructorStrategy : IConstructorOverloadingStrategy
{
    private readonly bool _defaultConstructorOnly;
    private readonly bool _markObsolete;
    private readonly string? _obsoleteDescription;
    private readonly bool _obsoleteIsError;

    internal ForwardConstructorStrategy(
        bool defaultConstructorOnly,
        bool markObsolete = false,
        string? obsoleteDescription = null,
        bool obsoleteIsError = false )
    {
        this._defaultConstructorOnly = defaultConstructorOnly;
        this._markObsolete = markObsolete;
        this._obsoleteDescription = obsoleteDescription;
        this._obsoleteIsError = obsoleteIsError;
    }

    /// <summary>
    /// Returns a new <see cref="ForwardConstructorStrategy"/> that additionally decorates the generated
    /// forwarding constructor with <see cref="System.ObsoleteAttribute"/>. The scope (all source
    /// constructors vs the parameterless default constructor) is preserved.
    /// </summary>
    /// <param name="description">Optional deprecation message displayed at the call site. When <c>null</c>, a bare
    /// <c>[Obsolete]</c> attribute is emitted.</param>
    /// <param name="isError">When <c>true</c>, calling the forwarding constructor is a compile-time
    /// error instead of a warning.</param>
    public ForwardConstructorStrategy WithObsoleteAttribute( string? description = null, bool isError = false )
        => new( this._defaultConstructorOnly, markObsolete: true, description, isError );

    ConstructorOverloadingAction IConstructorOverloadingStrategy.GetConstructorOverloadingAction(
        IConstructor mutatedConstructor,
        IParameter introducedParameter )
    {
        if ( mutatedConstructor.Origin.Kind != DeclarationOriginKind.Source )
        {
            return ConstructorOverloadingAction.None;
        }

        if ( this._defaultConstructorOnly
             && mutatedConstructor.Parameters.Any( p => p.Origin.Kind == DeclarationOriginKind.Source ) )
        {
            return ConstructorOverloadingAction.None;
        }

        return this._markObsolete
            ? ConstructorOverloadingAction.ForwardAndMarkObsolete( this._obsoleteDescription, this._obsoleteIsError )
            : ConstructorOverloadingAction.Forward;
    }

    [UsedImplicitly]
    private class Serializer : ReferenceTypeSerializer<ForwardConstructorStrategy>
    {
        public override ForwardConstructorStrategy CreateInstance( IArgumentsReader constructorArguments )
        {
            var defaultConstructorOnly = constructorArguments.GetValue<bool>( nameof(ForwardConstructorStrategy._defaultConstructorOnly) );
            var markObsolete = constructorArguments.GetValue<bool>( nameof(ForwardConstructorStrategy._markObsolete) );
            var obsoleteDescription = constructorArguments.GetValue<string>( nameof(ForwardConstructorStrategy._obsoleteDescription) );
            var obsoleteIsError = constructorArguments.GetValue<bool>( nameof(ForwardConstructorStrategy._obsoleteIsError) );

            return new ForwardConstructorStrategy( defaultConstructorOnly, markObsolete, obsoleteDescription, obsoleteIsError );
        }

        public override void SerializeObject(
            ForwardConstructorStrategy obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( nameof(ForwardConstructorStrategy._defaultConstructorOnly), obj._defaultConstructorOnly );
            constructorArguments.SetValue( nameof(ForwardConstructorStrategy._markObsolete), obj._markObsolete );
            constructorArguments.SetValue( nameof(ForwardConstructorStrategy._obsoleteDescription), obj._obsoleteDescription );
            constructorArguments.SetValue( nameof(ForwardConstructorStrategy._obsoleteIsError), obj._obsoleteIsError );
        }
    }
}
