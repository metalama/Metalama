using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace ProjectWithMetalama20251;

[Inheritable]
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AspectState = new AspectState( builder.Target.ToRef(), builder.Target.ToSerializableId(),
            ((IType) builder.Target).ToSerializableId() );
    }

    private class AspectState : IAspectState
    {
        public IRef<INamedType> TypeRef { get; }
        public SerializableDeclarationId SerializableDeclarationId { get; }
        public SerializableTypeId SerializableTypeId { get; }

        public AspectState( IRef<INamedType> typeRef, SerializableDeclarationId serializableDeclarationId, SerializableTypeId serializableTypeId )
        {
            this.TypeRef = typeRef;
            this.SerializableDeclarationId = serializableDeclarationId;
            this.SerializableTypeId = serializableTypeId;
        }
    }
}