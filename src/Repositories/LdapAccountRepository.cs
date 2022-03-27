using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nervestaple.LdapRepository.Configuration;
using Nervestaple.LdapRepository.Models;
using Nervestaple.WebService.Models.security;
using Nervestaple.WebService.Repositories.security;
using Novell.Directory.Ldap;

namespace Nervestaple.LdapRepository.Repositories
{
    /// <summary>
    /// Provides a repository for LDAP account information
    ///
    /// Note: the asynchronous methods are here for convenience, in practice
    /// only the searching the LDAP tree four sets of data can be handled in a
    /// truly asynchronous manner and those functions are not provided by
    /// this repository.
    /// </summary>
    public class LdapAccountRepository : IAccountRepository, IDisposable
    {
        /// <summary>
        /// Returns true if this repository has been disposed.
        /// <returns>
        /// true if this repository has been disposed
        /// </returns>
        /// </summary>
        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        /// <summary>
        /// LDAP configuration used to communicate with the LDAP server
        /// </summary>
        private readonly LdapConfiguration _ldapConfiguration;

        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instances and sets it's configuration
        /// </summary>
        /// <param name="ldapConfiguration">Configuration for the new instance</param>
        /// <param name="logger">Logger instance</param>
        public LdapAccountRepository(ILogger<LdapAccountRepository> logger, LdapConfiguration ldapConfiguration)
        {
            _logger = logger;
            _ldapConfiguration = ldapConfiguration;
        }

        /// <inheritdoc/>
        public Account Authenticate(IAccountCredentials credentials)
        {
            return AuthenticateLdapAccount(credentials);
        }

        /// <inheritdoc/>
        public Task<Account> AuthenticateAsync(IAccountCredentials credentials) {
            return Task.FromResult(Authenticate(credentials));
        }

        /// <summary>
        /// Fetches the account with the matching credentials or returns null
        /// if no account with matching credentials can be found
        /// </summary>
        /// <param name="credentials">Credentials used when searching</param>
        /// <returns>Matching account instance</returns>
        public LdapAccount AuthenticateLdapAccount(IAccountCredentials credentials)
        {
            string identifier = "uid";     // default LDAP unique ID attribute
            string accountName = credentials.Id;
            
            // if we have a domain name, we use sAMAccountName instead of uid for our id attribute
            if (!String.IsNullOrEmpty(_ldapConfiguration.domainName))
            {
                identifier = "sAMAccountName";
                accountName = _ldapConfiguration.domainName + "\\" + credentials.Id;
            }

            LdapAccount ldapAccount = null;
            try
            {
                using (LdapConnection connection = Connect(_ldapConfiguration))
                {
                    // bind as the provided account
                    connection.Bind(accountName, credentials.Password);

                    if (connection.Bound)
                    {
                        ldapAccount = FindAccount(connection, identifier, credentials.Id);
                    }
                }
            }
            catch (LdapException exception)
            {
                _logger.LogWarning("Failed to authenticate " + 
                                   credentials.Id + ": " + exception.Message);
            }

            return ldapAccount;
        }

        /// <inheritdoc/>
        public Account Find(string id)
        {
            return FindLdapAccount(id);
        }

        /// <inheritdoc/>
        public Task<Account> FindAsync(string id) {
            return Task.FromResult(Find(id));
        }

        /// <summary>
        /// Returns the account with the matching unique identifier
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <returns>Matching account</returns>
        public LdapAccount FindLdapAccount(string id)
        {
            string accountName = _ldapConfiguration.ReadOnlyDn;
            
            LdapAccount ldapAccount = null;
            try
            {
                using (LdapConnection connection = Connect(_ldapConfiguration))
                {
                    // bind as the read only account
                    connection.Bind(accountName, _ldapConfiguration.ReadOnlyPassword);

                    if (connection.Bound) {
                        
                        // account to find
                        string identifierFind = "uid";
                        string accountNameFind = id;
                        
                        // if we have a domain name, we use sAMAccountName instead of uid for our id attribute
                        if (!String.IsNullOrEmpty(_ldapConfiguration.domainName))
                        {
                            identifierFind = "sAMAccountName";
                            accountNameFind = _ldapConfiguration.domainName + "\\" + id;
                        }

                        ldapAccount = FindAccount(connection, identifierFind, id);
                    }
                }
            }
            catch (LdapException exception)
            {
                _logger.LogWarning("Failed to find " + id + ": " + exception.Message);
            }

            return ldapAccount;
        }

