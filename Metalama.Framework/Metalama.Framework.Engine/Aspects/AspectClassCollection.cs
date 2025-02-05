// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects
{
    internal sealed class AspectClassCollection : IReadOnlyCollection<IBoundAspectClass>, IReadOnlyCollection<IAspectClass>, IAspectClassResolver
    {
        public ImmutableDictionary<string, IBoundAspectClass> Dictionary { get; }

        public IBoundAspectClass this[ string typeName ] => this.Dictionary[typeName];

        public AspectClassCollection( IEnumerable<IBoundAspectClass> aspectClasses )
        {
            this.Dictionary = aspectClasses.ToImmutableDictionary( c => c.FullName, c => c );

            this.HashCode = HashUtilities.HashStrings( this.Dictionary.Keys );
        }

        IEnumerator<IAspectClass> IEnumerable<IAspectClass>.GetEnumerator() => this.GetEnumerator();

        public IEnumerator<IBoundAspectClass> GetEnumerator() => this.Dictionary.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this.Dictionary.Count;

        public ulong HashCode { get; }

        public bool TryGetAspectClass( Type aspectType, [NotNullWhen( true )] out IAspectClass? aspectClass )
        {
            if ( this.Dictionary.TryGetValue( aspectType.FullName.AssertNotNull(), out var boundAspectClass ) )
            {
                aspectClass = boundAspectClass;

                return true;
            }
            else
            {
                aspectClass = null;

                return false;
            }
        }
    }
}