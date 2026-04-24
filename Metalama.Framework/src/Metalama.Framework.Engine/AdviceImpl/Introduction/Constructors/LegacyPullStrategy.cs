// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed class LegacyPullStrategy : IPullStrategy
{
    private readonly Func<IParameter, IConstructor, PullAction> _func;

    private LegacyPullStrategy( Func<IParameter, IConstructor, PullAction> func )
    {
        this._func = func;
    }

    public static IPullStrategy? Create( Func<IParameter, IConstructor, PullAction>? func ) => func == null ? null : new LegacyPullStrategy( func );

    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember ) => this._func( pulledParameter, (IConstructor) targetMember );
}