// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Options;
using System;

namespace Metalama.Framework.Engine.HierarchicalOptions;

public sealed partial class HierarchicalOptionsManager
{
    private sealed class DeclarationNode
    {
        private readonly ConcurrentLinkedList<DeclarationNode> _children = new();

        public bool HasCachedMergedOptions { get; private set; }

        public bool HasCachedMergedOptionsExcludingNamespace { get; private set; }

        private IHierarchicalOptions? _cachedMergedOptions;
        private IHierarchicalOptions? _cachedMergedOptionsExcludingNamespace;
        private IHierarchicalOptions? _directOptions;

        public object Sync { get; } = new();

        public IHierarchicalOptions? DirectOptions
        {
            get => this._directOptions;
            set
            {
                this._directOptions = value;
                this.ResetMergedOptions();
            }
        }

        public IHierarchicalOptions? CachedMergedOptions
        {
            get
            {
                if ( !this.HasCachedMergedOptions )
                {
                    throw new InvalidOperationException();
                }

                return this._cachedMergedOptions;
            }
            set
            {
                this._cachedMergedOptions = value;
                this.HasCachedMergedOptions = true;
            }
        }

        public IHierarchicalOptions? CachedMergedOptionsExcludingNamespace
        {
            get
            {
                if ( !this.HasCachedMergedOptionsExcludingNamespace )
                {
                    throw new InvalidOperationException();
                }

                return this._cachedMergedOptionsExcludingNamespace;
            }
            set
            {
                this._cachedMergedOptionsExcludingNamespace = value;
                this.HasCachedMergedOptionsExcludingNamespace = true;
            }
        }

        private void ResetMergedOptions()
        {
            this.CachedMergedOptions = null;
            this.HasCachedMergedOptions = false;

            foreach ( var child in this._children )
            {
                child.ResetMergedOptions();
            }
        }

        public void AddChildNode( DeclarationNode node )
        {
            this._children.Add( node );
        }
    }
}