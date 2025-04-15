import pulumi
from pulumi_azure_native import containerregistry
from pulumi_azure_native import operationalinsights
from pulumi_azure_native import resources
from pulumi_azure_native import app
import pulumi_docker_build as docker_build

# Pulumi component
from pulumi_pequod_stackmgmt import StackSettings, StackSettingsArgs

# Get stack config
import config
app_path = "app"
image_name = app_path
image_tag = "latest"

resource_group = resources.ResourceGroup("rg")

workspace = operationalinsights.Workspace(
    "loganalytics",
    resource_group_name=resource_group.name,
    sku=operationalinsights.WorkspaceSkuArgs(name=config.insights_sku),
    retention_in_days=30,
)

workspace_shared_keys = pulumi.Output.all(resource_group.name, workspace.name).apply(
    lambda args: operationalinsights.get_shared_keys(
        resource_group_name=args[0], workspace_name=args[1]
    )
)

managed_env = app.ManagedEnvironment(
    "env",
    resource_group_name=resource_group.name,
    app_logs_configuration=app.AppLogsConfigurationArgs(
        destination="log-analytics",
        log_analytics_configuration=app.LogAnalyticsConfigurationArgs(
            customer_id=workspace.customer_id,
            shared_key=workspace_shared_keys.apply(lambda r: r.primary_shared_key),
        ),
    ),
)

registry = containerregistry.Registry(
    "registry",
    resource_group_name=resource_group.name,
    sku=containerregistry.SkuArgs(name="Basic"),
    admin_user_enabled=True,
)

credentials = pulumi.Output.all(resource_group.name, registry.name).apply(
    lambda args: containerregistry.list_registry_credentials(
        resource_group_name=args[0], registry_name=args[1]
    )
)
registry_username = credentials.username
registry_password = credentials.passwords[0]["value"]

my_image = docker_build.Image("myImage",
    context={
        "location": app_path,
    },
    push=True,
    exec_=True,
    tags=[registry.login_server.apply(lambda login_server: f"{login_server}/{image_name}:{image_tag}")],
    platforms=[docker_build.Platform(config.platform)],
    registries=[{
        "address": registry.login_server,
        "username": registry_username,
        "password": registry_password,
    }])

containerapp = app.ContainerApp("containerapp",
    resource_group_name=resource_group.name,
    managed_environment_id=managed_env.id,
    configuration={
        "ingress": {
            "external": True,
            "target_port": config.app_ingress_port,
        },
        "registries": [{
            "server": registry.login_server,
            "username": registry_username,
            "password_secret_ref": "pwd",
        }],
        "secrets": [{
            "name": "pwd",
            "value": registry_password
        }],
    },
    template={
        "containers": [{
            "name": "myapp",
            "image": my_image.ref,
        }],
    })

stacksettings = StackSettings("stacksettings")

pulumi.export("endpoint", containerapp.configuration.apply(lambda configuration: f"https://{configuration.ingress.fqdn}"))
