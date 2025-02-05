// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using K4os.Hash.xxHash;
using Metalama.Framework.DesignTime.Pipeline.Dependencies;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

/// <summary>
/// Implementation of the code diff algorithm.
/// </summary>
internal sealed class DiffStrategy
{
    private readonly bool _isTest;
    private readonly bool _detectCompileTimeCode;
    private readonly bool _detectPartialTypes;

    public IDifferObserver? Observer { get; }

    public DiffStrategy( bool isTest, bool detectCompileTimeCode, bool detectPartialTypes, IDifferObserver? observer = null )
    {
        this._detectCompileTimeCode = detectCompileTimeCode;
        this._detectPartialTypes = detectPartialTypes;
        this._isTest = isTest;
        this.Observer = observer;
    }

    public static CompileTimeChangeKind GetCompileTimeChangeKind( bool oldValue, bool newValue )
        => (oldValue, newValue) switch
        {
            (true, true) => CompileTimeChangeKind.None,
            (false, false) => CompileTimeChangeKind.None,
            (true, false) => CompileTimeChangeKind.NoLongerCompileTime,
            (false, true) => CompileTimeChangeKind.NewlyCompileTime
        };

    /// <summary>
    /// Determines whether two syntax trees are significantly different. This overload is called from tests.
    /// </summary>
    public bool IsDifferent( SyntaxTree oldSyntaxTree, SyntaxTree newSyntaxTree )
    {
        if ( this._detectPartialTypes )
        {
            throw new InvalidOperationException();
        }

        var oldSyntaxTreeVersion = this.GetSyntaxTreeVersion( oldSyntaxTree, null );

        return this.IsDifferent( oldSyntaxTreeVersion, newSyntaxTree, null, out _ );
    }

    public bool IsDifferent(
        in SyntaxTreeVersion oldSyntaxTreeVersion,
        SyntaxTree newSyntaxTree,
        Compilation? newCompilation,
        out SyntaxTreeVersion newSyntaxTreeVersion )
    {
        // Check if the source text has changed.
        if ( newSyntaxTree == oldSyntaxTreeVersion.SyntaxTree )
        {
            newSyntaxTreeVersion = oldSyntaxTreeVersion;

            return false;
        }
        else
        {
            var newSyntaxRoot = newSyntaxTree.GetRoot();
            var newHasCompileTimeCode = this._detectCompileTimeCode && CompileTimeCodeFastDetector.HasCompileTimeCode( newSyntaxRoot );
            var hhx64 = new XXH64();
            BaseCodeHasher hasher = newHasCompileTimeCode ? new CompileTimeCodeHasher( hhx64 ) : new RunTimeCodeHasher( hhx64 );
            hasher.Visit( newSyntaxRoot );
            var newSyntaxTreeHash = hhx64.Digest();

            if ( newSyntaxTreeHash == oldSyntaxTreeVersion.DeclarationHash )
            {
                newSyntaxTreeVersion = oldSyntaxTreeVersion;

                return false;
            }
            else
            {
                var (partialTypes, partialTypesHash) = this.FindPartialTypes( oldSyntaxTreeVersion, newSyntaxTree, newCompilation );

                newSyntaxTreeVersion = new SyntaxTreeVersion( newSyntaxTree, newHasCompileTimeCode, newSyntaxTreeHash, partialTypes, partialTypesHash );

                return true;
            }
        }
    }

    private (ImmutableArray<TypeDependencyKey> PartialTypes, int PartialTypesHash) FindPartialTypes(
        in SyntaxTreeVersion oldVersion,
        SyntaxTree syntaxTree,
        Compilation? compilation )
    {
        if ( !this._detectPartialTypes )
        {
            return default;
        }
        else if ( compilation == null )
        {
            throw new ArgumentNullException( nameof(compilation) );
        }

        var syntaxRoot = syntaxTree.GetRoot();
        var partialTypesHash = PartialTypesHasher.Instance.Visit( syntaxRoot );

        ImmutableArray<TypeDependencyKey> partialTypeKeys;

        if ( partialTypesHash.HasValue )
        {
            // See if we can reuse the old TypeDependencyKey without going to the semantic model. This should be the case
            // most of the time when the user is editing the body of the type.
            if ( !oldVersion.IsDefault && oldVersion.PartialTypesHash == partialTypesHash.Value )
            {
                partialTypeKeys = oldVersion.PartialTypes;
            }
            else
            {
                // We need to get the symbol of partial types from the semantic model.

                // Get the syntax nodes declaring the partial types.
                var partialTypes = PartialTypesVisitor.Instance.Visit( syntaxRoot );

                // Map these nodes to a symbol and get the TypeDependencyKey.
                var semanticModel = compilation.GetCachedSemanticModel( syntaxTree );
                var partialTypeKeysBuilder = ImmutableArray.CreateBuilder<TypeDependencyKey>( partialTypes.Length );

                foreach ( var partialType in partialTypes )
                {
                    var symbol = (INamedTypeSymbol) semanticModel.GetDeclaredSymbol( partialType ).AssertNotNull();
                    partialTypeKeysBuilder.Add( new TypeDependencyKey( symbol, this._isTest ) );
                }

                partialTypeKeys = partialTypeKeysBuilder.MoveToImmutable();
            }
        }
        else
        {
            partialTypeKeys = ImmutableArray<TypeDependencyKey>.Empty;
        }

        return (partialTypeKeys, partialTypesHash ?? 0);
    }

    public SyntaxTreeVersion GetSyntaxTreeVersion( SyntaxTree syntaxTree, Compilation? compilation )
    {
        var syntaxRoot = syntaxTree.GetRoot();
        var hasCompileTimeCode = this._detectCompileTimeCode && CompileTimeCodeFastDetector.HasCompileTimeCode( syntaxRoot );
        var hhx64 = new XXH64();
        BaseCodeHasher hasher = hasCompileTimeCode ? new CompileTimeCodeHasher( hhx64 ) : new RunTimeCodeHasher( hhx64 );
        hasher.Visit( syntaxRoot );
        var declarationHash = hhx64.Digest();

        var (partialTypes, partialTypesHash) = this.FindPartialTypes( default, syntaxTree, compilation );

        return new SyntaxTreeVersion( syntaxTree, hasCompileTimeCode, declarationHash, partialTypes, partialTypesHash );
    }
}