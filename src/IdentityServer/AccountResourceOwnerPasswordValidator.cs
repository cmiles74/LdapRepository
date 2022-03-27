using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Nervestaple.WebService.Models.security;
using Nervestaple.WebService.Services.security;

namespace Nervestaple.LdapRepository.IdentityServer {
    /// <summary>
    /// Provides a resource owner password validator that is backed by our
    /// LDAP account service
    /// </summary>
    public class AccountResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator {
        /// <summary>
        /// Account service for finding and authenticating account information
        /// </summary>
        private readonly IAccountService _accountService;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="accountService">backing account service</param>
        public AccountResourceOwnerPasswordValidator(IAccountService accountService) {
            _accountService = accountService;
        }
        
        /// <inheritdoc />
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, 
                "Invalid account name and password combination");
            
            var account = await _accountService.AuthenticateAsync(new SimpleAccountCredentials {
                Id = context.UserName,
                Password = context.Password
            });

            if (account != null) {
                context.Result = new GrantValidationResult (
                    account.Id, 
                    "ldap", 
                    GetAccountClaims(account));
            }
        }

        /// <summary>
        /// Returns a set of claims for the provided account, these claims are
        /// fetched from the backing account service
        /// </summary>
        /// <param name="account">account name</param>
        /// <returns>claims for the account</returns>
        public static IList<Claim> GetAccountClaims(Nervestaple.WebService.Models.security.Account account) {
            var claims = new List<Claim>();

            claims.Add(new Claim("user_id", account.Id));
            claims.Add(new Claim(JwtClaimTypes.Name, account.FullName));
            claims.Add(new Claim(JwtClaimTypes.Email, account.Mail));
            
            foreach (var role in account.Roles) {
                claims.Add(new Claim(JwtClaimTypes.Role, role));
            }
            
            return claims;
        }
    }
}