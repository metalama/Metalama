// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceNamespaceAdvice : IntroduceDeclarationAdvice<INamespace, NamespaceBuilder>
{
    private readonly string _name;

    private static readonly char[] _nsSplitChars = ['.'];

    public override AdviceKind AdviceKind => AdviceKind.IntroduceNamespace;

    public IntroduceNamespaceAdvice(
        AdviceConstructorParameters<INamespace> parameters,
        string name ) : base( parameters, null )
    {
        this._name = name;
    }

    protected override NamespaceBuilder CreateBuilder()
    {
        var nameParts = this._name.Split( _nsSplitChars );

        var parentNamespace = (INamespace) this.TargetDeclaration;

        for ( var index = 0; index < nameParts.Length - 1; index++ )
        {
            var childNs = parentNamespace.Namespaces.OfName( nameParts[index] )
                          ?? new NamespaceBuilder( this.AspectLayerInstance, parentNamespace, nameParts[index] );

            parentNamespace = childNs;
        }

        return new NamespaceBuilder( this.AspectLayerInstance, parentNamespace, nameParts[^1] );
    }

    protected override IntroductionAdviceResult<INamespace> ImplementCore( NamespaceBuilder builder, in AdviceImplementationContext context )
    {
        var contextCopy = context;
     
        void AddTransformationRecursive( NamespaceBuilder ns )
        {
            if ( ns.ContainingNamespace is NamespaceBuilder parentBuilder )
            {
                // Make sure to the add parent namespace before the child one.
                AddTransformationRecursive( parentBuilder );
            }

            contextCopy.AddTransformation( ns.CreateTransformation() );
        }

        var existingNamespace = builder.ContainingNamespace.TryForCompilation( context.MutableCompilation, out var containingNamespace )
            ? containingNamespace.Namespaces.OfName( builder.Name )
            : null;

        if ( existingNamespace == null )
        {
            // We have a new namespace.
            AddTransformationRecursive( builder );

            return this.CreateSuccessResult( AdviceOutcome.Default, builder );
        }
        else
        {
            return this.CreateSuccessResult( AdviceOutcome.Ignore, existingNamespace );
        }
    }
}