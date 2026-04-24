// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER && NET7_0_OR_GREATER

// We don't run these tests with old frameworks because they require the type CompilerFeatureRequiredAttribute.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Source;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Accessibility = Microsoft.CodeAnalysis.Accessibility;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed partial class CodeModelTests
{
    [Fact]
    public void UnaryCompoundOperator()
    {
        const string code = """
                            class C1
                            {
                                public int Value;

                                public void operator ++()
                                {
                                    Value++;
                                }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var @operator = compilation.Types.Single().Methods.Single();
        Assert.Equal( MethodKind.Operator, @operator.MethodKind );
        Assert.Equal( OperatorKind.IncrementAssignment, @operator.OperatorKind );
    }

    [Fact]
    public void BinaryCompoundOperator()
    {
        const string code = """
                            class C1
                            {
                                public int Value;

                               public void operator +=(int x)
                               {
                                   Value+=x;
                               }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var @operator = compilation.Types.Single().Methods.Single();
        Assert.Equal( MethodKind.Operator, @operator.MethodKind );
        Assert.Equal( OperatorKind.AdditionAssignment, @operator.OperatorKind );
    }

    [Fact]
    public void ExtensionMembers()
    {
        const string code = """
                            using System;
                            using System.Collections.Generic;
                            using System.Linq;
                            using System.Numerics;

                            public static class MyExtensions
                            {
                                extension(IEnumerable<int> source)
                                {
                                    public IEnumerable<int> ValuesGreaterThan(int threshold)
                                        => source.Where(x => x > threshold);
                                }

                                // Same signature as above, to check if they are grouped (they are not).
                                extension(IEnumerable<int> source)
                                {
                                    public IEnumerable<int> ValuesGreaterThanZero
                                        => source.ValuesGreaterThan(0);
                                }
                                
                                extension(IEnumerable<string> source)
                                {
                                  public IEnumerable<string> ValuesDifferentTo(string threshold)
                                      => source.Where(x => x != threshold);

                                  public IEnumerable<string> ValuesNonNull
                                      => source.ValuesDifferentTo(null);
                                }
                                
                                // Static members only, type parameters.
                                extension<TElement>(IEnumerable<TElement>) where TElement : INumber<TElement>
                               {
                                   public static IEnumerable<TElement> operator *(IEnumerable<TElement> vector, TElement scalar) => throw new NotImplementedException();
                                   public static IEnumerable<TElement> operator *(TElement scalar, IEnumerable<TElement> vector) => throw new NotImplementedException();
                               }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var type = compilation.Types.Single();

        Assert.Empty( type.Types );
        Assert.All( type.Methods, m => Assert.True( m.IsImplicitlyDeclared ) );
        Assert.Equal( 4, type.ExtensionBlocks.Count );

        var extension1 = type.ExtensionBlocks.OfReceivingType( typeof(IEnumerable<int>) ).OrderBy( x => x.Sources[0].Span.Start ).First();
        Assert.Equal( TypeKind.Extension, extension1.TypeKind );
        Assert.Null( extension1.ReceiverParameter.DeclaringMember );

        // Methods.
        var method = Assert.Single( extension1.Methods );
        Assert.Same( extension1, method.DeclaringType );

        // Depth.
        Assert.Equal( type.Depth + 1, extension1.Depth );
        Assert.Equal( extension1.Depth + 1, method.Depth );

        // Refs.
        var reference = extension1.ToRef();
        var roundloop = reference.GetTarget( compilation );
        Assert.Same( extension1, roundloop );

        // Nameless parameter.
        var extension4 = type.ExtensionBlocks.OrderBy( x => x.Sources[0].Span.Start ).ElementAt( 3 );
        Assert.Empty( extension4.ReceiverParameter.Name );

        // Type parameters.
        Assert.Single( extension4.TypeParameters );
    }

    [Fact]
    public void ExtensionBlockAccessibility()
    {
        const string code = """
                            public static class MyExtensions
                            {
                                extension(int source)
                                {
                                    public int Double => source * 2;
                                }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var type = compilation.Types.Single();
        var extensionBlock = type.ExtensionBlocks.Single();

        // Check what Roslyn reports via the code model.
        var codeModelAccessibility = extensionBlock.Accessibility;

        // Also check the underlying Roslyn symbol directly.
        var sourceExtensionBlock = (ExtensionBlock) extensionBlock;
        var roslynAccessibility = sourceExtensionBlock.Symbol.DeclaredAccessibility;

        // Roslyn reports Public accessibility for extension blocks.
        Assert.Equal( Accessibility.Public, roslynAccessibility );

        // The code model maps this to Public.
        Assert.Equal( Code.Accessibility.Public, codeModelAccessibility );
    }

    [Fact]
    public void ExtensionMemberAttributes_MirroredToImplementation()
    {
        // This test examines how Roslyn mirrors custom attributes from extension member declarations
        // to the implicit implementation methods on the parent type.
        const string code = """
                            using System;
                            using System.Runtime.CompilerServices;

                            [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
                            public class MyMethodAttribute : Attribute
                            {
                                public MyMethodAttribute() { }
                                public MyMethodAttribute(string name) { Name = name; }
                                public string Name { get; set; }
                            }

                            [AttributeUsage(AttributeTargets.All)]
                            public class MyParamAttribute : Attribute { }

                            [AttributeUsage(AttributeTargets.All)]
                            public class MyReturnAttribute : Attribute { }

                            public static class MyExtensions
                            {
                                extension(string source)
                                {
                                    // Method with attributes on method, return, and parameter.
                                    [MyMethod("OnMethod")]
                                    [return: MyReturn]
                                    public int MethodWithAttributes([MyParam] int x) => source.Length + x;

                                    // Property with attribute on getter.
                                    public int PropertyWithAttributes
                                    {
                                        [MyMethod("OnGetter")]
                                        get => source.Length;
                                    }
                                }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var type = compilation.Types.OfName( "MyExtensions" ).Single();

        // Get the extension block.
        var extensionBlock = type.ExtensionBlocks.Single();

        // Get the extension method and property.
        var extensionMethod = extensionBlock.Methods.Single();
        var extensionProperty = extensionBlock.Properties.Single();
        var extensionGetter = extensionProperty.GetMethod!;

        // Check attributes on the extension method itself.
        var extensionMethodAttrs = extensionMethod.Attributes.ToList();
        var extensionMethodMyMethodAttr = extensionMethodAttrs.FirstOrDefault( a => a.Type.Name == "MyMethodAttribute" );

        // Check attributes on the extension method's return parameter.
        var extensionMethodReturnAttrs = extensionMethod.ReturnParameter.Attributes.ToList();
        var extensionMethodReturnMyReturnAttr = extensionMethodReturnAttrs.FirstOrDefault( a => a.Type.Name == "MyReturnAttribute" );

        // Check attributes on the extension method's parameter.
        var extensionMethodParamAttrs = extensionMethod.Parameters[0].Attributes.ToList();
        var extensionMethodParamMyParamAttr = extensionMethodParamAttrs.FirstOrDefault( a => a.Type.Name == "MyParamAttribute" );

        // Check attributes on the getter.
        var extensionGetterAttrs = extensionGetter.Attributes.ToList();
        var extensionGetterMyMethodAttr = extensionGetterAttrs.FirstOrDefault( a => a.Type.Name == "MyMethodAttribute" );

        // Now check the implicit implementation methods on the parent type.
        // These are the static methods that Roslyn generates.
        var implMethods = type.Methods.Where( m => m.IsImplicitlyDeclared ).ToList();

        // Find the implementation method for MethodWithAttributes.
        var implMethod = implMethods.FirstOrDefault( m => m.Name == "MethodWithAttributes" );
        var implMethodAttrs = implMethod?.Attributes.ToList();
        var implMethodMyMethodAttr = implMethodAttrs?.FirstOrDefault( a => a.Type.Name == "MyMethodAttribute" );

        // Check implementation method's return parameter.
        var implMethodReturnAttrs = implMethod?.ReturnParameter.Attributes.ToList();
        var implMethodReturnMyReturnAttr = implMethodReturnAttrs?.FirstOrDefault( a => a.Type.Name == "MyReturnAttribute" );

        // Check implementation method's parameters (first param is receiver, second is x).
        var implMethodParamAttrs = implMethod?.Parameters.Count > 1 ? implMethod.Parameters[1].Attributes.ToList() : null;
        var implMethodParamMyParamAttr = implMethodParamAttrs?.FirstOrDefault( a => a.Type.Name == "MyParamAttribute" );

        // Find the implementation method for the getter (get_PropertyWithAttributes).
        var implGetter = implMethods.FirstOrDefault( m => m.Name == "get_PropertyWithAttributes" );
        var implGetterAttrs = implGetter?.Attributes.ToList();
        var implGetterMyMethodAttr = implGetterAttrs?.FirstOrDefault( a => a.Type.Name == "MyMethodAttribute" );

        // Output the findings for analysis.
        // Extension method attributes:
        Assert.NotNull( extensionMethodMyMethodAttr );       // Extension method has the attribute.
        Assert.NotNull( extensionMethodReturnMyReturnAttr ); // Extension method return has the attribute.
        Assert.NotNull( extensionMethodParamMyParamAttr );   // Extension method param has the attribute.
        Assert.NotNull( extensionGetterMyMethodAttr );       // Extension getter has the attribute.

        // Implementation method attributes - document what Roslyn does:
        // These assertions document the observed behavior. If they fail, Roslyn's behavior has changed.

        // Method attribute mirroring:
        var methodAttrMirrored = implMethodMyMethodAttr != null;

        // Return attribute mirroring:
        var returnAttrMirrored = implMethodReturnMyReturnAttr != null;

        // Parameter attribute mirroring:
        var paramAttrMirrored = implMethodParamMyParamAttr != null;

        // Getter attribute mirroring:
        var getterAttrMirrored = implGetterMyMethodAttr != null;

        // Log what we found (these will show in test output if test fails).
        Assert.True( implMethod != null, "Implementation method 'MethodWithAttributes' not found" );
        Assert.True( implGetter != null, "Implementation getter 'get_PropertyWithAttributes' not found" );

        // Document the actual Roslyn behavior by checking what's mirrored.
        // If these assertions fail, we'll see what Roslyn actually does.
        // For now, we're just documenting - adjust based on actual behavior.

        // Document Roslyn's actual behavior: ALL attributes are mirrored from extension members to implementation.
        // Method attributes are mirrored.
        Assert.True( methodAttrMirrored, "Roslyn mirrors method attributes to implementation" );

        // Return attributes are mirrored.
        Assert.True( returnAttrMirrored, "Roslyn mirrors return attributes to implementation" );

        // Parameter attributes are mirrored (at the correct offset - first param is receiver).
        Assert.True( paramAttrMirrored, "Roslyn mirrors parameter attributes to implementation" );

        // Accessor (getter/setter) attributes are mirrored.
        Assert.True( getterAttrMirrored, "Roslyn mirrors accessor attributes to implementation" );

        // Verify the implementation method has the expected structure.
        Assert.Equal( 2, implMethod.Parameters.Count ); // receiver + x
        Assert.Single( implGetter.Parameters );         // receiver only
    }
}

#endif