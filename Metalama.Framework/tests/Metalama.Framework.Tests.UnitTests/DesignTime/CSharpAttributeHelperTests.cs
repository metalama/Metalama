// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Refactoring;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable InconsistentNaming

namespace Metalama.Framework.Tests.UnitTests.DesignTime
{
    public sealed class CSharpAttributeHelperTests : IDisposable
    {
        private readonly ITestOutputHelper _logger;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly AdhocWorkspace _workspace = new();
        private Document? _testFileDocument;

        public CSharpAttributeHelperTests( ITestOutputHelper logger )
        {
            this._logger = logger;
        }

        public void Dispose() => this._workspace.Dispose();

        private void LogAndAssertContains<T>( IEnumerable<T> enumerable, T expectedItem, int expectedOccurrences = 1 )
        {
            this._logger.WriteLine( "Expected:" );
            this._logger.WriteLine( expectedItem!.ToString() );
            this._logger.WriteLine( "Actual:" );

            var actualOccurrences = 0;

            foreach ( var item in enumerable )
            {
                this._logger.WriteLine( item?.ToString() ?? "<null>" );

                if ( expectedItem.Equals( item ) )
                {
                    actualOccurrences++;
                }
            }

            Assert.Equal( expectedOccurrences, actualOccurrences );
        }

        private void LogAndAssertContains( IEnumerable<AttributeSyntax> attributes, string expectedAttributeName, int expectedOccurrences = 1 )
        {
            this.LogAndAssertContains( attributes.Select( a => a.Name.ToString() ), expectedAttributeName, expectedOccurrences );
        }

