// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed partial class TemplateCompilerRewriter
    {
        /// <summary>
        /// The return value of <see cref="TemplateCompilerRewriter.WithMetaContext"/>.
        /// </summary>
        private readonly struct MetaContextCookie : IDisposable
        {
            private readonly TemplateCompilerRewriter _parent;
            private readonly MetaContext? _previousMetaContext;

            public MetaContextCookie( TemplateCompilerRewriter parent, MetaContext? previousMetaContext )
            {
                this._parent = parent;
                this._previousMetaContext = previousMetaContext;
            }

            public void Dispose()
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                //    Would be true in the default instance.

                if ( this._parent != null )
                {
                    this._parent._currentMetaContext = this._previousMetaContext;
                }
            }
        }
    }
}