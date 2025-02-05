// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Collects the dependencies of a given dependent syntax tree in a given compilation.
/// </summary>
internal sealed class DependencyCollectorByDependentSyntaxTreeAndMasterProject
{
    private readonly Dictionary<string, ulong> _masterFilePathsAndHashes = new( StringComparer.Ordinal );
    private readonly HashSet<TypeDependencyKey> _masterPartialTypes = new();
    private int _hashCode;

    public string DependentFilePath { get; }

    public IReadOnlyDictionary<string, ulong> MasterFilePathsAndHashes => this._masterFilePathsAndHashes;

    public IReadOnlyCollection<TypeDependencyKey> MasterPartialTypes => this._masterPartialTypes;

    public bool Contains( TypeDependencyKey type ) => this._masterPartialTypes.Contains( type );

    public DependencyCollectorByDependentSyntaxTreeAndMasterProject( string dependentFilePath )
    {
        this.DependentFilePath = dependentFilePath;
    }

    public void AddSyntaxTreeDependency( string masterFilePath, ulong masterHash )
    {
#if DEBUG
        if ( this._isReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif
        lock ( this._masterFilePathsAndHashes )
        {
            if ( !this._masterFilePathsAndHashes.TryGetValue( masterFilePath, out var existingHash ) )
            {
                this._masterFilePathsAndHashes.Add( masterFilePath, masterHash );
                this._hashCode ^= HashCode.Combine( masterFilePath, masterHash );
            }
            else if ( existingHash != masterHash )
            {
                throw new AssertionFailedException( $"Hashes '{existingHash}' and '{masterHash}' do not match for '{masterFilePath}'." );
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
             || this._masterFilePathsAndHashes.Count != other._masterFilePathsAndHashes.Count
             || this._masterPartialTypes.Count != other._masterPartialTypes.Count )
        {
            return false;
        }

        foreach ( var dependency in this._masterFilePathsAndHashes )
        {
            if ( !other._masterFilePathsAndHashes.TryGetValue( dependency.Key, out var otherHash ) || otherHash != dependency.Value )
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