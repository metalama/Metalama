// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#if NET8_0_OR_GREATER
using System;
using System.Text;
#endif

namespace Metalama.Framework.Tests.UnitTests
{
    public sealed class AspectOrderingTests : UnitTestClass
    {
        public AspectOrderingTests( ITestOutputHelper logger ) : base( logger, false ) { }

        protected override void ConfigureServices( IAdditionalServiceCollection services )
        {
            base.ConfigureServices( services );
            services.AddProjectService( new PipelineExtensionProvider( ImmutableArray<PipelineExtension>.Empty ) );
        }

        private bool TryGetOrderedAspectLayers( string code, string[] aspectNames, DiagnosticBag diagnostics, [NotNullWhen( true )] out string? sortedAspects )
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCompilationModel( code );

            var serviceProvider = testContext.ServiceProvider;

            var compileTimeDomain = testContext.Domain;

            var compileTimeProjectRepository = CompileTimeProjectRepository.Create(
                    compileTimeDomain,
                    serviceProvider,
                    compilation.RoslynCompilation,
                    NullDiagnosticAdder.Instance )
                .AssertNotNull();

            Assert.NotNull( compileTimeProjectRepository );

            var compileTimeProject = compileTimeProjectRepository.RootProject;

            serviceProvider = serviceProvider.WithCompileTimeProjectServices( compileTimeProjectRepository );

            var aspectTypeFactory = new AspectClassFactory(
                new AspectDriverFactory( compilation, ImmutableArray<object>.Empty, serviceProvider ),
                compilation.CompilationContext );

            var aspectNamedTypes = aspectNames.SelectAsImmutableArray( name => compilation.Types.OfName( name ).Single().GetSymbol() );

            var aspectTypes = aspectTypeFactory.GetClasses(
                    serviceProvider,
                    compilation.CompilationContext,
                    aspectNamedTypes,
                    compileTimeProject,
                    diagnostics )
                .ToImmutableArray();

            var dependencies = new IAspectOrderingSource[]
            {
                new AspectLayerOrderingSource( aspectTypes ), new AttributeAspectOrderingSource( serviceProvider, compilation.CompilationContext )
            };

            if ( AspectLayerSorter.TrySort(
                    aspectTypes,
                    dependencies, // We don't apply alphabetical ordering for better testing.
                    diagnostics,
                    out var sortedAspectLayers ) )
            {
                sortedAspects = string.Join(
                    ", ",
                    sortedAspectLayers.OrderBy( l => l.Order ).ThenBy( l => l.AspectName ) );

                this.TestOutput.WriteLine( sortedAspects );

                return true;
            }
            else
            {
                sortedAspects = null;

                return false;
            }
        }

        private string GetOrderedAspectLayers( string code, params string[] aspectNames )
        {
            var diagnostics = new DiagnosticBag();
            Assert.True( this.TryGetOrderedAspectLayers( code, aspectNames, diagnostics, out var sortedAspects ), "A cycle was detected." );
            Assert.Empty( diagnostics );

            return sortedAspects;
        }

        [Fact]
        public void OneSingleLayerAspect()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
class Aspect1 : TypeAspect { }
";

            var ordered = this.GetOrderedAspectLayers( code, "Aspect1" );
            Assert.Equal( "Aspect1 => 0", ordered );
        }

        [Fact]
        public void OneDoubleLayerAspect()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[Layers(""Layer1"")]
class Aspect1 : TypeAspect { }
";

            var ordered = this.GetOrderedAspectLayers( code, "Aspect1" );
            Assert.Equal( "Aspect1 => 0, Aspect1:Layer1 => 1", ordered );
        }

        [Fact]
        public void TwoUnorderedDoubleLayerAspects()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[Layers(""Layer1"")]
class Aspect1 : TypeAspect { }

[Layers(""Layer1"")]
class Aspect2 : TypeAspect { }
";

            var ordered = this.GetOrderedAspectLayers( code, "Aspect1", "Aspect2" );
            Assert.Equal( "Aspect2 => 0, Aspect1 => 0, Aspect2:Layer1 => 1, Aspect1:Layer1 => 1", ordered );
        }

        [Fact]
        public void ThreeOrderedSingleLayerAspects()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect2), typeof(Aspect1), typeof(Aspect3) ) ]

class Aspect3 : TypeAspect
{
    
}

class Aspect1 : TypeAspect
{
    
}

class Aspect2 : TypeAspect
{
    
}
";

            var ordered = this.GetOrderedAspectLayers( code, "Aspect1", "Aspect2", "Aspect3" );
            Assert.Equal( "Aspect3 => 0, Aspect1 => 1, Aspect2 => 2", ordered );
        }

        [Fact]
        public void TwoOrderedDoubleLayerAspects()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect2), typeof(Aspect1) ) ]

[Layers(""Layer1"")]
class Aspect1 : TypeAspect { }

[Layers(""Layer1"")]
class Aspect2 : TypeAspect { }
";

            var ordered = this.GetOrderedAspectLayers( code, "Aspect1", "Aspect2" );
            Assert.Equal( "Aspect1 => 0, Aspect1:Layer1 => 1, Aspect2 => 2, Aspect2:Layer1 => 3", ordered );
        }

        [Fact]
        public void TwoPartiallyOrderedDoubleLayerAspects()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[assembly: AspectOrder( ""Aspect2"", ""Aspect1"" ) ]

