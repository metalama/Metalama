// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Collections.Concurrent;

namespace Metalama.Patterns.Observability.Implementation;

[CompileTime]
internal sealed class InpcInstrumentationKindLookup
{
    private readonly ConcurrentDictionary<IType, InpcInstrumentationKind> _inpcInstrumentationKindLookup = new();
    private readonly INamedType _targetType;
    private readonly Assets _assets;

    public InpcInstrumentationKindLookup( INamedType targetType, Assets assets )
    {
        this._targetType = targetType;
        this._assets = assets;
    }

    public InpcInstrumentationKind Get( IType type ) => this._inpcInstrumentationKindLookup.GetOrAdd( type, this.GetCore );

    private InpcInstrumentationKind GetCore( IType type )
    {
        switch ( type )
        {
            case INamedType namedType:
                if ( namedType.SpecialType != SpecialType.None )
                {
                    // None of the special types implement INPC.
                    return InpcInstrumentationKind.None;
                }
                else if ( namedType.IsConvertibleTo( this._assets.INotifyPropertyChanged ) )
                {
                    if ( namedType.TryFindImplementationForInterfaceMember( this._assets.PropertyChangedEventOfINotifyPropertyChanged, out var member )
                         && member.IsExplicitInterfaceImplementation )
                    {
                        return InpcInstrumentationKind.InpcPrivateImplementation;
                    }
                    else
                    {
                        return InpcInstrumentationKind.InpcPublicImplementation;
                    }
                }
                else if ( !namedType.BelongsToCurrentProject )
                {
                    return InpcInstrumentationKind.None;
                }
                else
                {
                    if ( this._targetType.Compilation.IsPartial && !this._targetType.Compilation.Types.Contains( type ) )
                    {
                        return InpcInstrumentationKind.Unknown;
                    }

                    return namedType.Definition.Enhancements().HasAspect<ObservableAttribute>()
                        ? InpcInstrumentationKind.Aspect // For now, the aspect always introduces implicit implementation.
                        : InpcInstrumentationKind.None;
                }

            case ITypeParameter typeParameter:
                var hasImplicit = false;

                foreach ( var t in typeParameter.TypeConstraints )
                {
                    var k = this.Get( t );

                    switch ( k )
                    {
                        case InpcInstrumentationKind.Aspect:
                            return InpcInstrumentationKind.Aspect;

                        case InpcInstrumentationKind.InpcPublicImplementation:
                            hasImplicit = true;

                            break;
                    }
                }

                return hasImplicit ? InpcInstrumentationKind.Aspect : InpcInstrumentationKind.None;

            default:
                return InpcInstrumentationKind.None;
        }
    }
}