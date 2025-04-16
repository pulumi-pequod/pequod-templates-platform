import pulumi
from pulumi_azure_native import operationalinsights
from pulumi_azure_native import app
from typing import Optional, TypedDict

class AppDeployArgs(TypedDict):

    resource_group_name: pulumi.Input[str] 
    """The resource group in which the image registry should be deployed."""
    registry_login_server: pulumi.Input[str]
    """Container registry login server."""
    registry_username: pulumi.Input[str] 
    """Login for container registry."""
    registry_password: pulumi.Input[str]
    """Password for container registry login."""
    image_ref: pulumi.Input[str]
    """Reference for the image in the container registry."""
    insights_sku: Optional[pulumi.Input[str]] 
    """Sku for the insights workspace. Default: PerGB2018"""
    app_ingress_port: Optional[pulumi.Input[int]] 
    """Ingress port for the app. Default: 80"""

class AppDeploy(pulumi.ComponentResource):
    """
    Deploys image to Azure container app.
    """

    container_app_fqdn: pulumi.Output[str]

    def __init__(
            self,
            name: str,
            args: AppDeployArgs,
            opts: Optional[pulumi.ResourceOptions] = None
    ):

        super().__init__('containerapps:index:AppDeploy', name, {}, opts)

        resource_group_name = args.get("resource_group_name")
        registry_login_server = args.get("registry_login_server")
        registry_username = args.get("registry_username")
        registry_password = args.get("registry_password")
        image_ref = args.get("image_ref")
        insights_sku = args.get("insights_sku") or "PerGB2018"
        app_ingress_port = args.get("app_ingress_port") or 80

        workspace = operationalinsights.Workspace(
            "loganalytics",
            resource_group_name=resource_group_name,
            sku=operationalinsights.WorkspaceSkuArgs(name=insights_sku),
            retention_in_days=30,
            opts=pulumi.ResourceOptions(parent=self)
        )

        workspace_shared_keys = pulumi.Output.all(resource_group_name, workspace.name).apply(
            lambda args: operationalinsights.get_shared_keys(
                resource_group_name=args[0], workspace_name=args[1]
            )
        )

        managed_env = app.ManagedEnvironment(
            "env",
            resource_group_name=resource_group_name,
            app_logs_configuration=app.AppLogsConfigurationArgs(
                destination="log-analytics",
                log_analytics_configuration=app.LogAnalyticsConfigurationArgs(
                    customer_id=workspace.customer_id,
                    shared_key=workspace_shared_keys.apply(lambda r: r.primary_shared_key),
                ),
            ),
            opts=pulumi.ResourceOptions(parent=self)
        )

        containerapp = app.ContainerApp("containerapp",
            resource_group_name=resource_group_name,
            managed_environment_id=managed_env.id,
            configuration={
                "ingress": {
                    "external": True,
                    "target_port": app_ingress_port,
                },
                "registries": [{
                    "server": registry_login_server,
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
                    "image": image_ref,
                }],
            },
            opts=pulumi.ResourceOptions(parent=self)
        )

        self.container_app_fqdn = containerapp.configuration.ingress.fqdn

        self.register_outputs({})
