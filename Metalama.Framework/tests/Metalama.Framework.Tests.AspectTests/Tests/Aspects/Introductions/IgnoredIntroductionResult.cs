#if TEST_OPTIONS
// @Include(../../Common/_ImplementInterfaceAdviceResultExtensions.cs)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;
using Metalama.Framework.Engine.Advising;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.IgnoredIntroductionResult;

#pragma warning disable CS0067, CS0169, CS0649

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        AssertResult(
            builder.IntroduceField(
                nameof(_field),
                whenExists: OverrideStrategy.Ignore ) );

        AssertResult(
            builder.IntroduceProperty(
                nameof(Property),
                whenExists: OverrideStrategy.Ignore ) );

        AssertResult(
            builder.IntroduceIndexer(
                typeof(int),
                nameof(IndexerGet),
                null,
                whenExists: OverrideStrategy.Ignore ) );

        AssertResult(
            builder.IntroduceEvent(
                nameof(Event),
                whenExists: OverrideStrategy.Ignore ) );

        AssertResult(
            builder.IntroduceUnaryOperator(
                nameof(OperatorMinus),
                builder.Target,
                builder.Target,
                OperatorKind.UnaryNegation,
                whenExists: OverrideStrategy.Ignore ) );

        AssertResult(
            builder.IntroduceMethod(
                nameof(CloneMethod),
                whenExists: OverrideStrategy.Ignore,
                buildMethod: method => method.Name = nameof(Clone) ) );

        AssertResult(
            builder.IntroduceAttribute(
                AttributeConstruction.Create( typeof(Aspect) ),
                OverrideStrategy.Ignore ) );

        var implementInterfaceResult = builder.ImplementInterface( typeof(ICloneable), OverrideStrategy.Ignore );

        if (implementInterfaceResult.Outcome != AdviceOutcome.Ignore)
        {
            throw new Exception( "Advice wasn't ignored." );
        }

        if (implementInterfaceResult.GetObsoleteInterfaceMembers().Count != 1)
        {
            throw new Exception( $"Expected 1 implemented interface member, got {implementInterfaceResult.GetObsoleteInterfaceMembers().Count}." );
        }
    }

    private static void AssertResult( IIntroductionAdviceResult<IDeclaration> result )
    {
        if (result.Outcome != AdviceOutcome.Ignore)
        {
            throw new Exception( "Advice wasn't ignored." );
        }

        if (result.Declaration == null)
        {
            throw new Exception( "Original declaration wasn't provided." );
        }
    }

    [Template]
    private int _field;

    [Template]
    private int Property { get; set; }

    [Template]
    private int IndexerGet( int i ) => 42;

    [Template]
    private event EventHandler? Event;

    [Template]
    public static Target OperatorMinus( Target target ) => target;

    [Template]
    public object CloneMethod() => new Target();

    [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Ignore )]
    public object Clone() => new Target();
}

// <target>
[Aspect]
internal class Target : ICloneable
{
    private int _field;

    private int Property { get; set; }

    private int this[ int i ] => 42;

    private event EventHandler? Event;

    public static Target operator -( Target target ) => target;

    public object Clone() => new Target();

    ~Target() { }
}