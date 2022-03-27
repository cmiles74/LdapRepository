using System.Collections.Generic;
using Nervestaple.WebService.Models.security;
using Newtonsoft.Json;

namespace Nervestaple.LdapRepository.Models
{
    /// <summary>
    /// Models an LDAP Account instance
    /// </summary>
    public class LdapAccount : Account
    {
        /// <summary>
        /// List of associated group distinguished names
        /// </summary>
        private List<string> _groupDistinguishedNames;
        
        /// <inheritdoc />
        public LdapAccount()
        {
            
        }
        
        /// <inheritdoc />
        public LdapAccount(Account account) : base(account)
        {
            
        }

        /// <inheritdoc />
        public LdapAccount(string id, string fullName, string mail, List<string> roles) 
            : base(id, fullName, mail, roles)
        {
            
        }
        
        /// <summary>
        /// The sAMAccountName of the account
        /// </summary>
        public string SAMAccountName { get; set; }

        /// <summary>
        /// Distinguished name for this account
        /// </summary>
        [JsonIgnore]
        public string DistinguishedName { get; set; }

        /// <summary>
        /// List of distinguished names of the groups for which this account is
        /// a member
        /// </summary>
        [JsonIgnore]
        public List<string> GroupDistinguishedNames
        {
            get { return _groupDistinguishedNames; }
            set
            {
                _groupDistinguishedNames = value;
                
                // populate roles
                List<string> groups = new List<string>();
                foreach (var dn in GroupDistinguishedNames)
                {
                    groups.Add(dn.Split(',')[0].Substring(3));
                }

                Roles = groups;
            }
        }
    }
}