// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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