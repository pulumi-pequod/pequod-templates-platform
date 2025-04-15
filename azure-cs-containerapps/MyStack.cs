// Copyright 2016-2025, Pulumi Corporation.  All rights reserved.

using Pulumi;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.ContainerRegistry.Inputs;
using Pulumi.AzureNative.OperationalInsights;
using Pulumi.AzureNative.OperationalInsights.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using DockerBuild = Pulumi.DockerBuild;
using ContainerArgs = Pulumi.AzureNative.App.Inputs.ContainerArgs;
using SecretArgs = Pulumi.AzureNative.App.Inputs.SecretArgs;

using Stackmgmt = Pequod.Stackmgmt;

class MyStack : Stack
{
    public MyStack()
    {
        var config = new Config();
        var insightsSku = config.Get("insightsSku") ?? "PerGB2018";
        var appIngressPort = config.GetInt32("appIngressPort") ?? 80;
        var platform = config.Get("platform") ?? "linux/amd64";
        var pulumiTags = config.Require("pulumi:tags");
        var resourceGroup = new AzureNative.Resources.ResourceGroup("resourceGroup");

        var workspace = new AzureNative.OperationalInsights.Workspace("workspace", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new AzureNative.OperationalInsights.Inputs.WorkspaceSkuArgs
            {
                Name = insightsSku,
            },
            RetentionInDays = 30,
        });

        var sharedKey = AzureNative.OperationalInsights.GetSharedKeys.Invoke(new()
        {
            ResourceGroupName = resourceGroup.Name,
            WorkspaceName = workspace.Name,
        }).Apply(invoke => invoke.PrimarySharedKey);

        var registry = new AzureNative.ContainerRegistry.Registry("registry", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new AzureNative.ContainerRegistry.Inputs.SkuArgs
            {
                Name = AzureNative.ContainerRegistry.SkuName.Basic,
            },
            AdminUserEnabled = true,
        });

        var registryUsername = AzureNative.ContainerRegistry.ListRegistryCredentials.Invoke(new()
        {
            ResourceGroupName = resourceGroup.Name,
            RegistryName = registry.Name,
        }).Apply(invoke => invoke.Username);

        var registryPasswords = AzureNative.ContainerRegistry.ListRegistryCredentials.Invoke(new()
        {
            ResourceGroupName = resourceGroup.Name,
            RegistryName = registry.Name,
        }).Apply(invoke => invoke.Passwords);

        var appPath = "app";

        var imageName = appPath;

        var imageTag = "latest";

        var managedEnv = new AzureNative.App.ManagedEnvironment("managedEnv", new()
        {
            ResourceGroupName = resourceGroup.Name,
            AppLogsConfiguration = new AzureNative.App.Inputs.AppLogsConfigurationArgs
            {
                Destination = "log-analytics",
                LogAnalyticsConfiguration = new AzureNative.App.Inputs.LogAnalyticsConfigurationArgs
                {
                    CustomerId = workspace.CustomerId,
                    SharedKey = sharedKey,
                },
            },
        });

        var myImage = new DockerBuild.Image("myImage", new()
        {
            Context = new DockerBuild.Inputs.BuildContextArgs
            {
                Location = appPath,
            },
            Push = true,
            Exec = true,
            Tags = new[]
            {
                registry.LoginServer.Apply(loginServer => $"{loginServer}/{imageName}:{imageTag}"),
            },
            Platforms = new[]
            {
                System.Enum.Parse<DockerBuild.Platform>(platform),
            },
            Registries = new[]
            {
                new DockerBuild.Inputs.RegistryArgs
                {
                    Address = registry.LoginServer,
                    Username = registryUsername,
                    Password = registryPasswords[0]?.Value,
                },
            },
        });

        var containerapp = new AzureNative.App.ContainerApp("containerapp", new()
        {
            ResourceGroupName = resourceGroup.Name,
            ManagedEnvironmentId = managedEnv.Id,
            Configuration = new AzureNative.App.Inputs.ConfigurationArgs
            {
                Ingress = new AzureNative.App.Inputs.IngressArgs
                {
                    External = true,
                    TargetPort = appIngressPort,
                },
                Registries = new[]
                {
                    new AzureNative.App.Inputs.RegistryCredentialsArgs
                    {
                        Server = registry.LoginServer,
                        Username = registryUsername,
                        PasswordSecretRef = "pwd",
                    },
                },
                Secrets = new[]
                {
                    new AzureNative.App.Inputs.SecretArgs
                    {
                        Name = "pwd",
                        Value = registryPasswords[0]?.Value,
                    },
                },
            },
            Template = new AzureNative.App.Inputs.TemplateArgs
            {
                Containers = new[]
                {
                    new AzureNative.App.Inputs.ContainerArgs
                    {
                        Name = "myapp",
                        Image = myImage.Ref,
                    },
                },
            },
        });

        var stacksettings = new Stackmgmt.StackSettings("stacksettings");

        this.Endpoint = containerapp.Configuration.Apply(configuration => $"https://{configuration?.Ingress?.Fqdn}"),
    }

    [Output("endpoint")]
    public Output<string> Endpoint { get; set; }
}
