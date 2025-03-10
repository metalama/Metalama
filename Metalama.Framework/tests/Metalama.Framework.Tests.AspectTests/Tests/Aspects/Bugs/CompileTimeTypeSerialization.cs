// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CompileTimeTypeSerialization;

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true )]
[Inheritable]
public sealed class ListImplementedTypesAttribute : TypeAspect
{
    public ListImplementedTypesAttribute( Type type )
    {
        Type = type;
    }

    public Type Type { get; }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var types = new[] { Type }.Concat( builder.AspectInstance.SecondaryInstances.Select( i => ( (ListImplementedTypesAttribute)i.Aspect ).Type ) )
            .OrderBy( t => t.FullName );

        var sb = new StringBuilder();

        foreach (var type in types)
        {
            sb.Append( $"{type}: {( builder.Target.IsConvertibleTo( type ) ? "" : "not " )}implemented; " );
        }

        builder.IntroduceField( "types", typeof(string), buildField: field => field.InitializerExpression = TypedConstant.Create( sb.ToString() ) );
    }
}

// <target>
[ListImplementedTypes( typeof(ICloneable) )]
[ListImplementedTypes( typeof(ISomeInterface) )]
[ListImplementedTypes( typeof(ISomeInterface.INested) )]
[ListImplementedTypes( typeof(ISomeInterface[]) )]
[ListImplementedTypes( typeof(List<ISomeInterface>) )]
[ListImplementedTypes( typeof(ISomeInterface<int>) )]
[ListImplementedTypes( typeof(ISomeInterface<DateTime>) )]
[ListImplementedTypes( typeof(ISomeInterface<Type>) )]
[ListImplementedTypes( typeof(E) )]
public abstract class BaseClass : ISomeInterface { }

public interface ISomeInterface
{
    public interface INested { }
}

public interface ISomeInterface<T> { }

public enum E { }

// <target>
public sealed class TestClass : BaseClass, ICloneable
{
    public object Clone() => throw new NotImplementedException();
}