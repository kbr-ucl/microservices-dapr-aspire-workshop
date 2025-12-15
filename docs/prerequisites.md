# Prerequisites

## Dependencies

Download and install the following locally:

- [Docker](https://docs.docker.com/engine/install/)
- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Visual Studio Dapr Extension - Optional but recommended](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vs-dapr)
- [Feature Flags - Optional but recommended](https://marketplace.visualstudio.com/items?itemName=PaulHarrington.FeatureFlagsPreview)
  - Remember to set Dapr flag

- [Powershell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.4) (for Windows users)



## Upgrade Aspire
To determine whether PowerShell may be upgraded with WinGet, run the following command:

```powershell
winget list --id Microsoft.PowerShell --upgrade-available
```

If there is an available upgrade, the output indicates the latest available version. Use the following command to upgrade PowerShell using WinGet:

```powershell
winget upgrade --id Microsoft.PowerShell
```

[Link](https://learn.microsoft.com/da-dk/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.5#msi#deploying-on-windows-10-iot-enterprise)



## Dapr Installation

1. Follow [these steps](https://docs.dapr.io/getting-started/install-dapr-cli/) to install the Dapr CLI.

2. [Initialize Dapr](https://docs.dapr.io/getting-started/install-dapr-cli/):

    ```bash
    dapr init
    ```

3. Verify if local Dapr containers are running:

    ```bash
    docker ps
    ```

![containers](./../imgs/docker-ps.png)



## Upgrade Aspire

Before beginning to code, upgrade to the latest Aspire version.
1. Check your current Aspire version by running the following in a new terminal window:
	```powershell
    aspire --version
   ```
2. Check the current release [Aspire Github](https://github.com/dotnet/aspire/releases). If your version is not up-to-date: Upgrade.

   1.  To install the latest released version of the Aspire CLI: 

      ```powershell
      irm https://aspire.dev/install.ps1 | iex
      ```
   2. Upgrade Aspire templates 

      ```powershell
      dotnet new install Aspire.ProjectTemplates
      ```



##  Redis Insight on Docker

If you use Redis Insight from docker ([Link](https://redis.io/docs/latest/operate/redisinsight/install/install-on-docker/)):

```bash
docker run -d --name redisinsight -p 5540:5540 redis/redisinsight:latest
```
When adding the Redis database, set the host to: host.docker.internal



## Considerations

### Integrated terminal

It is recommended to use Visual Studio to run the workshop.

### Prevent port collisions

During the workshop you will run the services in the solution on your local machine. To prevent port-collisions, all services listen on a different HTTP port. When running the services with Dapr, you need additional ports for HTTP and gRPC communication with the sidecars. If you follow the Dapr CLI instructions, the services will use the following ports for their Dapr sidecars to prevent port collisions:

| Service                    | Application port | Dapr sidecar HTTP port  |
|----------------------------|------------------|------------------------|
| pizza-order      | 8001             | 3501                   |
| pizza-storefront      | 8002             | 3502                  |
| pizza-kitchen | 8003             | 3503               |
| pizza-delivery | 8004             | 3504               |
| pizza-workflow | 8005             | 3505               |

If you're on Windows with Hyper-V enabled, you might run into an issue that you're not able to use one (or more) of these ports. This could have something to do with aggressive port reservations by Hyper-V. You can check whether or not this is the case by executing this command:

```powershell
netsh int ipv4 show excludedportrange protocol=tcp
```

If you see one (or more) of the ports shown as reserved in the output, fix it by executing the following commands in an administrative terminal:

```powershell
dism.exe /Online /Disable-Feature:Microsoft-Hyper-V
netsh int ipv4 add excludedportrange protocol=tcp startport=8001 numberofports=5
netsh int ipv4 add excludedportrange protocol=tcp startport=3501 numberofports=5
dism.exe /Online /Enable-Feature:Microsoft-Hyper-V /All
```



### Central Package Management

The repository is set up using [Central Package Management](https://devblogs.microsoft.com/dotnet/introducing-central-package-management/).

CPM addresses the complexity of managing dependencies across multi-project solutions. Instead of defining versions in each project file, you can centralize them using a central File: Create a `Directory.Packages.props` file at the root of your solution.

At the time of writing this guide - the versions was:

```xml
<Project>
  <PropertyGroup>
    <!-- Enable central package management, https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management -->
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="CommunityToolkit.Aspire.Hosting.Dapr" Version="13.0.0" />
    <PackageVersion Include="Dapr.AspNetCore" Version="1.16.1" />
    <PackageVersion Include="Dapr.Workflow" Version="1.16.1" />
    <PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.1.0" />
    <PackageVersion Include="Microsoft.Extensions.ServiceDiscovery" Version="10.1.0" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.14.0" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.14.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.14.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.14.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Runtime" Version="1.14.0" />
  </ItemGroup>
</Project>
```



In the `.csproj` files - you references nuget packages this way:

```
<ItemGroup>
  <PackageReference Include="Dapr.AspNetCore" />
</ItemGroup>

```

The version of the package will be controlled from the `Directory.Packages.props` file. Updating versions can be done from Visual Studio Package manager.



### File structure



