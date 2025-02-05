// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Utilities;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverrideAccessorAdviceResult<TOwner> : AdviceResult, IOverrideAdviceResult<IMethod>
    where TOwner : class, IMember
{
    private readonly OverrideMemberAdviceResult<TOwner> _owner;
    private readonly Func<TOwner, IMethod?> _getMethod;

    public OverrideAccessorAdviceResult( OverrideMemberAdviceResult<TOwner> owner, Func<TOwner, IMethod?> getMethod )
    {
        this._owner = owner;
        this._getMethod = getMethod;
    }

    [Memo]
    public IMethod Declaration => this._getMethod( this._owner.Declaration ).AssertNotNull();
}