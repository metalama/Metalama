// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;

namespace Metalama.Backstage.Telemetry
{
    // Warning: this file is linked to UserInterface solution. We need to serialize
    // exceptions from debugging server in the same way as ExceptionPackager does without
    // referencing PostSharp.Compiler.Settings.
    [PublicAPI]
    public static class ExceptionXmlFormatter
    {
        // Exception.Data is an arbitrary, developer-populated bag that may hold secrets, connection
        // strings or PII. We therefore redact the value of every key by default and only serialize the
        // values of keys explicitly allow-listed here as known-safe. The (scrubbed) key is always emitted
        // so reviewers can see which entries were present. This list is intentionally conservative. See #1680.
        private static readonly HashSet<string> _allowedDataKeys = new( StringComparer.OrdinalIgnoreCase );

        private static bool IsAllowedDataKey( string? key ) => key != null && _allowedDataKeys.Contains( key );

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
                    var key = data.Value.Key.ToString();
                    writer.WriteElementString( "Key", ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( key ) );

                    if ( data.Value.Value != null )
                    {
                        if ( IsAllowedDataKey( key ) )
                        {
                            WriteDataValue( writer, data.Value.Value );
                        }
                        else
                        {
                            // The key is not on the allow-list: redact the value rather than serializing it. See #1680.
                            writer.WriteElementString( "Value", "#redacted" );
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

        private static void WriteDataValue( XmlWriter writer, object value )
        {
            switch ( value )
            {
                case Array array:
                    {
                        writer.WriteStartElement( "Array" );

                        for ( var i = 0; i < array.Length; i++ )
                        {
                            var item = array.GetValue( i );

                            switch ( item )
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
                                        ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( item.ToString() ) );

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
                    writer.WriteElementString( "Value", ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( value.ToString() ) );

                    break;
            }
        }
    }
}
