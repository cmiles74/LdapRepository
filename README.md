![Continuous Integration](https://github.com/cmiles74/LdapRepository/actions/workflows/ci.yml/badge.svg)

# Nervestaple.LDAPRepository

This library provides a repository for .Net Core applications that is backed by
data residing on an LDAP server. You would use this library to authenticate and
fetch the groups associated with accounts from that LDAP server. For instance,
you could use this repository to let your web application use account
information from a [Microsoft Active Directory Server][0] or any reasonable LDAP
server (like [OpenLDAP][1]).

If you are using IdentityServer and would like to use LDAP as an identity
provider, we also provide some glue code that will make that work.

## Configuration

Doing so is easy! First you need to add the package, then add the relevant 
information to your application's `appsettings.json` file. Here's a sample 
Active Directory configuration.

```json
{
  ...,
  "Ldap": {
    "ServerName": "my-ldap-server.com",
    "ServerPort": 636,
    "UseSsl": true,
    "DomainName": "test.local",
    "AccountSearchBase": "OU=Accounts,DC=test,DC=local",
    "GroupSearchBase": "OU=Security DC=test,DC=local",
    "ReadOnlyDn": "CN=Read Only,OU=Accounts,DC=test,DC=local",
    "ReadOnlyPassword": "********"
  }
 }
```

And here's a sample OpenLDAP configuration. :wink:

```json
{
  "Ldap": {
    "ServerName": "my-openldap-server.com",
    "ServerPort": 636,
    "UseSsl": true,
    "UserClass": "inetOrgPerson",
    "AccountSearchBase": "ou=people,dc=awesome,dc=com",
    "GroupSearchBase": "ou=people,dc=awsome,dc=com",
    "ReadOnlyDn": "cn=Ryan Reynolds,ou=people,dc=awesome,dc=com",
    "ReadOnlyPassword": "********"
  }
}
```

We need to provide a read-only account that has access to search the directory
to verify account information that has already been authenticated. In many
scenarios, the client will authenticate with the server and be provided a token
that is signed by the server. When that token is provided back to the server in
tandem with an action that requires authorization, the token is decrypted and
the server will fetch the matching user account to verify the roles. It is at
this step that the read-only user is required: it is used to fetch the matching
account.

## Add to Your Application

With the configuration set all you need to do is add it to your application's
`Startup.cs` file in the `ConfigureServices` method. We provided a helper 
method that will set that up for you.

```cs
LdapRepositoryStartupHelper.ConfigureLdapAuthentication(Configuration, services);
```

That method will register the LDAP repository and setup an `AccountService`
that your application service can use to authenticate and find accounts on the
configured LDAP server.

## IdentityServer

An IdentityServer project really isn't that much different from any other 
project, the same steps still apply. You will want to add the "Ldap" stanza to
your `appsettings.config` file with values that point to your LDAP server. You
will also want to add the call to the `LdapRepositoryStartupHelper` to the
`Startup.cs` file in your IdentityServer project.

With that in place the `AccountService` will be available to your application
and will be configured such that it can talk to your LDAP server. The next
step is to let IdentityServer know how to use it. We provide two classes in the
`Nervestaple.LdapRepository.IdentityServer` namespace that hook our service into
IdentityServer. You will want to add the following to your `Startup.cs` file
in the `ConfigureServices` method.

```cs
 var builder = services.AddIdentityServer(options =>
{
    // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
    options.EmitStaticAudienceClaim = true;
})
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryClients(Config.Clients)
    .AddProfileService<AccountProfileService>();  // <-- This is the important one!
```

Your options for IdentityServer may differ, the important part is the last line
where we plug in our profile service. This service will pull in our
`AccountService` to provide the profile information.

Next we tell IdentityServer how to validate passwords with our 
`AccountService`. Add the following to your `Startup.cs` in the 
`ConfigureServices` method.

```css
services.AddTransient<IResourceOwnerPasswordValidator, AccountResourceOwnerPasswordValidator>();
```

The `AccountResourceOwnerPasswordValidator` will use our `AccountService` to 
validate credentials provided to IdentityServer. With these two services in
place, you can use these services to validate accounts and fetch account 
information from your LDAP server.

Everyone's IdentityServer project varies from everyone else's, some more so 
than others. In general, you will be plugging the 
`AccountResourceOwnerPasswordValidator` service into your `AccountController`.
Typically you would inject the service by adding ut to the constructor of your
controller. In the `Login` method, you will want to use the credentials 
IdentityServer has collected and provide them to the 
`IResourceOwnerPasswordValidator` for validation.

The profile service will be used by IdentityServer all on it's own, you don't
need to make any additional changes to your project. It should just work! `:-D`

----

[0]: https://en.wikipedia.org/wiki/Active_Directory
[1]: https://en.wikipedia.org/wiki/OpenLDAP