[Layers(""Layer1"")]
class Aspect1  : TypeAspect { }

[Layers(""Layer1"")]
class Aspect2  : TypeAspect { }
";

            var ordered = this.GetOrderedAspectLayers( code, "Aspect1", "Aspect2" );
            Assert.Equal( "Aspect1 => 0, Aspect2 => 1, Aspect1:Layer1 => 1, Aspect2:Layer1 => 2", ordered );
        }

        [Fact]
        public void TwoTotallyOrderedDoubleLayerAspects()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[assembly: AspectOrder( ""Aspect2:Layer1"", ""Aspect1:Layer1"", ""Aspect2"", ""Aspect1"" ) ]

[Layers(""Layer1"")]
class Aspect1  : TypeAspect { }

[Layers(""Layer1"")]
class Aspect2  : TypeAspect { }
";

            var ordered = this.GetOrderedAspectLayers( code, "Aspect1", "Aspect2" );
            Assert.Equal( "Aspect1 => 0, Aspect2 => 1, Aspect1:Layer1 => 2, Aspect2:Layer1 => 3", ordered );
        }

        [Fact]
        public void InheritedAspects()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[Layers(""Layer1"")]
class Aspect1  : TypeAspect { }

class Aspect2 : Aspect1 {}
";

            var ordered = this.GetOrderedAspectLayers( code, "Aspect1", "Aspect2" );
            Assert.Equal( "Aspect2 => 0, Aspect1 => 0, Aspect2:Layer1 => 1, Aspect1:Layer1 => 1", ordered );
        }

        [Fact]
        public void ApplyToDerivedTypes()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[assembly: AspectOrder( ""AspectAA"", ""AspectBA"", ApplyToDerivedTypes = true ) ]

abstract class  AspectAA  : TypeAspect { }

class AspectAB : AspectAA {}

abstract class  AspectBA  : TypeAspect { }

class AspectBB : AspectBA {}


";

            var ordered = this.GetOrderedAspectLayers( code, "AspectAA", "AspectAB", "AspectBA", "AspectBB" );
            Assert.Equal( "AspectBB => 0, AspectAB => 1", ordered );
        }

        [Fact]
        public void InvalidAspectName()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[assembly: AspectOrder( ""NonExistent1"", ""Aspect1"" ) ]

class Aspect1 : TypeAspect { }

";

            var ordered = this.GetOrderedAspectLayers( code, "Aspect1" );
            Assert.Equal( "Aspect1 => 0", ordered );
        }

        [Fact]
        public void Cycle()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect2), typeof(Aspect1) ) ]
[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2) ) ]

class Aspect1 : TypeAspect { }
class Aspect2 : TypeAspect { }
";

            var diagnostics = new DiagnosticBag();
            Assert.False( this.TryGetOrderedAspectLayers( code, new[] { "Aspect1", "Aspect2" }, diagnostics, out _ ) );
            Assert.Single( diagnostics.SelectAsReadOnlyCollection( d => d.Id ), GeneralDiagnosticDescriptors.CycleInAspectOrdering.Id );
        }

        [Fact]
        public void Cycle2()
        {
            // The difference of Cycle2 compared to Cycle1 is that Aspect3 has no predecessor (in Cycle test, all nodes have a predecessor),
            // therefore the sort algorithm goes to another branch.

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect2), typeof(Aspect1), typeof(Aspect3) ) ]
[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2) ) ]

class Aspect1 : TypeAspect { }

class Aspect2 : TypeAspect { }

class Aspect3 : TypeAspect { }
";

            var diagnostics = new DiagnosticBag();
            Assert.False( this.TryGetOrderedAspectLayers( code, new[] { "Aspect1", "Aspect2", "Aspect3" }, diagnostics, out _ ) );
            Assert.Single( diagnostics.SelectAsReadOnlyCollection( d => d.Id ), GeneralDiagnosticDescriptors.CycleInAspectOrdering.Id );
        }

#if NET8_0_OR_GREATER
#pragma warning disable CA1305 // Specify IFormatProvider
        [Fact]
        public void ManyAspects()
        {
            // This test is carefully crafted to trigger the (now fixed) bug.
            // If the implementation of AspectLayerSorter, Array.Sort (used in AspectLayerSorter) or Random changes,
            // it might stop checking the problematic situation.

            var random = new Random( 45 );

            var stringBuilder = new StringBuilder(
                """
                using Metalama.Framework.Aspects;

                """ );

            const int n = 17;

            var aspects = Enumerable.Range( 1, n ).ToArray();
            random.Shuffle( aspects );

            var aspectOrder = string.Join( ", ", aspects.Reverse().Select( a => $"typeof(Aspect{a})" ) );

            stringBuilder.AppendLine( $"[assembly: AspectOrder( {aspectOrder} ) ]" );
            stringBuilder.AppendLine();

            for ( var j = 1; j <= n; j++ )
            {
                stringBuilder.AppendLine( $"class Aspect{j} : TypeAspect {{ }}" );
            }

            var code = stringBuilder.ToString();

            var ordered = this.GetOrderedAspectLayers( code, Enumerable.Range( 1, n ).Select( a => $"Aspect{a}" ).ToArray() );

            var expected = string.Join( ", ", aspects.Select( ( a, i ) => $"Aspect{a} => {i}" ) );

            Assert.Equal( expected, ordered );
        }
#pragma warning restore CA1305
#endif
    }
}