        [Fact]
        public async Task Can_AddAttributeToMethodAsync()
        {
            const string syntax = @"
public class Class
{
    public void Method1() { }
}
";

            var originalMethodDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalMethodDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .First()
                .AttributeLists
                .SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task Can_AddAttributeWithArgumentsAndPropertiesAsync()
        {
            const string syntax = @"
public class Class
{
    public void Method1() { }
}
";

            SyntaxNode originalMethodDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            var attributeDescription = new AttributeDescription(
                name: "TestAttribute",
                constructorArguments: new[] { "ARG1", "ARG2" }.ToImmutableList(),
                namedArguments: new List<(string Name, string Value)> { ("Prop1", "111"), ("Prop2", "222") }.ToImmutableList() );

            var newRoot = await this.AddAttributeThroughSymbolAsync( originalMethodDeclaration, attributeDescription );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .First()
                .AttributeLists
                .SelectAsImmutableArray( list => list.ToString() );

            this.LogAndAssertContains( resultAttributes, "[TestAttribute(ARG1, ARG2, Prop1 = 111, Prop2 = 222)]" );
        }

        [Fact]
        public async Task Can_AddAttributeToClassAsync()
        {
            const string syntax = @"
public class Class
{
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First();

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<ClassDeclarationSyntax>()
                .First()
                .AttributeLists
                .SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task Can_AddAttributeToInterfaceAsync()
        {
            const string syntax = @"
public interface IInterface
{
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .First();

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<InterfaceDeclarationSyntax>()
                .First()
                .AttributeLists
                .SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task Can_AddAttributeToDelegateAsync()
        {
            const string syntax = @"
public delegate void Delegate();
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<DelegateDeclarationSyntax>()
                .First();

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<DelegateDeclarationSyntax>()
                .First()
                .AttributeLists
                .SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task Can_AddAttributeToEnumAsync()
        {
            const string syntax = @"
public enum Enum
{
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<EnumDeclarationSyntax>()
                .First();

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<EnumDeclarationSyntax>()
                .First()
                .AttributeLists
                .SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task Can_AddAttributeToPropertyAccessorAsync()
        {
            const string syntax = @"
public class Class
{
    public int P1 { get; set; }
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<AccessorDeclarationSyntax>()
                .First( x => x.Keyword.ValueText == "get" );

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<AccessorDeclarationSyntax>()
                .First( x => x.Keyword.ValueText == "get" )
                .AttributeLists.SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task Can_AddAttributeToEventAsync()
        {
            const string syntax = @"
public class Class
{
    public event EventHandler { add { } remove { } }
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<EventDeclarationSyntax>()
                .First();

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<EventDeclarationSyntax>()
                .First()
                .AttributeLists.SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task Can_AddAttributeToFieldAsync()
        {
            const string syntax = @"
public class Class
{
    private List<int> field;
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .First( x => x.Declaration.Variables.First().Identifier.ToString() == "field" )
                .Declaration.Variables.First();

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<FieldDeclarationSyntax>()
                .First( x => x.Declaration.Variables.First().Identifier.ToString() == "field" )
                .AttributeLists.SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task Can_AddAttributeToVariableDeclaratorAsync()
        {
            const string syntax = @"
public class Class
{
    private List<int> field = new List<int>();
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .First( x => x.Identifier.ToString() == "field" );

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<FieldDeclarationSyntax>()
                .First( x => x.Declaration.Variables.First().Identifier.ToString() == "field" )
                .AttributeLists.SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task Can_AddAttributeToParameterAsync()
        {
            const string syntax = @"
public class Class
{
    public void Method(int param1, string param2) { }
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<ParameterSyntax>()
                .First( x => x.Identifier.ToString() == "param2" );

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var resultAttributes = newRoot.DescendantNodesAndSelf()
                .OfType<ParameterSyntax>()
                .First( x => x.Identifier.ToString() == "param2" )
                .AttributeLists.SelectMany( list => list.Attributes );

            this.LogAndAssertContains( resultAttributes, "TestAttribute" );
        }

        [Fact]
        public async Task When_AttributeAppliedToDeclarationWithComment_Then_AttributePreserveCommentAsync()
        {
            const string syntax = @"
namespace Test
{
    public class Class1 { }

    
    /// <summary>
    /// This is summary that needs to be preserved.
    /// </summary>
    public class Class2 { }
}
";

            const string expectedSyntax = @"
namespace Test
{
    public class Class1 { }


    /// <summary>
    /// This is summary that needs to be preserved.
    /// </summary>
    [TestAttribute]
    public class Class2 { }
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First( x => x.Identifier.ToString() == "Class2" );

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            var newSyntax = newRoot.ToFullString();

            AssertEx.EolInvariantEqual( expectedSyntax, newSyntax );
        }

        [Fact]
        public async Task When_AttributeAppliedToIndentedDeclaration_Then_AttributePreserveIndentationAndExactlyOneNewlineAfterAttributeAsync()
        {
            const string syntax = @"
namespace Test
{
    public class Class1 { }

    public class Class2 { }
}
";

            const string expectedSyntax = @"
namespace Test
{
    public class Class1 { }

    [TestAttribute]
    public class Class2 { }
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First( x => x.Identifier.ToString() == "Class2" );

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            AssertEx.EolInvariantEqual( expectedSyntax, newRoot.ToFullString() );
        }

        [Fact]
        public async Task When_AttributeAppliedToIndentedDeclarationWithAnAttribute_Then_AttributePreserveIndentationAndExactlyOneNewlineAfterAttributeAsync()
        {
            const string syntax = @"
namespace Test
{
    public class Class1 { }

    [FirstAttribute]
    public class Class2 { }
}
";

            const string expectedSyntax = @"
namespace Test
{
    public class Class1 { }

    [FirstAttribute]
    [TestAttribute]
    public class Class2 { }
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First( x => x.Identifier.ToString() == "Class2" );

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            AssertEx.EolInvariantEqual( expectedSyntax, newRoot.ToFullString() );
        }

        [Fact]
        public async Task
            When_AttributeAppliedToIndentedDeclarationWithSeveralAttributes_Then_AttributePreserveIndentationAndExactlyOneNewlineAfterAttributeAsync()
        {
            const string syntax = @"
namespace Test
{
    public class Class1 { }

    [FirstAttribute]
    [SecondAttribute]
    [ThirdAttribute]
    public class Class2 { }
}
";

            const string expectedSyntax = @"
namespace Test
{
    public class Class1 { }

    [FirstAttribute]
    [SecondAttribute]
    [ThirdAttribute]
    [TestAttribute]
    public class Class2 { }
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First( x => x.Identifier.ToString() == "Class2" );

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            AssertEx.EolInvariantEqual( expectedSyntax, newRoot.ToFullString() );
        }

        [Fact]
        public async Task When_AttributeAppliedToParameter_Then_NoIndentationAndNoNewlineAsync()
        {
            const string syntax = @"
namespace Test
{
    public class Class1
    {
        public void Method(int param1, string param2) { }
    }
}
";

            const string expectedSyntax = @"
namespace Test
{
    public class Class1
    {
        public void Method(int param1, [TestAttribute] string param2) { }
    }
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<ParameterSyntax>()
                .First( x => x.Identifier.ToString() == "param2" );

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            AssertEx.EolInvariantEqual( expectedSyntax, newRoot.ToFullString() );
        }

        [Fact]
        public async Task When_AttributeAppliedToAssembly_Then_AttributeIsAddedOnNewLineAfterLeadingCommentAsync()
        {
            const string syntax = """// leading comment""";

            const string expectedSyntax = """
                                          // leading comment
                                          [assembly: TestAttribute]

                                          """;

            var originalRoot = await this.GetSyntaxRootAsync( syntax );

            var newRoot = await this.AddAttributeAsync( "TestAttribute", originalRoot );

            Assert.Equal( expectedSyntax, newRoot.ToFullString() );
        }

        [Fact]
        public async Task When_AttributeAppliedToAssembly_Then_AttributeIsAddedOnNewLineAfterAlreadyExistingAttributesAsync()
        {
            const string syntax = """
                                  // leading comment
                                  // some comment
                                  [assembly: TestAttribute]
                                  // some other comment
                                  """;

            const string expectedSyntax = """
                                          // leading comment
                                          // some comment
                                          [assembly: TestAttribute]
                                          // some other comment
                                          [assembly: TestAttribute]

                                          """;

            var originalRoot = await this.GetSyntaxRootAsync( syntax );

            var newRoot = await this.AddAttributeAsync( "TestAttribute", originalRoot );

            Assert.Equal( expectedSyntax, newRoot.ToFullString() );
        }

        [Fact]
        public async Task When_AttributeAppliedToAssembly_Then_AttributeIsAddedBeforeNamespaceAsync()
        {
            const string syntax = """
                                  using System;

                                  namespace Test
                                  {
                                  }
                                  """;

            const string expectedSyntax = """
                                          using System;

                                          [assembly: TestAttribute]

                                          namespace Test
                                          {
                                          }
                                          """;

            var originalRoot = await this.GetSyntaxRootAsync( syntax );

            var newRoot = await this.AddAttributeAsync( "TestAttribute", originalRoot );

            Assert.Equal( expectedSyntax, newRoot.ToFullString() );
        }

        [Fact]
        public async Task When_AttributeAppliedToAssembly_Then_AttributeIsAddedBeforeFileScopedNamespaceAsync()
        {
            const string syntax = """
                                  namespace Test;
                                  """;

            const string expectedSyntax = """
                                          [assembly: TestAttribute]

                                          namespace Test;
                                          """;

            var originalRoot = await this.GetSyntaxRootAsync( syntax );

            var newRoot = await this.AddAttributeAsync( "TestAttribute", originalRoot );

            Assert.Equal( expectedSyntax, newRoot.ToFullString() );
        }

        [Fact]
        public async Task When_AttributeAppliedToAssembly_Then_AttributeIsAddedBeforeClassAsync()
        {
            const string syntax = """
                                  public class Test
                                  {
                                  }
                                  """;

            const string expectedSyntax = """
                                          [assembly: TestAttribute]

                                          public class Test
                                          {
                                          }
                                          """;

            var originalRoot = await this.GetSyntaxRootAsync( syntax );

            var newRoot = await this.AddAttributeAsync( "TestAttribute", originalRoot );

            Assert.Equal( expectedSyntax, newRoot.ToFullString() );
        }

        [Fact]
        public async Task When_AttributeAppliedToParameterWithAttribute_Then_NoIndentationAndNoNewlineAsync()
        {
            const string syntax = @"
namespace Test
{
    public class Class1
    {
        public void Method(int param1, [FirstAttribute] string param2) { }
    }
}
";

            const string expectedSyntax = @"
namespace Test
{
    public class Class1
    {
        public void Method(int param1, [FirstAttribute][TestAttribute] string param2) { }
    }
}
";

            SyntaxNode originalDeclaration = (await this.GetSyntaxRootAsync( syntax )).DescendantNodes()
                .OfType<ParameterSyntax>()
                .First( x => x.Identifier.ToString() == "param2" );

            var newRoot = await this.AddAttributeThroughSymbolAsync( "TestAttribute", originalDeclaration );

            AssertEx.EolInvariantEqual( expectedSyntax, newRoot.ToFullString() );
        }

        private Task<SyntaxNode> AddAttributeThroughSymbolAsync( string attributeName, SyntaxNode originalDeclaration )
            => this.AddAttributeThroughSymbolAsync( originalDeclaration, new AttributeDescription( attributeName ) );

        private async Task<SyntaxNode> AddAttributeThroughSymbolAsync( SyntaxNode syntaxNodeToBeDecorated, AttributeDescription attributeDescription )
        {
            var (semanticModel, context) = await this.GetModelAndContextAsync( syntaxNodeToBeDecorated );

            var symbolToBeDecorated = semanticModel.GetDeclaredSymbol( syntaxNodeToBeDecorated );

            var resultSolution = await CSharpAttributeHelper.AddAttributeAsync(
                this._testFileDocument,
                symbolToBeDecorated!,
                attributeDescription,
                context,
                this._cancellationTokenSource.Token );

            var resultDocument = resultSolution.GetDocument( this._testFileDocument.Id );
            var resultRoot = await resultDocument!.GetSyntaxRootAsync();

            return resultRoot!;
        }

        private async Task<SyntaxNode> AddAttributeAsync( string attributeName, SyntaxNode syntaxNodeToBeDecorated )
        {
            var attributeDescription = new AttributeDescription( attributeName );

            var (_, context) = await this.GetModelAndContextAsync( syntaxNodeToBeDecorated );

            var resultSolution = await CSharpAttributeHelper.AddAttributeAsync(
                this._testFileDocument,
                syntaxNodeToBeDecorated,
                attributeDescription,
                context,
                this._cancellationTokenSource.Token );

            var resultDocument = resultSolution.GetDocument( this._testFileDocument.Id );
            var resultRoot = await resultDocument!.GetSyntaxRootAsync();

            return resultRoot!;
        }

        private async Task<(SemanticModel, SyntaxGenerationContext)> GetModelAndContextAsync( SyntaxNode syntaxNodeToBeDecorated )
        {
            var semanticModel = await this._testFileDocument!.GetSemanticModelAsync();

            var context = semanticModel.Compilation.GetCompilationContext()
                .GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted, syntaxNodeToBeDecorated );

            return (semanticModel, context);
        }

        private async Task<SyntaxNode> GetSyntaxRootAsync( string syntax )
        {
            var project = ProjectInfo.Create( ProjectId.CreateNewId(), VersionStamp.Create(), "TestProject", "TestProject", LanguageNames.CSharp );
            this._workspace.AddProject( project );
            this._testFileDocument = this._workspace.AddDocument( project.Id, "TestFile.cs", SourceText.From( syntax ) );

            return (await this._testFileDocument.GetSyntaxRootAsync())!;
        }
    }
}