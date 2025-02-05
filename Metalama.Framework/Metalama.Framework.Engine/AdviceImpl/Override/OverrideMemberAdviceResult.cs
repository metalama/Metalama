// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

/// <summary>
/// An <see cref="AdviceResult"/> that does not have any property.
/// </summary>
internal sealed class OverrideMemberAdviceResult<TMember> : AdviceResult, IOverrideAdviceResult<TMember>
    where TMember : class, IMember
{
    private readonly IRef<TMember>? _declaration;

    // Errpr constructor.
    public OverrideMemberAdviceResult() { }

    // Success constructor.
    public OverrideMemberAdviceResult( IRef<TMember>? declaration )
    {
        this._declaration = declaration;
    }

    public TMember Declaration => this.Resolve( this._declaration );

    public OverrideAccessorAdviceResult<TMember> GetAccessor( Func<TMember, IMethod?> getAccessor ) => new( this, getAccessor );
}