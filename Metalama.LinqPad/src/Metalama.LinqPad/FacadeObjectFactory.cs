// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Introspection;
using Metalama.Framework.Workspaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        private static readonly ConcurrentDictionary<Type, FacadeType> _types = new();

        public ImmutableHashSet<Assembly> PublicAssemblies { get; }

        public Func<IDeclaration, GetCompilationInfo> GetGetCompilationInfo { get; }

        public FacadeObjectFactory( Func<IDeclaration, GetCompilationInfo>? workspaceExpression = null, IEnumerable<Assembly>? publicAssemblies = null )
        {
            this.GetGetCompilationInfo = workspaceExpression ?? (_ => new GetCompilationInfo( "workspace", false ));
            publicAssemblies ??= [];

            this.PublicAssemblies = new[]
                {
                    typeof(IDeclaration).Assembly, typeof(IIntrospectionAdvice).Assembly, typeof(ICompilationSet).Assembly, typeof(Type).Assembly
                }
                .Concat( publicAssemblies )
                .ToImmutableHashSet();
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
                return _objectFacades.GetOrAdd( instance!, i => new FacadeObject( this.GetFormatterType( i.GetType() ), i ) );
            }
        }

        public FacadeType GetFormatterType( Type type ) => _types.GetOrAdd( type, t => new FacadeType( this, t ) );
    }
}