        /// <summary>
        /// Returns the unique account matching the provided unique identifier
        /// and populates all of its roles
        /// </summary>
        /// <param name="connection">LDAP connection</param>
        /// <param name="identifier">Name of the server's ID attribute</param>
        /// <param name="id">Unique account identifier</param>
        /// <returns>Distinguished name for the account</returns>
        private LdapAccount FindAccount(LdapConnection connection, string identifier, string id)
        {
            // get the account's DN
            LdapAccount ldapAccount = QueryForAccount(connection, identifier, id);

            // get groups for the DN
            List<string> groups = QueryForGroupDns(connection, ldapAccount.DistinguishedName);
            ldapAccount.GroupDistinguishedNames = groups;

            return ldapAccount;
        }
        
        /// <summary>
        /// Returns the unique account matching the provided unique identifier
        /// </summary>
        /// <param name="connection">LDAP connection</param>
        /// <param name="identifier">Name of the server's ID attribute</param>
        /// <param name="accountId">Unique account identifier</param>
        /// <returns>Distinguished name for the account</returns>
        private LdapAccount QueryForAccount(LdapConnection connection, string identifier, string accountId)
        {
            LdapAccount ldapAccount = new LdapAccount();
            ldapAccount.Id = accountId;
            
            string objectClass = "user";
            if (!String.IsNullOrEmpty(_ldapConfiguration.userClass))
            {
                objectClass = _ldapConfiguration.userClass;
            }
            
            if (!String.IsNullOrEmpty(_ldapConfiguration.domainName))
            {
                ldapAccount.SAMAccountName = ldapAccount.Id;
            }
            
            var results = connection.Search(
                _ldapConfiguration.accountSearchBase,
                LdapConnection.ScopeSub,
                "(&(objectClass=" + objectClass + ")(" + identifier + "=" + accountId +"))",
                null,
                false,
                new LdapSearchConstraints());

            // we should only have one result
            while (results.HasMore())
            {
                var entry = results.Next();
                var attributes = entry.GetAttributeSet();
                
                if (attributes.ContainsKey("distinguishedName")) {
                    ldapAccount.DistinguishedName = entry.GetAttribute("distinguishedName").StringValue;
                }
                
                if (attributes.ContainsKey("cn")) {
                    ldapAccount.FullName = entry.GetAttribute("cn").StringValue;
                }
                
                if (attributes.ContainsKey("mail")) {
                    ldapAccount.Mail = entry.GetAttribute("mail").StringValue;
                }
            }

            return ldapAccount;
        }

        /// <summary>
        /// Returns a list of distinguished names for the groups for which the
        /// account with the provided distinguished name is a member
        /// </summary>
        /// <param name="connection">LDAP connection</param>
        /// <param name="dn">Account's distinguished name</param>
        /// <returns>List of group DN values</returns>
        private List<string> QueryForGroupDns(LdapConnection connection, string dn)
        {
            List<string> groups = new List<string>();
            
            var results = connection.Search(
                _ldapConfiguration.groupSearchBase,
                LdapConnection.ScopeSub,
                "(member:1.2.840.113556.1.4.1941:=" + dn +")",
                null,
                false,
                new LdapSearchConstraints());
                    
            while (results.HasMore())
            {
                var entry = results.Next();
                groups.Add(entry.GetAttribute("distinguishedName").StringValue);
            }

            return groups;
        }

        /// <summary>
        /// Returns a new LDAP connection for the server specified with the
        /// provided configuration
        /// </summary>
        /// <param name="ldapConfiguration">LDAP configuration</param>
        /// <returns>Active LDAP connection</returns>
        private LdapConnection Connect(LdapConfiguration ldapConfiguration)
        {
            LdapConnection ldapConnection = new LdapConnection();
            ldapConnection.SecureSocketLayer = ldapConfiguration.useSsl;
            ldapConnection.Connect(ldapConfiguration.serverName, ldapConfiguration.serverPort);
            return ldapConnection;
        }
        
        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing) {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // do nothing
                }

                _disposedValue = true;
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}