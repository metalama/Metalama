// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Collects the dependencies of a given dependent syntax tree in a given compilation.
/// </summary>
internal sealed class DependencyCollectorByDependentSyntaxTreeAndMasterProject
{
    private readonly Dictionary<DocumentKey, ulong> _masterDocumentKeysAndHashes = new();
    private readonly HashSet<TypeDependencyKey> _masterPartialTypes = new();
    private int _hashCode;

    public DocumentKey DependentDocumentKey { get; }

    public IReadOnlyDictionary<DocumentKey, ulong> MasterDocumentKeysAndHashes => this._masterDocumentKeysAndHashes;

    public IReadOnlyCollection<TypeDependencyKey> MasterPartialTypes => this._masterPartialTypes;

    public bool Contains( TypeDependencyKey type ) => this._masterPartialTypes.Contains( type );

    public DependencyCollectorByDependentSyntaxTreeAndMasterProject( DocumentKey dependentDocumentKey )
    {
        this.DependentDocumentKey = dependentDocumentKey;
    }

    public void AddSyntaxTreeDependency( DocumentKey masterDocumentKey, ulong masterHash )
    {
#if DEBUG
        if ( this._isReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif
        lock ( this._masterDocumentKeysAndHashes )
        {
            if ( !this._masterDocumentKeysAndHashes.TryGetValue( masterDocumentKey, out var existingHash ) )
            {
                this._masterDocumentKeysAndHashes.Add( masterDocumentKey, masterHash );
                this._hashCode ^= HashCode.Combine( masterDocumentKey, masterHash );
            }
            else if ( existingHash != masterHash )
            {
                throw new AssertionFailedException( $"Hashes '{existingHash}' and '{masterHash}' do not match for '{masterDocumentKey}'." );
            }
        }
    }

    public void AddPartialTypeDependency( TypeDependencyKey masterPartialType )
    {
#if DEBUG
        if ( this._isReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        lock ( this._masterPartialTypes )
        {
            if ( this._masterPartialTypes.Add( masterPartialType ) )
            {
                this._hashCode ^= masterPartialType.GetHashCode();
            }
        }
    }

#if DEBUG
    private bool _isReadOnly;

    public void Freeze()
    {
        this._isReadOnly = true;
    }
#endif

    public bool IsStructurallyEqual( DependencyCollectorByDependentSyntaxTreeAndMasterProject other )
    {
        if ( ReferenceEquals( this, other ) )
        {
            return true;
        }

        if ( this._hashCode != other._hashCode
             || this._masterDocumentKeysAndHashes.Count != other._masterDocumentKeysAndHashes.Count
             || this._masterPartialTypes.Count != other._masterPartialTypes.Count )
        {
            return false;
        }

        foreach ( var dependency in this._masterDocumentKeysAndHashes )
        {
            if ( !other._masterDocumentKeysAndHashes.TryGetValue( dependency.Key, out var otherHash ) || otherHash != dependency.Value )
            {
                return false;
            }
        }

        foreach ( var dependency in this._masterPartialTypes )
        {
            if ( !other._masterPartialTypes.Contains( dependency ) )
            {
                return false;
            }
        }

        return true;
    }
}