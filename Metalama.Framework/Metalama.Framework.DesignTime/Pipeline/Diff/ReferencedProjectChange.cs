// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

/// <summary>
/// Represents a change in a referenced project.
/// </summary>
internal readonly struct ReferencedProjectChange : IEquatable<ReferencedProjectChange>
{
    private readonly WeakReference<Compilation>? _oldCompilationRef;

    public ReferenceChangeKind ChangeKind { get; }

    public Compilation? NewCompilation { get; }

    public Compilation? OldCompilationDangerous
    {
        get
        {
            if ( this._oldCompilationRef == null )
            {
                return null;
            }
            else if ( this._oldCompilationRef.TryGetTarget( out var oldCompilation ) )
            {
                return oldCompilation;
            }
            else
            {
                throw new InvalidOperationException( "The old compilation is no longer alive." );
            }
        }
    }

    public bool HasCompileTimeCodeChange => this.ChangeKind != ReferenceChangeKind.Modified || this.Changes.AssertNotNull().HasCompileTimeCodeChange;

    /// <summary>
    /// Gets the changes in the referenced compilation, but only if <see cref="ChangeKind"/> is <see cref="ReferenceChangeKind.Modified"/>.
    /// Specifically, the property is not set when <see cref="ChangeKind"/> is <see cref="ReferenceChangeKind.Added"/>.
    /// </summary>
    public CompilationChanges? Changes { get; }

    public ReferencedProjectChange(
        Compilation? oldCompilation,
        Compilation? newCompilation,
        ReferenceChangeKind changeKind,
        CompilationChanges? changes = null )
    {
        Invariant.Assert( changes == null || oldCompilation == changes.OldProjectVersionDangerous?.Compilation );
        Invariant.Assert( changes == null || newCompilation == changes.NewProjectVersion.Compilation );

        this._oldCompilationRef = oldCompilation == null ? null : new WeakReference<Compilation>( oldCompilation );
        this.NewCompilation = newCompilation;
        this.ChangeKind = changeKind;
        this.Changes = changes;
    }

    public bool Equals( ReferencedProjectChange other )
        => ReferenceEquals( this._oldCompilationRef, other._oldCompilationRef )
           && this.ChangeKind == other.ChangeKind
           && ReferenceEquals( this.NewCompilation, other.NewCompilation )
           && ReferenceEquals( this.Changes, other.Changes );

    public override bool Equals( object? obj ) => obj is ReferencedProjectChange other && this.Equals( other );

    public override int GetHashCode() => HashCode.Combine( this._oldCompilationRef, (int) this.ChangeKind, this.NewCompilation, this.Changes );
}