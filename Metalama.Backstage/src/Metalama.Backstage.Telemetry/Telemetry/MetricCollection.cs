// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.ObjectModel;

namespace Metalama.Backstage.Telemetry
{
    /// <summary>
    /// Collection of metrics (<see cref="Metric"/>).
    /// </summary>
    [Serializable]
    public sealed class MetricCollection : KeyedCollection<string, Metric>
    {
        public MetricCollection( bool isReadOnly = false )
        {
            this.IsReadOnly = isReadOnly;
        }

        public static MetricCollection EmptyReadOnly { get; } = new( true );

        public bool IsReadOnly { get; private set; }

        public void Freeze() => this.IsReadOnly = true;

        /// <inheritdoc />
        protected override string GetKeyForItem( Metric item )
        {
            return item.Name;
        }

        private void CheckNotReadOnly()
        {
            if ( this.IsReadOnly )
            {
                throw new InvalidOperationException();
            }
        }

        protected override void ClearItems()
        {
            this.CheckNotReadOnly();
            base.ClearItems();
        }

        protected override void InsertItem( int index, Metric item )
        {
            this.CheckNotReadOnly();
            base.InsertItem( index, item );
        }

        protected override void SetItem( int index, Metric item )
        {
            this.CheckNotReadOnly();
            base.SetItem( index, item );
        }

        protected override void RemoveItem( int index )
        {
            this.CheckNotReadOnly();
            base.RemoveItem( index );
        }
    }
}