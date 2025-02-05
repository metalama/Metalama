using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#pragma warning disable CS8618, CS0618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples.Memento2;

public class MementoAttribute : TypeAspect
{
    [CompileTime]
    private sealed record Tags( 
        INamedType SnapshopType, 
        IReadOnlyList<(IFieldOrProperty Source, IField Snapshot)> Fields );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var snapshotType =
            builder.IntroduceClass(
                "Snapshot",
                buildType: b => { b.Accessibility = Accessibility.Private; } );

        var fields = new List<(IFieldOrProperty Source, IField Snapshot)>();

        var sourceFields = builder.Target.FieldsAndProperties
            .Where( f => f is { IsAutoPropertyOrField: true, IsImplicitlyDeclared: false, Writeability: Writeability.All } );
        
        foreach ( var sourceField in sourceFields )
        {
            var fieldIntroduction = snapshotType.IntroduceField(
                sourceField.Name,
                sourceField.Type,
                buildField: b =>
                {
                    b.Accessibility = Accessibility.Public;
                    b.Writeability = Writeability.ConstructorOnly;
                } );

            fields.Add( (sourceField, fieldIntroduction.Declaration) );
        }

        snapshotType.IntroduceConstructor(
            nameof(this.MementoConstructorTemplate),
            buildConstructor: b =>
            {
                foreach ( var field in fields )
                {
                    b.AddParameter( field.Snapshot.Name, field.Snapshot.Type );
                }
            } );

        builder.Tags = new Tags( snapshotType.Declaration, fields );
    }

    [Introduce]
    public object Save()
    {
        var tags = (Tags) meta.Tags.Source!;

#pragma warning disable CS0618 // Type or member is obsolete
        return tags.SnapshopType.Constructors.Single().Invoke( tags.Fields.Select( f=>f.Source ) )!;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Introduce]
    public void Restore( object snapshot )
    {
        var tags = (Tags) meta.Tags.Source!;
        var typedSnapshot = (IExpression) meta.Cast( tags.SnapshopType, snapshot );

        foreach ( var field in tags.Fields )
        {
            field.Source.Value = field.Snapshot.With( typedSnapshot ).Value;
        }
    }

    [Template]
    public void MementoConstructorTemplate()
    {
        var tags = (Tags) meta.Tags.Source!;

        var i = meta.CompileTime( 0 );

        foreach ( var parameter in meta.Target.Constructor.Parameters )
        {
            tags.Fields[i].Snapshot.Value = parameter.Value;
            i++;
        }
    }
}

// <target>
[Memento]
public class Vehicle
{
    public string Name { get; }
    public decimal Payload { get; set; }
    public string Fuel { get; set; }

    public Vehicle( string name, decimal payload, string fuel )
    {
        Name = name;
        Payload = payload;
        Fuel = fuel;
    }
}