// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects;

public sealed partial class InheritableAspectInstance
{
    // ReSharper disable once UnusedType.Local
    private class Serializer : ReferenceTypeSerializer
    {
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments ) => new InheritableAspectInstance();

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            var instance = (InheritableAspectInstance) obj;
            initializationArguments.SetValue( nameof(instance.TargetDeclaration), instance.TargetDeclaration );
            initializationArguments.SetValue( nameof(instance.Aspect), instance.Aspect );
            initializationArguments.SetValue( nameof(instance.SecondaryInstances), instance.SecondaryInstances );

            initializationArguments.SetValue( nameof(instance.AspectState), instance.AspectState );
        }

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments )
        {
            var instance = (InheritableAspectInstance) obj;
            instance.TargetDeclaration = initializationArguments.GetValue<IRef<IDeclaration>>( nameof(instance.TargetDeclaration) )!;
            instance.Aspect = initializationArguments.GetValue<IAspect>( nameof(instance.Aspect) )!;
            instance.SecondaryInstances = initializationArguments.GetValue<ImmutableArray<IAspectInstance>>( nameof(instance.SecondaryInstances) );
            instance.AspectState = initializationArguments.GetValue<IAspectState>( nameof(instance.AspectState) );
        }
    }
}