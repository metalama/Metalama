// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Represents a unique project in a solution. The implementation is optimized to be cheaply computed from a Compilation,
/// because a Compilation does not hold a reference to its project.
/// </summary>
public sealed class ProjectKey : IEquatable<ProjectKey>
{
    // We compare equality of two projects that have the same assembly name by hashing their preprocessor 
    // symbols. There are typically very few compilations of the same assembly name in a solution (one for each different platform)
    // so the chance of collision is negligible.

    // ReSharper disable once MemberCanBePrivate.Global (Json)
    public ulong PreprocessorSymbolHashCode { get; }

    public string AssemblyName { get; }

    public bool IsMetalamaEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="ProjectKey"/> contains a valid hash code. The value can be <c>false</c> in tests
    /// or at design time when the project has no syntax tree.
    /// </summary>
    [JsonIgnore]
    public bool HasHashCode => this.PreprocessorSymbolHashCode != 0;

    [JsonConstructor]
    public ProjectKey( string assemblyName, ulong preprocessorSymbolHashCode, bool isMetalamaEnabled )
    {
        this.AssemblyName = assemblyName;
        this.PreprocessorSymbolHashCode = preprocessorSymbolHashCode;
        this.IsMetalamaEnabled = isMetalamaEnabled;
    }

    public bool Equals( ProjectKey? other )
    {
        if ( ReferenceEquals( null, other ) )
        {
            return false;
        }

        if ( ReferenceEquals( this, other ) )
        {
            return true;
        }

        if ( this.AssemblyName != other.AssemblyName )
        {
            return false;
        }

        if ( this.PreprocessorSymbolHashCode != other.PreprocessorSymbolHashCode )
        {
            return false;
        }

        return true;
    }

    public override bool Equals( object? obj )
    {
        if ( ReferenceEquals( null, obj ) )
        {
            return false;
        }

        if ( ReferenceEquals( this, obj ) )
        {
            return true;
        }

        if ( obj.GetType() != this.GetType() )
        {
            return false;
        }

        return this.Equals( (ProjectKey) obj );
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (this.AssemblyName.GetHashCode() * 47) + (int) this.PreprocessorSymbolHashCode;
        }
    }

    public static bool operator ==( ProjectKey? left, ProjectKey? right ) => Equals( left, right );

    public static bool operator !=( ProjectKey? left, ProjectKey? right ) => !Equals( left, right );

    public override string ToString() => $"{this.AssemblyName}, {this.PreprocessorSymbolHashCode:x}";
}