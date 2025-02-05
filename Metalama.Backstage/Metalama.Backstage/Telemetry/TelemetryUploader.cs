// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Tools;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IHttpClientFactory = Metalama.Backstage.Infrastructure.IHttpClientFactory;
using RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

namespace Metalama.Backstage.Telemetry
{
    internal sealed class TelemetryUploader : ITelemetryUploader
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStandardDirectories _directories;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDateTimeProvider _time;
        private readonly ILogger _logger;
        private readonly Uri _requestUri = new( "https://bits.postsharp.net:44301/upload" );
        private readonly IConfigurationManager _configurationManager;
        private readonly IExceptionReporter _exceptionReporter;
        private readonly List<(string File, Exception Reason)> _failedFiles = [];
        private readonly TelemetryLogger _telemetryLogger;

        public TelemetryUploader( IServiceProvider serviceProvider )
        {
            this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();

            this._serviceProvider = serviceProvider;
            this._directories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();
            this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
            this._httpClientFactory = serviceProvider.GetRequiredBackstageService<IHttpClientFactory>();
            this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
            this._logger = serviceProvider.GetLoggerFactory().Telemetry();
            this._exceptionReporter = serviceProvider.GetRequiredBackstageService<IExceptionReporter>();
            this._telemetryLogger = serviceProvider.GetRequiredBackstageService<TelemetryLogger>();
        }

        private static void CopyStream( Stream inputStream, Stream outputStream )
        {
            const int bufferLen = 16 * 1024;
            var buffer = new byte[bufferLen];
            int bytesRead;

            while ( (bytesRead = inputStream.Read( buffer, 0, bufferLen )) > 0 )
            {
                outputStream.Write( buffer, 0, bytesRead );
            }
        }

        private void EncryptFile( string inputFile, string outputFile )
        {
            using ( var inputStream = this._fileSystem.OpenRead( inputFile ) )
            using ( var outputStream = this._fileSystem.CreateFile( outputFile ) )
            {
                EncryptStream( inputStream, outputStream );
            }
        }

        private static void EncryptStream( Stream inputStream, Stream outputStream )
        {
            var cryptoStream = GetCryptoStream( outputStream );
            CopyStream( inputStream, cryptoStream );
            cryptoStream.FlushFinalBlock();
        }

        private static CryptoStream GetCryptoStream( Stream outputStream )
        {
            // Create a symmetric random key.
            var symmetricKey = new byte[256 / 8];
            var randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes( symmetricKey );

            // Retrieve the public key.
            byte[] publicKey;
            string publicKeyXml;

            using (
                var keyStream = typeof(TelemetryUploader)
                    .Assembly
                    .GetManifestResourceStream( "Metalama.Backstage.Telemetry.public.key" ) )
            {
                if ( keyStream == null )
                {
                    throw new InvalidOperationException( "Public key not found." );
                }

                publicKey = new byte[keyStream.Length];
                var totalBytesRead = 0;

                while ( totalBytesRead < keyStream.Length )
                {
                    totalBytesRead += keyStream.Read( publicKey, totalBytesRead, (int) keyStream.Length - totalBytesRead );
                }

                keyStream.Position = 0;

                using var keyReader = new StreamReader( keyStream );
                publicKeyXml = keyReader.ReadToEnd();
            }

            // Compute a hash of the public key.
            var sha = SHA512.Create();
            var publicKeyHash = sha.ComputeHash( publicKey );

            // Encrypt the random key using the public key.
            using var rsa = RSA.Create();
            rsa.FromXmlString( publicKeyXml );

            var encryptedSymmetricKey = rsa.Encrypt( symmetricKey, RSAEncryptionPadding.Pkcs1 );

            var aes = Aes.Create();
            aes.GenerateIV();

            var writer = new BinaryWriter( outputStream );

            // Write the version.
            // 0 = Windows-specific PostSharp implementation.
            // 1 = Metalama multi-platform implementation. 
            writer.Write( 1 );

            // Write the public key hash.
            writer.Write( publicKeyHash.Length );
            writer.Write( publicKeyHash );

            // Write the encrypted key
            writer.Write( encryptedSymmetricKey.Length );
            writer.Write( encryptedSymmetricKey );

            // Write the initial vector.
            writer.Write( aes.IV.Length );
            writer.Write( aes.IV );

            // Encrypt the package content.
            return new CryptoStream(
                outputStream,
                aes.CreateEncryptor( symmetricKey, aes.IV ),
                CryptoStreamMode.Write );
        }

