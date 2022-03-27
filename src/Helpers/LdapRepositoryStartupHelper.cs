using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nervestaple.LdapRepository.Configuration;
using Nervestaple.LdapRepository.Repositories;
using Nervestaple.WebService.Repositories.security;
using Nervestaple.WebService.Services.security;

namespace Nervestaple.LdapRepository.Helpers
{
    /// <summary>
    /// Provides helper methods to make it easier to configure your
    /// application for an LDAP repository.
    /// </summary>
    public class LdapRepositoryStartupHelper
    {
        /// <summary>
        /// Configures an account service that is backed by an LDAP repository.
        /// This method presumes that you have and section of your configuration
        /// called "Ldap" with the appropriate settings.
        /// </summary>
        /// <param name="configuration">Configuration instance</param>
        /// <param name="services">Application services</param>
        public static void ConfigureLdapAuthentication(
            IConfiguration configuration, IServiceCollection services)
        {
            // configuration
            LdapConfiguration ldapConfiguration = new LdapConfiguration();
            configuration.Bind("Ldap", ldapConfiguration);
            services.AddSingleton(ldapConfiguration);
            
            // repository and service
            services.AddTransient<IAccountRepository, LdapAccountRepository>();
            services.AddTransient<IAccountService, AccountService>();
        }
    }
}