
using System;
using Npgsql;


/// <summary>
/// Extension methods used to normalize PostgreSQL connection strings before they reach any Npgsql consumer.
/// </summary>
public static class NpgsqlConnectionStringExtensions
{
    extension(string connectionString)
    {
        /// <summary>
        /// Forces <see cref="GssEncryptionMode.Disable"/> on <paramref name="connectionString"/>.
        /// </summary>
        /// <remarks>
        /// Npgsql defaults to <see cref="GssEncryptionMode.Prefer"/>, which dlopens <c>libgssapi_krb5.so.2</c>; that library ships in no container image used here and the failed load aborts the process.
        /// </remarks>
        /// <param name="connectionString">The connection string to normalize.</param>
        /// <returns>The connection string with GSS encryption explicitly disabled.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> is <see langword="null"/>.</exception>
        public string WithGssDisabled()
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            return new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable }.ConnectionString;
        }
    }
}