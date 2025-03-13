// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed partial class TemplateAnnotator
    {
        private readonly struct ScopeContextCookie : IDisposable
        {
            private readonly TemplateAnnotator? _parent;
            private readonly ScopeContext _initialValue;

            public ScopeContextCookie( TemplateAnnotator parent, ScopeContext initialValue )
            {
                this._parent = parent;
                this._initialValue = initialValue;
            }

            public void Dispose()
            {
                if ( this._parent != null )
                {
                    this._parent._currentScopeContext = this._initialValue;
                }
            }
        }
    }
}