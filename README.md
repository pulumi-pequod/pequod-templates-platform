# pequod-templates-platform
A set of templates that can be launched to tell a story about developer experience and platform team support.

## Notes
* azure-cs|py|yaml-containerapps: used as starting point to show the basics
  * There are operationalinsights related mandatory policies in the azure policy pack that can be used to show the developer going rogue and being prevented from doing things.
  * But can be addressed by capturing those best practives in components.
* component-*: abstractions to capture best practices
  * There is a policy that fires if resources are created outside approved components.
  * These components could be added.
* azure-cs(|yaml?)-comp-containerapp: Uses the component(s) 
  * Cleaner code
