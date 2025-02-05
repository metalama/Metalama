using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia;

#pragma warning disable CS0067, CS0169

[assembly:
    AspectOrder(
        AspectOrderDirection.RunTime,
        typeof(RemoveAttributeAspect),
        typeof(IntroduceAttributeAspect),
        typeof(RemoveAttributeAspect2),
        ApplyToDerivedTypes = false )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia;

public class OldAttribute : Attribute { }

public class NewAttribute : Attribute { }

[AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
public class DescriptionAttribute : Attribute
{
    public DescriptionAttribute( string description ) { }
}

public class IntroduceAttributeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var member in builder.Target.Members().Cast<IDeclaration>().Concat( builder.Target.Types ))
        {
            builder.With( member ).IntroduceAttribute( AttributeConstruction.Create( typeof(NewAttribute) ) );
        }
    }
}

public class RemoveAttributeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var member in builder.Target.Members().Cast<IDeclaration>().Concat( builder.Target.Types ))
        {
            builder.With( member ).RemoveAttributes( typeof(OldAttribute) );
        }
    }
}

public class RemoveAttributeAspect2 : RemoveAttributeAspect { }

// <target>
internal class Target
{
    [IntroduceAttributeAspect]
    private class IntroduceTarget
    {
        /// <summary>
        /// Gets or sets a test property value.
        /// </summary>
        public static object? TestProperty { get; set; }

        /// <summary>
        /// A test method.
        /// </summary>
        public static void TestMethod() { }

        // nested class
        private class Nested { }

        /// <summary>
        /// Field.
        /// </summary>
        private int _field;

        // multifield
        private int _f1, _f2;

        /// <summary>
        /// Event.
        /// </summary>
        public event EventHandler? Event;

        /// <summary>
        /// Another event.
        /// </summary>
        public event EventHandler? Event2
        {
            add { }
            remove { }
        }
    }

    [RemoveAttributeAspect]
    private class RemoveTarget
    {
        /// <summary>
        /// Gets or sets a test property value.
        /// </summary>
        [OldAttribute]
        public static object? TestProperty { get; set; }

        /// <summary>
        /// A test method.
        /// </summary>
        [OldAttribute]
        public static void TestMethod() { }

        // nested class
        [OldAttribute]
        private class Nested { }

        /// <summary>
        /// Field.
        /// </summary>
        [OldAttribute]
        private int _field;

        // multifield
        [OldAttribute]
        private int _f1, _f2;

        /// <summary>
        /// Event.
        /// </summary>
        [OldAttribute]
        public event EventHandler? Event;

        /// <summary>
        /// Another event.
        /// </summary>
        [OldAttribute]
        public event EventHandler? Event2
        {
            add { }
            remove { }
        }
    }

    [RemoveAttributeAspect]
    [IntroduceAttributeAspect]
    private class ReplaceTarget
    {
        /// <summary>
        /// Gets or sets a test property value.
        /// </summary>
        [OldAttribute]
        public static object? TestProperty { get; set; }

        /// <summary>
        /// A test method.
        /// </summary>
        [OldAttribute]
        public static void TestMethod() { }

        // nested class
        [OldAttribute]
        private class Nested { }

        /// <summary>
        /// Field.
        /// </summary>
        [OldAttribute]
        private int _field;

        // multifield
        [OldAttribute]
        private int _f1, _f2;

        /// <summary>
        /// Event.
        /// </summary>
        [OldAttribute]
        public event EventHandler? Event;

        /// <summary>
        /// Another event.
        /// </summary>
        [OldAttribute]
        public event EventHandler? Event2
        {
            add { }
            remove { }
        }
    }

    [RemoveAttributeAspect2]
    [IntroduceAttributeAspect]
    private class ReplaceTarget2
    {
        /// <summary>
        /// Gets or sets a test property value.
        /// </summary>
        [OldAttribute]
        public static object? TestProperty { get; set; }

        /// <summary>
        /// A test method.
        /// </summary>
        [OldAttribute]
        public static void TestMethod() { }

        // nested class
        [OldAttribute]
        private class Nested { }

        /// <summary>
        /// Field.
        /// </summary>
        [OldAttribute]
        private int _field;

        // multifield
        [OldAttribute]
        private int _f1, _f2;

        /// <summary>
        /// Event.
        /// </summary>
        [OldAttribute]
        public event EventHandler? Event;

        /// <summary>
        /// Another event.
        /// </summary>
        [OldAttribute]
        public event EventHandler? Event2
        {
            add { }
            remove { }
        }
    }
}