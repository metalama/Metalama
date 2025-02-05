using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug31128
{
    [Inheritable]
    public sealed class BusinessObjectModelImplementationAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            if (builder == null)
            {
                throw new ArgumentNullException( nameof(builder) );
            }

            base.BuildAspect( builder );

            if (builder.Target.IsConvertibleTo( typeof(BusinessObjectModel<>) ))
            {
                return;
            }

            builder.IntroduceMethod( nameof(CreateColumns), whenExists: OverrideStrategy.Override );

            foreach (var property in GetDataClassProperties( builder.Target ))
            {
                builder.IntroduceProperty(
                    name: property.Name,
                    getTemplate: nameof(GetColumn),
                    setTemplate: null,
                    args: new { columnName = property.Name } );
            }
        }

        [Template( Accessibility = Accessibility.Public )]
        private BusinessObjectModelColumn GetColumn( [CompileTime] string columnName )
        {
            return meta.This.Columns[columnName];
        }

        [Template( Accessibility = Accessibility.Protected )]
        private IList<BusinessObjectModelColumn> CreateColumns()
        {
            var columns = meta.Proceed()!;

            foreach (var property in GetDataClassProperties( meta.Target.Type ))
            {
                columns.Add(
                    property.Attributes.Any( a => a.Type.IsConvertibleTo( typeof(KeyAttribute) ) )
                        ? new BusinessObjectModelColumn( property.Name ) { VisibleInDetailView = false }
                        : new BusinessObjectModelColumn( property.Name ) );
            }

            return columns;
        }

        private IList<IProperty> GetDataClassProperties( INamedType type )
        {
            var result = new List<IProperty>();

            IGeneric baseType = type.BaseType!;
            var xpoType = (INamedType)baseType.TypeArguments[0];

            foreach (var property in xpoType.Properties)
            {
                if (property.Accessibility == Accessibility.Public)
                {
                    result.Add( property );
                }
            }

            return result;
        }
    }

    public interface ITypesInfo { }

    public interface IBusinessObjectModel
    {
        void CustomizeTypesInfo( ITypesInfo typesInfo );
    }

    [BusinessObjectModelImplementation]
    public abstract partial class BusinessObjectModel<T> : IBusinessObjectModel where T : class
    {
        public IDictionary<string, BusinessObjectModelColumn> Columns => throw new NotImplementedException();

        public void CustomizeTypesInfo( ITypesInfo typesInfo ) { }
    }

    public sealed class BusinessObjectModelColumn
    {
        public bool VisibleInDetailView { get; init; }

        public BusinessObjectModelColumn( string name ) { }
    }

    public sealed class KeyAttribute : Attribute
    {
        public KeyAttribute( bool b ) { }
    }

    public sealed class UsersLoginInfo
    {
        [Key( true )]
        public int Id { get; init; }

        public string? ProviderUserKey { get; set; }
    }

    // <target>
    public sealed partial class UsersLoginInfoModel : BusinessObjectModel<UsersLoginInfo>
    {
        public UsersLoginInfoModel() { }
    }
}