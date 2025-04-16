from pulumi.provider.experimental import component_provider_host

from appImage import AppImage # Docker build and registry
from appDeploy import AppDeploy # Push to container app
# from appImageDeploy import AppImageDeploy # All of the above

if __name__ == "__main__":
    component_provider_host(name="gke", components=[AppImage, AppDeploy])
