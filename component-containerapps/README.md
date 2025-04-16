# component-gke
Abstraction for Google Cloud K8s cluster resources.

This repo delivers a component to abstract the details related to:
- Creating a Google Cloud K8s cluster.

This mitigates the cognitive load on the developer to get the infrastructure they need to run their application.

# Usage

In the folder of the project code that is using the component, run the following command using the release you want.
```bash
pulumi package add https://github.com/pulumi-pequod/component-gke@v0.1.0
```

Or, add a `packages` section to the project's `Pulumi.yaml` file.
See [Using Components](https://www.pulumi.com/docs/iac/using-pulumi/extending-pulumi/build-a-component/#add-the-component-reference-in-pulumiyaml) 

# Example Programs
TBD
