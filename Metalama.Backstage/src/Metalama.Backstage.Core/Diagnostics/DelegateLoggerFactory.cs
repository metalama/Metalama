// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Immutable;

namespace Metalama.Backstage.Diagnostics;

internal sealed class DelegateLoggerFactory : SimpleLoggerFactory
{
    private readonly Action<string> _traceAction;

    public DelegateLoggerFactory( string productName, Action<string> traceAction, ImmutableHashSet<string> traceCategories, bool includeThreadId = true ) : base(
        productName,
        traceCategories,
        includeThreadId )
    {
        this._traceAction = traceAction;
    }

    public override void Flush() { }

    protected override void Write( string message ) => this._traceAction.Invoke( message );
}