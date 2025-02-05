using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0169, CS0649

namespace Metalama.Framework.Tests.AspectTests.Aspects.Samples.Memento
{
    public class MementoAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var mementoType =
                builder.IntroduceClass(
                    "Memento",
                    buildType: b => { b.Accessibility = Accessibility.Public; } );

            var mementoFields = new List<IField>();

            foreach (var fieldOrProperty in builder.Target.FieldsAndProperties)
            {
                if (fieldOrProperty is not { IsAutoPropertyOrField: true, IsImplicitlyDeclared: false })
                {
                    continue;
                }

                var field = mementoType.IntroduceField(
                    nameof(MementoField),
                    buildField: b =>
                    {
                        b.Name = fieldOrProperty.Name;
                        b.Type = fieldOrProperty.Type;
                        b.Accessibility = Accessibility.Public;
                        b.Writeability = Writeability.ConstructorOnly;
                    } );

                mementoFields.Add( field.Declaration );
            }

            mementoType.ImplementInterface( typeof(IMemento) );

            mementoType.IntroduceConstructor(
                nameof(MementoConstructorTemplate),
                buildConstructor: b =>
                {
                    foreach (var mementoField in mementoFields)
                    {
                        b.AddParameter( mementoField.Name, mementoField.Type );
                    }
                },
                args: new { fields = mementoFields } );

            builder.ImplementInterface( typeof(IOriginator), tags: new { mementoType = mementoType.Declaration } );
        }

        [Template]
        private object? MementoField;

        [InterfaceMember]
        public IMemento Save()
        {
            var mementoType = (INamedType)meta.Tags["mementoType"];
            var fieldExpressions = meta.Target.Type.FieldsAndProperties.Where( f => f.IsAutoPropertyOrField == true && !f.IsImplicitlyDeclared );

            return mementoType.Constructors.Single().Invoke( fieldExpressions );
        }

        [InterfaceMember]
        public void Restore( IMemento memento )
        {
            var mementoType = (INamedType)meta.Tags["mementoType"];

            foreach (var fieldOrProperty in meta.Target.Type.FieldsAndProperties.Where( f => f.IsAutoPropertyOrField == true && !f.IsImplicitlyDeclared ))
            {
                var mementoField = mementoType.FieldsAndProperties.OfName( fieldOrProperty.Name ).Single();

                fieldOrProperty.Value = mementoField.With( (IExpression)meta.Cast( mementoType, memento ) ).Value;
            }
        }

        [Template]
        public void MementoConstructorTemplate( [CompileTime] List<IField> fields )
        {
            var i = meta.CompileTime( 0 );

            foreach (var parameter in meta.Target.Constructor.Parameters)
            {
                fields[i].Value = parameter;
                i++;
            }
        }
    }

    public interface IOriginator
    {
        IMemento Save();

        void Restore( IMemento memento );
    }

    public interface IMemento { }

    // <target>
    [Memento]
    internal class TargetClass
    {
        private int _state1;

        private int State2 { get; set; }
    }
}