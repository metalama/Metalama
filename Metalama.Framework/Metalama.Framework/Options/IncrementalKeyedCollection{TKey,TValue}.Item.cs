// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Options;

public partial class IncrementalKeyedCollection<TKey, TValue>
{
    protected internal readonly struct Item : ICompileTimeSerializable, IEquatable<Item>
    {
        public TValue? Value { get; }

        public bool IsEnabled { get; }

        public Item( TValue? value, bool isEnabled = true )
        {
            this.Value = value;
            this.IsEnabled = isEnabled;
        }

#pragma warning disable SA1101

        // ReSharper disable once MemberHidesStaticFromOuterClass
        [UsedImplicitly]
        private sealed class Serializer : ValueTypeSerializer<Item>
        {
            public override void SerializeObject( Item obj, IArgumentsWriter constructorArguments )
            {
                constructorArguments.SetValue( nameof(Value), obj.Value );
                constructorArguments.SetValue( nameof(IsEnabled), obj.IsEnabled );
            }

            public override Item DeserializeObject( IArgumentsReader constructorArguments )
            {
                return new Item(
                    constructorArguments.GetValue<TValue>( nameof(Value) ),
                    constructorArguments.GetValue<bool>( nameof(IsEnabled) ) );
            }
        }

        public bool Equals( Item other ) => EqualityComparer<TValue?>.Default.Equals( this.Value, other.Value ) && this.IsEnabled == other.IsEnabled;

        public override bool Equals( object? obj ) => obj is Item other && this.Equals( other );

        public override int GetHashCode() => HashCode.Combine( this.Value, this.IsEnabled );
    }
}