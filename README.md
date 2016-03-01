#Pandora.Server.Api setup dev environment

#####Prerequisites:
- Roslyn
- .NET Framework 4.6
- ASP.NET 5
- HttpPlatformHandler v1.2 - http://www.iis.net/downloads/microsoft/httpplatformhandler#additionalDownloads (use the additional downloads section)

- - -
    Create google credentials for Pandora on https://console.developers.google.com
    (You can skip this step if you've already did it)
    1. Click on "Use Google APIs"
    2. Click on "Credentials" (on the left side panel)
    3. Click on "Create credentials" > "OAuth client ID"
    4. Select "Web application"
    5. Enter "Pandora" as a name
    6. Copy the client id and the client secret and stick them ... somewhere safe
    7. Go to "Overview" (on the left side panel)
    8. Click on "Google+ API"
    9. Click "Enable"

1. Clone the repository from `git@github.com:Elders/Pandora.Server.Api.git` or `https://github.com/Elders/Pandora.Server.Api.git`
2. Go to `Pandora.Server.Api\src` and create a copy of the `Pandora.Server.Api.Configuration.Sample` folder
3. Rename the newly created folder to `Pandora.Server.Api.Configuration` (remove `.Sample - Copy`)
4. Go to `Pandora.Server.Api.Configuration`
5. Open all Json config files in a text editor
 - Elders.Pandora.Server.Git.json
    This is the configuration for accessing the git repository with configurations.

    - Replase the `machine-name` node with your machine's name (tip: execute `hostname` in cmd)
    - The rest of the values are self-explanatory

 - Elders.Pandora.Server.Authentication.json
    This is the configuration for the api authentication with IdentiotyServer

    - Replase the `machine-name` node with your machine's name
    - Set your client id as an `audience`
    - Set `accounts.google.com` as an issuer

 - Elders.Pandora.Server.Api.json
    This is the configuration file for the Pandora api itself. It describes where the working directories are and what are the "super duper mega giga" admin users.
    This file references the other configuration files for Pandora.

    - Set your machine name in the configuration
    - Set your email as a `super_admin_users` (the same you used for creating credentials)

6. Ensure that you have an environment variabe `CLUSTER_NAME` with value `local`
7. Open a cmd window as an administrator in the repository directory and execute the `set_variables-as-admin.bat` script
8. Execute `dnu restore`
9. Execute `run.cmd`
10. Happy coding!
