// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NET5_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime
{
    // We skip this test in .NET Framework because we would need to implement all implicit interface methods, and it would have low value anyway.
    public sealed class EligibilityTests : UnitTestClass
    {
        private void IsEligible( string code, string target, string aspects )
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );

            static string GetName( INamedDeclaration d )
                => d switch
                {
                    IParameter { IsReturnParameter: false } parameter => parameter.DeclaringMember.Name + "." + parameter.Name,
                    IParameter { IsReturnParameter: true } returnParameter => returnParameter.DeclaringMember.Name + "." + "return",
                    IConstructor { IsStatic: false } constructor => constructor.DeclaringType.Name + ".new",
                    IConstructor { IsStatic: true } constructor => constructor.DeclaringType.Name + ".static",
                    _ => d.Name
                };

            var declarationList = compilation
                .GetContainedDeclarations()
                .OfType<INamedDeclaration>()
                .Where( d => d.Name != "BuildEligibility" && d.ContainingDeclaration is not INamedDeclaration { Name: "BuildEligibility" } )
                .ToReadOnlyList();

            var declarations = declarationList
                .ToDictionary( GetName, d => d );

            using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
            var pipeline = pipelineFactory.CreatePipeline( compilation.RoslynCompilation );

            // Force the pipeline to execute so the tests can do queries over it.
            pipeline.Execute( compilation.RoslynCompilation );

            var targetSymbol = declarations[target].GetSymbol().AssertNotNull();

            var eligibleAspects = pipeline.GetEligibleAspects( compilation.RoslynCompilation, targetSymbol, default )
                .Where( c => !c.Project!.IsFramework );

            var eligibleAspectsString = string.Join( ",", eligibleAspects.OrderBy( a => a.ShortName ) );

            Assert.Equal( aspects, eligibleAspectsString );
        }

        [Theory]
        [InlineData( "Class", "DeclarationAspect,MyTypeAspect" )]
        [InlineData( "Class.new", "ConstructorAspect,DeclarationAspect,MethodBaseAspect" )]
        [InlineData( "Class.static", "ConstructorAspect,DeclarationAspect,MethodBaseAspect" )]
        [InlineData( "Method", "DeclarationAspect,MethodAspect,MethodBaseAspect" )]
        [InlineData( "StaticMethod", "DeclarationAspect,MethodAspect,MethodBaseAspect,StaticMethodAspect" )]
        [InlineData( "Method.intParameter", "DeclarationAspect,ParameterAspect" )]
        [InlineData( "Field", "DeclarationAspect" )]
        [InlineData( "Property", "DeclarationAspect" )]
        [InlineData( "Event", "DeclarationAspect" )]
        public void IsEligibleFromType( string target, string aspects )
        {
            const string code = @"
using System;
using Metalama.Framework.Code;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Eligibility;

class MethodAspect : IAspect<IMethod> { }
class StaticMethodAspect : IAspect<IMethod> { public void BuildEligibility( IEligibilityBuilder<IMethod> builder ) => builder.MustBeStatic(); }
class ConstructorAspect : IAspect<IConstructor> { }
class MethodBaseAspect : IAspect<IMethodBase> { }
class DeclarationAspect : IAspect<IDeclaration> { }
class ParameterAspect : IAspect<IParameter> { }
class GenericParameterAspect : IAspect<ITypeParameter> { }
class MyTypeAspect : TypeAspect {}

class Class<T>
{
  public Class() {}
  static Class() {}
  void Method( int intParameter, string stringParameter ) {}
  static void StaticMethod( int p ) {}
  int Field;
  string Property { get; set; }
  event EventHandler Event;
}

namespace Ns { class C {} }
";

            this.IsEligible( code, target, aspects );
        }

        [Fact]
        public void NotEligibleWhenSameAspectPresent()
        {
            const string code = @"
using System;
using Metalama.Framework.Code;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Eligibility;

class MyTypeAspect : TypeAspect {}

[MyTypeAspect]
class ClassWithAspect {}
";

            // MyTypeAspect should not be offered because it is already there.
            this.IsEligible( code, "ClassWithAspect", "" );
        }

        [Fact]
        public void NotEligibleWhenInaccessible()
        {
            const string code = @"
interface Interface {}
";

            // InternalImplement should not be present because it is not accessible.
            this.IsEligible( code, "Interface", "" );
        }

        [Theory]
        [InlineData( "ClassWithAspect", "RequiringMyTypeAspect" )]
        [InlineData( "ClassWithoutAspect", "MyTypeAspect" )]
        public void MustHaveAspectOfType( string target, string aspects )
        {
            const string code = @"
using System;
using Metalama.Framework.Code;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Eligibility;

class MyTypeAspect : TypeAspect {}
class RequiringMyTypeAspect : TypeAspect { public override void BuildEligibility( IEligibilityBuilder<INamedType> builder ) => builder.MustHaveAspectOfType( typeof(MyTypeAspect) ); }

[MyTypeAspect]
class ClassWithAspect {}

class ClassWithoutAspect {}

";

            this.IsEligible( code, target, aspects );
        }

        [Theory]
        [InlineData( "ClassWithAspect", "" )]
        [InlineData( "ClassWithoutAspect", "ForbiddingMyTypeAspect,MyTypeAspect" )]
        public void MustNotHaveAspectOfType( string target, string aspects )
        {
            const string code = @"
using System;
using Metalama.Framework.Code;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Eligibility;

class MyTypeAspect : TypeAspect {}
class ForbiddingMyTypeAspect : TypeAspect { public override void BuildEligibility( IEligibilityBuilder<INamedType> builder ) => builder.MustNotHaveAspectOfType( typeof(MyTypeAspect) ); }

[MyTypeAspect]
class ClassWithAspect {}

class ClassWithoutAspect {}

";

            this.IsEligible( code, target, aspects );
        }

        [Fact]
        public void MustBeOfType_ThrowsWhenImpossible()
        {
            Assert.Throws<ArgumentOutOfRangeException>( () => EligibilityRuleFactory.CreateRule<IType>( d => d.MustBeInstanceOfType( typeof(int) ) ) );
        }

        [Fact]
        public void MustBeOfAnyType_ThrowsWhenImpossible()
        {
            Assert.Throws<ArgumentOutOfRangeException>( () => EligibilityRuleFactory.CreateRule<IType>( d => d.MustBeInstanceOfAnyType( typeof(int) ) ) );
        }

        [Fact]
        public void MustBeOfType()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
  void Method( int intParameter, string stringParameter ) {}
}
";

            var compilation = testContext.CreateCompilation( code );
            var intParameter = compilation.Types.Single().Methods.Single().Parameters[0];

            var eligibility = EligibilityRuleFactory.CreateRule<IType>( d => d.MustBeInstanceOfType( typeof(INamedType) ) );

            Assert.Equal( EligibleScenarios.All, eligibility.GetEligibility( intParameter.Type ) );
        }

        [Fact]
        public void MustBeOfAnyType()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
  void Method( int intParameter, string stringParameter ) {}
}
";

            var compilation = testContext.CreateCompilation( code );
            var intParameter = compilation.Types.Single().Methods.Single().Parameters[0];

            var eligibility = EligibilityRuleFactory.CreateRule<IType>( d => d.MustBeInstanceOfAnyType( typeof(INamedType), typeof(IArrayType) ) );
            Assert.Equal( EligibleScenarios.All, eligibility.GetEligibility( intParameter.Type ) );
        }

        [Fact]
        public void MustBe()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C
{
  void Method( int intParameter, string stringParameter ) {}
}
";

            var compilation = testContext.CreateCompilation( code );
            var method = compilation.Types.Single().Methods.Single();
            var intParameter = method.Parameters[0];
            var stringParameter = method.Parameters[1];

            var eligibility = EligibilityRuleFactory.CreateRule<IType>( d => d.MustEqual( typeof(int) ) );

            Assert.Equal( EligibleScenarios.All, eligibility.GetEligibility( intParameter.Type ) );
            Assert.Equal( EligibleScenarios.None, eligibility.GetEligibility( stringParameter.Type ) );
        }
    }
}
#endif