        private bool TryCreatePackage( IReadOnlyList<string> files, string outputPath, out IReadOnlyList<string> filesToDelete )
        {
            var filesToDeleteLocal = new List<string>();
            string? tempPackagePath = null;
            Stream? packageStream = null;
            Package? package = null;

            try
            {
                foreach ( var file in files )
                {
                    this._logger.Trace?.Log( $"Packing '{file}'." );

                    // Attempt to open that file. Skip the file if we can't open it.
                    try
                    {
                        using ( var stream = this._fileSystem.Open( file, FileMode.Open, FileAccess.Read, FileShare.None ) )
                        {
                            // Create a ZIP package if it does not exist yet.
                            if ( package == null )
                            {
                                if ( packageStream != null )
                                {
                                    throw new InvalidOperationException( "Package stream has to be assigned along with package." );
                                }

                                this._logger.Trace?.Log( $"Creating package." );
                                tempPackagePath = this._fileSystem.GetTempFileName();
                                this._logger.Trace?.Log( $"The package is stored at '{tempPackagePath}'." );
                                packageStream = this._fileSystem.Open( tempPackagePath, FileMode.Create );
                                package = Package.Open( packageStream, FileMode.Create );
                            }

                            string? mime = null;

                            // Add the file to the zip.
                            this._logger.Trace?.Log( $"Adding '{file}' file to '{tempPackagePath}' package." );

                            var packagePart =
                                package.CreatePart(
                                    new Uri( "/" + Uri.EscapeDataString( Path.GetFileName( file ) ), UriKind.Relative ),
                                    mime ?? MediaTypeNames.Application.Octet,
                                    CompressionOption.Maximum );

                            // ReSharper disable once PossibleNullReferenceException
                            using ( var packagePartStream = packagePart.GetStream( FileMode.Create ) )
                            {
                                CopyStream( stream, packagePartStream );
                            }
                        }

                        filesToDeleteLocal.Add( file );

                        this._logger.Trace?.Log( $"'{file}' file added to '{tempPackagePath}' package." );
                    }
                    catch ( Exception e )
                    {
                        this._logger.Error?.Log( $"Cannot pack file '{file}': {e.Message}" );
                        this._failedFiles.Add( (file, e) );
                    }
                }

                filesToDelete = filesToDeleteLocal;

                if ( package == null )
                {
                    // We did not find any file.
                    this._logger.Trace?.Log( "No file found." );

                    return false;
                }

                this._logger.Trace?.Log( $"Closing '{tempPackagePath}' package." );
                package.Close();
                packageStream!.Close();

                // Encrypt the package.
                this._logger.Trace?.Log( $"Encrypting '{tempPackagePath}' package to '{outputPath}'." );
                this.EncryptFile( tempPackagePath!, outputPath );

                this._logger.Trace?.Log( $"'{outputPath}' package created." );

                return true;
            }
            finally
            {
                this._logger.Trace?.Log( $"Disposing temporary package stream." );
                packageStream?.Dispose();

                if ( tempPackagePath != null && this._fileSystem.FileExists( tempPackagePath ) )
                {
                    this._logger.Trace?.Log( $"Deleting temporary package '{tempPackagePath}'." );
                    this._fileSystem.DeleteFile( tempPackagePath );
                }
            }
        }

        /// <inheritdoc />
        public void StartUpload( bool force = false )
        {
            var toolExecutor = this._serviceProvider.GetBackstageService<IBackstageToolsExecutor>();

            if ( toolExecutor == null )
            {
                this._logger.Trace?.Log( $"Do not upload now because there is no IWorkerProgram service." );

                return;
            }

            var now = this._time.UtcNow;

            this._logger.Trace?.Log( "Acquiring mutex." );

            if ( !this._configurationManager.UpdateIf<TelemetryConfiguration>(
                    c => force ||
                         c.LastUploadTime == null ||
                         c.LastUploadTime.Value.AddDays( 1 ) < now,
                    c => c with { LastUploadTime = this._time.UtcNow } ) )
            {
                this._logger.Trace?.Log( $"It's not time to upload the telemetry yet." );

                return;
            }

            toolExecutor.Start( BackstageTool.Worker, "upload" );
        }

        internal static string ComputeHash( string packageName )
        {
            // ReSharper disable once StringLiteralTypo
            const string salt = @"<27e\)$a<=b9&zyVwjzaJ`!WW`rwHh~;Z5QAC.J5TQ`.NY"")]~FGA);AKSSmbV$M";

            var sha = SHA512.Create();
            var data = sha.ComputeHash( Encoding.UTF8.GetBytes( packageName + salt ) );
            var builder = new StringBuilder();

            foreach ( var t in data )
            {
                builder.Append( t.ToString( "x2", CultureInfo.InvariantCulture ) );
            }

            return builder.ToString();
        }

