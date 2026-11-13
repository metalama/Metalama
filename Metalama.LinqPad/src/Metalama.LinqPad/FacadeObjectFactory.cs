// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Workspaces.Navigation.Facade;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Metalama.LinqPad
{
    /// <summary>
    /// Gets <see cref="FacadeObject"/> instances for any given input object.
    /// </summary>
    internal sealed class FacadeObjectFactory
    {
#pragma warning disable CA1805 // Do not initialize unnecessarily
        private static readonly WeakCache<object, FacadeObject> _objectFacades = new();
#pragma warning restore CA1805 // Do not initialize unnecessarily

        private readonly FacadeTypeFactory _typeFactory;

        public Func<IDeclaration, GetCompilationInfo> GetGetCompilationInfo { get; }

        public FacadeObjectFactory( Func<IDeclaration, GetCompilationInfo>? workspaceExpression = null, IEnumerable<Assembly>? publicAssemblies = null )
        {
            this.GetGetCompilationInfo = workspaceExpression ?? (_ => new GetCompilationInfo( "workspace", false ));
            this._typeFactory = publicAssemblies == null ? FacadeTypeFactory.Default : new FacadeTypeFactory( publicAssemblies );
        }

        internal FacadeObject? GetFacade( object? instance )
        {
            var isInlineType = instance is null or IEnumerable or string || instance.GetType().IsPrimitive
                                                                         || (instance.GetType().Assembly.FullName is { } fullName
                                                                             && fullName.StartsWith( "LINQPad", StringComparison.OrdinalIgnoreCase ));

            if ( isInlineType )
            {
                return null;
            }
            else
            {
                return _objectFacades.GetOrAdd( instance!, i => new FacadeObject( this, this.GetFormatterType( i.GetType() ), i ) );
            }
        }

        public FacadeType GetFormatterType( Type type ) => this._typeFactory.GetFacadeType( type );
    }
}
