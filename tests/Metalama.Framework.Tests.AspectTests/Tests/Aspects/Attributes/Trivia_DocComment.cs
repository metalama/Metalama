using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Fabrics;

using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(AddChildAspectsAspect), typeof(TestAspect))]


#pragma warning disable CS0169, CS0649

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_DocComment;

public sealed class TestAspect : TypeAspect
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public string ErrorMessage { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceAttribute(
            AttributeConstruction.Create( ( (INamedType)TypeFactory.GetType( typeof(TestAttribute) ) ).Constructors.Single() ),
            OverrideStrategy.Override );

        foreach (var property in builder.Target.FieldsAndProperties)
        {
            builder.With( property )
                .IntroduceAttribute(
                    AttributeConstruction.Create( ( (INamedType)TypeFactory.GetType( typeof(TestAttribute) ) ).Constructors.Single() ),
                    OverrideStrategy.Override );
        }
    }
}

public class ExistingAttribute : Attribute { }

public class TestAttribute : Attribute { }

public class AddChildAspectsAspect : TypeAspect
{
    override public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var type in builder.Target.Types)
        {
            builder.With( type ).AddAspect<TestAspect>();
        }
    }
}

[AddChildAspectsAspect]
internal partial class TestTypes;

// <target>
internal partial class TestTypes
{
    /// <summary>
    /// </summary>
    private class C
    {
        /// <summary>
        /// </summary>
        public int? Field1;

        /// <summary>
        /// </summary>
        [Test]
        public int? Field2;

        /// <summary>
        /// </summary>
        [Existing]
        public int? Field3;

        /// <summary>
        /// </summary>
        [Existing]
        [Test]
        public int? Field4;

        /// <summary>
        /// </summary>
        public int? Property1 { get; }

        /// <summary>
        /// </summary>
        [Test]
        public int? Property2 { get; }

        /// <summary>
        /// </summary>
        [Existing]
        public int? Property3 { get; }

        /// <summary>
        /// </summary>
        [Existing]
        [Test]
        public int? Property4 { get; }
    }

    /// <summary>
    /// </summary>
    private enum E
    {
        /// <summary>
        /// </summary>
        Value1,

        /// <summary>
        /// </summary>
        [Test]
        Value2,

        /// <summary>
        /// </summary>
        [Existing]
        Value3,

        /// <summary>
        /// </summary>
        [Existing]
        [Test]
        Value4
    }
}