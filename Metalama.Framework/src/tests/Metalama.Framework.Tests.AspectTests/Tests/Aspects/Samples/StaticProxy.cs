// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples.StaticProxy;

public class ProxyAspect : TypeAspect
{
    private readonly Type _interfaceType;

    [Introduce]
    private IInterceptor _interceptor;

    public ProxyAspect( Type interfaceType )
    {
        _interfaceType = interfaceType;
    }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        // Add a field with the intercepted object.
        var interceptedField = builder.IntroduceField(
                "_intercepted",
                _interfaceType,
                IntroductionScope.Instance )
            .Declaration;

        // Implement the interface.
        var implementInterfaceResult = builder.ImplementInterface( _interfaceType );

        // Implement interface members.
        var namedType = (INamedType)TypeFactory.GetType( _interfaceType );

        foreach (var method in namedType.Methods)
        {
            implementInterfaceResult.ExplicitMembers.IntroduceMethod(
                method.ReturnType.IsConvertibleTo( SpecialType.Void )
                    ? nameof(VoidTemplate)
                    : nameof(NonVoidTemplate),
                IntroductionScope.Instance,
                buildMethod: methodBuilder =>
                {
                    methodBuilder.Name = method.Name;
                    methodBuilder.ReturnType = method.ReturnType;

                    foreach (var parameter in method.Parameters)
                    {
                        methodBuilder.AddParameter(
                            parameter.Name,
                            parameter.Type,
                            parameter.RefKind );
                    }
                },
                args:
                /*method.ReturnType.IsConvertibleTo( SpecialType.Void ) ? new { method, interceptedField } :*/
                new { T = method.ReturnType, method, interceptedField } );
        }

        // Add the constructor.
        builder.IntroduceConstructor(
            nameof(Constructor),
            buildConstructor: constructorBuilder
                => constructorBuilder.AddParameter( "intercepted", _interfaceType ),
            args: new { interceptedField } );
    }

    [Template]
    private T NonVoidTemplate<[CompileTime] T>( IMethod method, IField interceptedField )
    {
        return _interceptor.Invoke( () => (T)method.With( interceptedField ).Invoke( method.Parameters )! );
    }

    [Template]
    private void VoidTemplate( IMethod method, IField interceptedField )
    {
        _interceptor.Invoke( () => { method.With( interceptedField ).Invoke( method.Parameters ); } );
    }

    [Template]
    public void Constructor( IInterceptor interceptor, IField interceptedField )
    {
        _interceptor = interceptor;
        interceptedField.Value = meta.Target.Parameters["intercepted"].Value;
    }
}

public interface IPropertyStore
{
    object Get( string name );

    void Store( string name, object value );
}

public interface IInterceptor
{
    public T Invoke<T>( Func<T> next );

    public void Invoke( Action next );
}

// <target>
[ProxyAspect( typeof(IPropertyStore) )]
public class PropertyStoreProxy { }