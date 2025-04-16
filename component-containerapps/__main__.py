from pulumi.provider.experimental import component_provider_host

from appImage import AppImage # Docker build and registry
from appDeploy import AppDeploy # Push to container app
from appBuildDeploy import AppBuildDeploy # All of the above

if __name__ == "__main__":
    component_provider_host(name="containerapps", components=[AppImage, AppDeploy, AppBuildDeploy])
