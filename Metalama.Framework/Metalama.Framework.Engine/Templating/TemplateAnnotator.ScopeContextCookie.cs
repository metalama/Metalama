// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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