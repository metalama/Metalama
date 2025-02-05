// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections;
using System.Xml;

namespace Metalama.Backstage.Telemetry
{
    // Warning: this file is linked to UserInterface solution. We need to serialize
    // exceptions from debugging server in the same way as ExceptionPackager does without
    // referencing PostSharp.Compiler.Settings.
    public static class ExceptionXmlFormatter
    {
        public static void WriteException( XmlWriter writer, Exception e )
        {
            writer.WriteElementString( "Type", ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( e.GetType().FullName ) );
            writer.WriteElementString( "Message", ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( e.Message ) );
            writer.WriteElementString( "Source", ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( e.Source ) );

            writer.WriteStartElement( "Data" );

            foreach ( DictionaryEntry? data in e.Data )
            {
                writer.WriteStartElement( "Item" );

                if ( data != null )
                {
                    writer.WriteElementString( "Key", ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( data.Value.Key.ToString() ) );

                    if ( data.Value.Value != null )
                    {
                        switch ( data.Value.Value )
                        {
                            case Array array:
                                {
                                    writer.WriteStartElement( "Array" );

                                    for ( var i = 0; i < array.Length; i++ )
                                    {
                                        var value = array.GetValue( i );

                                        switch ( value )
                                        {
                                            case Exception exception:
                                                writer.WriteStartElement( "Item" );
                                                WriteException( writer, exception );
                                                writer.WriteEndElement();

                                                break;

                                            case null:
                                                writer.WriteElementString( "Item", "<null>" );

                                                break;

                                            default:
                                                writer.WriteElementString(
                                                    "Item",
                                                    ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( value.ToString() ) );

                                                break;
                                        }
                                    }

                                    writer.WriteEndElement();

                                    break;
                                }

                            case Exception exception:
                                writer.WriteStartElement( "Value" );
                                WriteException( writer, exception );
                                writer.WriteEndElement();

                                break;

                            default:
                                writer.WriteElementString( "Value", ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( data.Value.ToString() ) );

                                break;
                        }
                    }
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteElementString(
                "StackTrace",
                Environment.NewLine + ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( e.StackTrace ) + Environment.NewLine );

            if ( e.InnerException != null )
            {
                writer.WriteStartElement( "InnerException" );
                WriteException( writer, e.InnerException );
                writer.WriteEndElement();
            }

            if ( e is AggregateException aggregate )
            {
                writer.WriteStartElement( "InnerExceptions" );

                foreach ( var innerException in aggregate.InnerExceptions )
                {
                    writer.WriteStartElement( "Exception" );
                    WriteException( writer, innerException );
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }
    }
}