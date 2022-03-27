using System.IO;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nervestaple.LdapRepository.Helpers;
using Nervestaple.WebService.Models.security;
using Nervestaple.WebService.Services.security;
using Newtonsoft.Json.Linq;

namespace Nervestaple.LdapRepositoryTest {
    [TestClass]
    public class LdapRepositoryTest {
        private static ServiceProvider _serviceProvider;
        private static ILogger _logger;
        private static IContainerService _ldapServer;

        [ClassInitialize]
        public static void Setup(TestContext testContext)
        {
            _ldapServer = new Builder().UseContainer()
                .UseImage("rroemhild/test-openldap")
                .ExposePort(10389, 10389)
                .ExposePort(10636, 10636)
                .WaitForPort("10389/tcp", 30000)
                .Build()
                .Start();
            
            IServiceCollection serviceCollection = new ServiceCollection()
                .AddLogging(b => {
                    b.AddConsole();
                    b.AddDebug();
                    b.SetMinimumLevel(LogLevel.Trace);
                });
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            LdapRepositoryStartupHelper.ConfigureLdapAuthentication(configuration, serviceCollection);
            
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // get a logger instance
            _logger = _serviceProvider.GetService<ILogger<LdapRepositoryTest>>();
        }

        [ClassCleanup]
        public static void Teardown()
        {
            _ldapServer.Dispose();
        }
        
        [TestMethod]
        public void TestAuthenticate() {
            var configuration = JObject.Parse(File.ReadAllText("appsettings.json"));
            var userDn = configuration["Ldap"]["ReadOnlyDn"].ToString();
            var password = configuration["Ldap"]["ReadOnlyPassword"].ToString();
            var service = _serviceProvider.GetService<IAccountService>();
            var account = service.Authenticate(new SimpleAccountCredentials {
                Id = userDn,
                Password = password
            });
            Assert.IsNotNull(account);
        }

        [TestMethod]
        public void TestFind() {
            var service = _serviceProvider.GetService<IAccountService>();
            var account = service.Find("fry");
            Assert.IsNotNull(account.Mail);
        }
    }
}