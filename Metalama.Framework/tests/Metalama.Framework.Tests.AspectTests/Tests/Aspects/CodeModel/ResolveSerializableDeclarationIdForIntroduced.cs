using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced;
using System.Linq;

#pragma warning disable CS0067, CS0169, CS0649

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(SerializeAttribute), typeof(IntroduceMembersAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced;

internal class IntroduceMembersAttribute : TypeAspect
{
    [Template]
    private void M<T>( (int x, int y) p ) { }

    [Template]
    private int _field;

    [Template]
    private event EventHandler? Event;

    [Template]
    private int Property { get; set; }

    [Template]
    private int IndexerGet( int i ) => 0;

    [Template]
    private void IndexerSet( int i, int value ) { }

    [Template]
    public static bool NotOperator( dynamic x ) => false;

    [Template]
    public static int PlusOperator( dynamic x, dynamic y ) => 0;

    [Template]
    public static bool CastOperator( dynamic x ) => true;

    [Template]
    private void Finalizer() { }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceMethod( nameof(M) );
        builder.IntroduceField( nameof(_field) );
        builder.IntroduceEvent( nameof(Event) );
        builder.IntroduceProperty( nameof(Property) );
        builder.IntroduceIndexer( typeof(int), nameof(IndexerGet), nameof(IndexerSet) );

        builder.IntroduceUnaryOperator(
            nameof(NotOperator),
            builder.Target,
            TypeFactory.GetType( typeof(bool) ),
            OperatorKind.LogicalNot );

        builder.IntroduceBinaryOperator(
            nameof(PlusOperator),
            builder.Target,
            builder.Target,
            TypeFactory.GetType( typeof(int) ),
            OperatorKind.Addition );

        builder.IntroduceConversionOperator( nameof(CastOperator), builder.Target, TypeFactory.GetType( typeof(bool) ) );

        builder.IntroduceFinalizer( nameof(Finalizer) );

        builder.With( builder.Target.Constructors.First() ).IntroduceParameter( "x", typeof(int), TypedConstant.Create( 42 ) );
    }
}

internal class SerializeAttribute : TypeAspect
{
    [Introduce]
    private static string[] GetAllBuiltIds()
    {
        // This is mostly the output of the GetAllBuiltIds method from the GetSerializableDeclarationIdForIntroduced test, except for the namespace.
        var declarationIds = meta.CompileTime(
            new[]
            {
                "E:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.Event",
                "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C._field",
                "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C._field;PropertyGet",
                "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C._field;PropertyGetReturnParameter",
                "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C._field;PropertySet",
                "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C._field;PropertySetParameter",
                "F:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C._field;PropertySetReturnParameter",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.#ctor(System.Int32)",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.#ctor(System.Int32);Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.#ctor(System.String)",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.#ctor(System.String);Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.add_Event(System.EventHandler)",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.add_Event(System.EventHandler);Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.add_Event(System.EventHandler);Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.Finalize",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.Finalize;Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.get_Item(System.Int32)~System.Int32",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.get_Item(System.Int32)~System.Int32;Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.get_Item(System.Int32)~System.Int32;Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.get_Property~System.Int32",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.get_Property~System.Int32;Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32})",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32});Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32});Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.M``1(System.ValueTuple{System.Int32,System.Int32});TypeParameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Int32",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Int32;Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Int32;Parameter=1",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_Addition(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C,Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Int32;Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_Explicit(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Boolean",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_Explicit(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Boolean;Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_Explicit(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Boolean;Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_LogicalNot(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Boolean",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_LogicalNot(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Boolean;Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.op_LogicalNot(Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C)~System.Boolean;Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.remove_Event(System.EventHandler)",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.remove_Event(System.EventHandler);Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.remove_Event(System.EventHandler);Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.set_Item(System.Int32,System.Int32)",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.set_Item(System.Int32,System.Int32);Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.set_Item(System.Int32,System.Int32);Parameter=1",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.set_Item(System.Int32,System.Int32);Return",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.set_Property(System.Int32)",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.set_Property(System.Int32);Parameter=0",
                "M:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.set_Property(System.Int32);Return",
                "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.Item(System.Int32)",
                "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.Item(System.Int32);Parameter=0",
                "P:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C.Property",
                "T:Metalama.Framework.IntegrationTests.Aspects.CodeModel.ResolveSerializableDeclarationIdForIntroduced.C"
            } );

        var arrayBuilder = new ArrayBuilder( typeof(string) );

        foreach (var idString in declarationIds)
        {
            var id = new SerializableDeclarationId( idString );
            var declaration = id.Resolve( meta.Target.Compilation );

            arrayBuilder.Add( declaration.ToString() );
        }

        return arrayBuilder.ToValue();
    }
}

// <target>
[IntroduceMembers]
[Serialize]
internal class C
{
    private C() { }

    private C( string id ) : this() { }
}