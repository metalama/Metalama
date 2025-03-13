// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.TestHelpers
{
    [Serializable]
    public class CachedValueClass
    {
        private int? _id;

        public int Id
        {
            get => this._id!.Value;

            set
            {
                if ( this._id.HasValue )
                {
                    throw new InvalidOperationException( "The id can (and has to be) set exactly once." );
                }

                this._id = value;
            }
        }

        public CachedValueClass() { }

        public CachedValueClass( int id )
        {
            this._id = id;
        }

        public override int GetHashCode() => this.Id.GetHashCode();

        public override bool Equals( object? obj )
        {
            var value = obj as CachedValueClass;

            return value != null && this.Equals( value );
        }

        public bool Equals( CachedValueClass? other ) => other != null && this.Id.Equals( other.Id );

        public override string ToString() => $"Value #{this.Id}";
    }
}