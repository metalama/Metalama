// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Serialization;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class SerializableTransitiveAspectInstance : ICompileTimeSerializable, ITransitiveAspectsManifestExtension
{
    public IAspect Aspect { get; }

    public IAspectState? AspectState { get; }

    public IRef<IDeclaration> TargetDeclaration { get; }

    public int TargetDeclarationDepth { get; }

    public string AspectClassName { get; }

    public SerializableTransitiveAspectInstance( TransitiveAspectInstance transitiveAspectInstance ) : this(
        transitiveAspectInstance.Aspect,
        transitiveAspectInstance.AspectClass.FullName,
        transitiveAspectInstance.AspectState,
        transitiveAspectInstance.TargetDeclaration,
        transitiveAspectInstance.TargetDeclarationDepth ) { }

    internal SerializableTransitiveAspectInstance(
        IAspect aspect,
        string aspectClassName,
        IAspectState? aspectState,
        IRef<IDeclaration> targetDeclaration,
        int targetDeclarationDepth )
    {
        this.Aspect = aspect;
        this.AspectClassName = aspectClassName;
        this.AspectState = aspectState;
        this.TargetDeclaration = targetDeclaration;
        this.TargetDeclarationDepth = targetDeclarationDepth;
    }

    public AspectInstance? ToAspectInstance( IAspectClassResolver aspectClassResolver )
    {
        if ( !aspectClassResolver.TryGetAspectClass( this.AspectClassName, out var aspectClass ) )
        {
            // The aspect class may not be found when the referenced assembly was compiled with a different
            // version of Metalama that had a different set of aspect classes.
            return null;
        }

        return new AspectInstance(
            this.Aspect,
            this.TargetDeclaration,
            this.TargetDeclarationDepth,
            (IAspectClassImpl) aspectClass,
            [],
            ImmutableArray<AspectPredecessor>.Empty,
            false );
    }

    [UsedImplicitly]
    private class Serializer : ReferenceTypeSerializer<SerializableTransitiveAspectInstance>
    {
        public override SerializableTransitiveAspectInstance CreateInstance( IArgumentsReader constructorArguments )
        {
#pragma warning disable SA1101
            var aspect = constructorArguments.GetValue<IAspect>( nameof(Aspect) ).AssertNotNull();
            var aspectClassName = constructorArguments.GetValue<string>( nameof(AspectClassName) ).AssertNotNull();
            var aspectState = constructorArguments.GetValue<IAspectState>( nameof(AspectState) );
            var targetDeclaration = constructorArguments.GetValue<IRef<IDeclaration>>( nameof(TargetDeclaration) ).AssertNotNull();
            var targetDeclarationDepth = constructorArguments.GetValue<int>( nameof(TargetDeclarationDepth) );
#pragma warning restore SA1101

            return new SerializableTransitiveAspectInstance( aspect, aspectClassName, aspectState, targetDeclaration, targetDeclarationDepth );
        }

        public override void SerializeObject(
            SerializableTransitiveAspectInstance obj,
            IArgumentsWriter constructorArguments,
            IArgumentsWriter initializationArguments )
        {
            constructorArguments.SetValue( nameof(obj.Aspect), obj.Aspect );
            constructorArguments.SetValue( nameof(obj.AspectClassName), obj.AspectClassName );
            constructorArguments.SetValue( nameof(obj.AspectState), obj.AspectState );
            constructorArguments.SetValue( nameof(obj.TargetDeclaration), obj.TargetDeclaration );
            constructorArguments.SetValue( nameof(obj.TargetDeclarationDepth), obj.TargetDeclarationDepth );
        }
    }

    public ContributorKind ContributorKind => ContributorKind.SerializableTransitiveAspectInstance;
}