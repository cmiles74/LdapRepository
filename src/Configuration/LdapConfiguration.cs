namespace Nervestaple.LdapRepository.Configuration
{
    /// <summary>
    /// Models the configuration data necessary to communicate with an LDAP
    /// server
    /// </summary>
    public class LdapConfiguration
    {
        /// <summary>
        /// The name of the LDAP server
        /// </summary>
        public string serverName { get; set; }

        /// <summary>
        /// The port used to communicate with the LDAP server
        /// </summary>
        public int serverPort { get; set; }

        /// <summary>
        /// Flag indicating if SSL should be used when communicating with
        /// the LDAP server
        /// </summary>
        public bool useSsl { get; set; }
        
        /// <summary>
        /// The name of the account class used by the LDAP server (i.e. "user"
        /// for Microsoft Active Directory or "inetOrgPerson" for OpenLDAP
        /// </summary>
        public string userClass { get; set; }

        /// <summary>
        /// Active Directory domain name (not used when communicating
        /// with an LDAP server that is _not_ an Active Directory server)
        /// </summary>
        public string domainName { get; set; }

        /// <summary>
        /// Search tree that contains all of the account names that will be
        /// used for authentication against the LDAP server
        /// </summary>
        public string accountSearchBase { get; set; }

        /// <summary>
        /// Search tree used to collect the groups for which a particular
        /// account is a member
        /// </summary>
        public string groupSearchBase { get; set; }
        
        /// <summary>
        /// Distinguished name of an account with read-only permissions to the
        /// LDAP server
        /// </summary>
        public string ReadOnlyDn { get; set; }
        
        /// <summary>
        /// Password for the account with read-only permissions to the LDAP
        /// server
        /// </summary>
        public string ReadOnlyPassword { get; set; }
    }
}