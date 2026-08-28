// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using System.Linq;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// The source code fixtures and the declaration builders shared by <see cref="RefTests"/>, whose tests run once per
/// kind of durable reference, and by <see cref="SerializableRefResolutionTests"/>, whose tests run once.
/// </summary>
internal static class RefTestFixtures
{
    /// <summary>
    /// Source code that declares one type of each shape that a reference to a named type must support.
    /// </summary>
    public const string GenericTypesCode = """
                                           class Plain { }

                                           class Generic<T>
                                           {
                                               public class Nested { }
                                           }

                                           class Container
                                           {
                                               public Generic<int> ConstructedField = null!;
                                               public Generic<string> OtherConstructedField = null!;
                                           }
                                           """;

    /// <summary>
    /// Returns the type of <see cref="GenericTypesCode"/> denoted by a kind name.
    /// </summary>
    public static INamedType GetTestType( CompilationModel compilation, string kind )
        => kind switch
        {
            "Plain" => compilation.Types.OfName( "Plain" ).Single(),
            "Generic" => compilation.Types.OfName( "Generic" ).Single(),
            "Nested" => compilation.Types.OfName( "Generic" ).Single().Types.OfName( "Nested" ).Single(),
            "Constructed" => (INamedType) compilation.Types.OfName( "Container" ).Single().Fields.OfName( "ConstructedField" ).Single().Type,
            "External" => compilation.Factory.GetTypeByReflectionType( typeof(string) ).AssertCast<INamedType>(),
            _ => throw new AssertionFailedException( $"Unknown kind '{kind}'." )
        };

    /// <summary>
    /// Introduces a namespace into a mutable compilation and returns the resulting declaration.
    /// </summary>
    public static INamespace IntroduceNamespace( CompilationModel compilation, INamespace containingNamespace, string name )
    {
        var namespaceBuilder = new NamespaceBuilder( null!, containingNamespace, name );
        compilation.AddTransformation( namespaceBuilder.CreateTransformation() );

        return containingNamespace.Namespaces.OfName( name ).AssertNotNull();
    }

    /// <summary>
    /// Introduces a type into a mutable compilation and returns the resulting declaration.
    /// </summary>
    public static INamedType IntroduceType( CompilationModel compilation, INamespace containingNamespace, string name )
    {
        var typeBuilder = new NamedTypeBuilder( null!, containingNamespace, name, TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        return containingNamespace.Types.OfName( name ).Single();
    }
}