        /// <inheritdoc />
        public async Task UploadAsync()
        {
            if ( !this._fileSystem.DirectoryExists( this._directories.TelemetryUploadQueueDirectory ) )
            {
                this._logger.Trace?.Log(
                    $"The telemetry upload queue directory '{this._directories.TelemetryUploadQueueDirectory}' doesn't exist. Assuming there's nothing to upload." );

                return;
            }

            this._logger.Trace?.Log( $"Creating upload directory '{this._directories.TelemetryUploadPackagesDirectory}'" );
            this._fileSystem.CreateDirectory( this._directories.TelemetryUploadPackagesDirectory );

            var packageId = Guid.NewGuid().ToString();
            var packageName = packageId + ".psf";
            var packagePath = Path.Combine( this._directories.TelemetryUploadPackagesDirectory, packageName );

            IReadOnlyList<string> filesToDelete;

            try
            {
                var files = this._fileSystem.GetFiles( this._directories.TelemetryUploadQueueDirectory );

                if ( files.Length == 0 )
                {
                    this._logger.Trace?.Log( $"No files found to be uploaded in '{this._directories.TelemetryUploadQueueDirectory}'." );

                    return;
                }

                // TODO: Stream the data directly to HTTP
                if ( !this.TryCreatePackage( files, packagePath, out filesToDelete ) )
                {
                    return;
                }

                this._logger.Trace?.Log( "Preparing request content." );
                using var formData = new MultipartFormDataContent();

                this._logger.Trace?.Log( $"Adding '{packagePath}' package as '{packageName}', ID '{packageId}'." );

                // ReSharper disable once UseAwaitUsing
                using var packageFile = this._fileSystem.OpenRead( packagePath );
                var streamContent = new StreamContent( packageFile );
                formData.Add( streamContent, packageId, packageName );

                // ReSharper disable once StringLiteralTypo
                this._logger.Trace?.Log( $"Computing hash of '{packageName}'." );
                var check = ComputeHash( packageName );

                this._logger.Trace?.Log( $"Creating client." );
                using var client = this._httpClientFactory.Create();

                this._logger.Trace?.Log( $"Uploading." );
                var response = await client.PutAsync( $"{this._requestUri}?check={check}", formData );

                if ( !response.IsSuccessStatusCode )
                {
                    throw new InvalidOperationException( $"Request failed: {response.StatusCode} {response.ReasonPhrase}" );
                }

                this._telemetryLogger.WriteLine( $"Uploaded '{packageName}' containing: {string.Join( ", ", files.Select( f => $"'{f}'" ) )}" );
                this._logger.Trace?.Log( $"Upload succeeded." );
            }
            catch ( Exception exception )
            {
                this._logger.Error?.Log( exception.ToString() );
                this._telemetryLogger.WriteLine( $"Upload failure: {exception.Message}" );

                throw;
            }
            finally
            {
                RetryHelper.RetryWithLockDetection(
                    packagePath,
                    f =>
                    {
                        if ( this._fileSystem.FileExists( packagePath ) )
                        {
                            this._logger.Trace?.Log( $"Deleting '{packagePath}' package." );
                            this._fileSystem.DeleteFile( f );
                        }
                    },
                    this._serviceProvider,
                    logger: this._logger );

#if DEBUG
                var failedFileExceptions = new List<Exception>();
#endif

                foreach ( var failedFile in this._failedFiles )
                {
                    var exception = new TelemetryFilePackingFailedException(
                        $"Failed to pack '{failedFile.File}' telemetry file: {failedFile.Reason.Message}",
                        failedFile.Reason );

                    this._exceptionReporter.ReportException( exception );

#if DEBUG
                    failedFileExceptions.Add( exception );
#endif
                }

#if DEBUG
                if ( failedFileExceptions.Count > 0 )
                {
                    throw new AggregateException( "No all files have been packed. See inner exceptions for the failed files.", failedFileExceptions );
                }
#endif
            }

            // Delete the files that have just been sent.
            RetryHelper.RetryWithLockDetection(
                filesToDelete,
                f =>
                {
                    this._logger.Trace?.Log( $"Deleting sent file '{f}'." );
                    this._fileSystem.DeleteFile( f );
                },
                this._serviceProvider,
                logger: this._logger );

            this._logger.Trace?.Log( "Telemetry upload finished." );
        }
    }
}