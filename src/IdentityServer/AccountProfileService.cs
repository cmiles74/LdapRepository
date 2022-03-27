using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using Nervestaple.WebService.Services.security;

namespace Nervestaple.LdapRepository.IdentityServer {
    /// <summary>
    /// Provides a profile service backed by our LDAP account service
    /// </summary>
    public class AccountProfileService : IProfileService {
        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger _logger;
        
        /// <summary>
        /// Account service for finding and authenticating account information
        /// </summary>
        private readonly IAccountService _accountService;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="logger">logger instance</param>
        /// <param name="accountService">the backing account service</param>
        public AccountProfileService(ILogger<AccountProfileService> logger, IAccountService accountService) {
            _logger = logger;
            _accountService = accountService;
        }
        
        /// <inheritdoc />
        public async Task GetProfileDataAsync(ProfileDataRequestContext context) {
            // the identity claim set in the password validator
            var userIdClaim  = context.Subject.Claims.FirstOrDefault(x => x.Type == "sub");
            
            try
            {
                if (!string.IsNullOrEmpty(userIdClaim?.Value)) {
                    var user = await _accountService.FindAsync(userIdClaim.Value);

                    // issue the claims for the user
                    if (user != null) {
                        var claims = AccountResourceOwnerPasswordValidator.GetAccountClaims(user);

                        context.IssuedClaims = claims.Where(x => context.RequestedClaimTypes.Contains(x.Type)).ToList();
                    }
                }
            } catch (Exception exception) {
                _logger.LogWarning("Couldn't get profile data for account with subject name \"" + 
                                context.Subject.Identity.Name + "\" or \"sub\" claim " + userIdClaim + "\": " +
                                exception.Message);
            }
        }

        /// <inheritdoc />
        public async Task IsActiveAsync(IsActiveContext context) {
            //get subject from context (set in ResourceOwnerPasswordValidator.ValidateAsync),
            var userIdClaim = context.Subject.Claims.FirstOrDefault(x => x.Type == "sub");
            
            try {
                if (!string.IsNullOrEmpty(userIdClaim?.Value)) {
                    var account = await _accountService.FindAsync(userIdClaim.Value);

                    if (account != null) {
                        context.IsActive = true;
                    }
                }
            } catch (Exception exception) {
                _logger.LogWarning("Couldn't get validate account as active with subject name \"" + 
                                context.Subject.Identity.Name + "\" or \"sub\" claim " + userIdClaim + "\": " +
                                exception.Message);
            }
        }
    }
}