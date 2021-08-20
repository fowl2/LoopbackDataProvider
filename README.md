# Intro

This is a custom Microsoft Dataverse [Virtual Table Data Provider](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/virtual-entities/get-started-ve) plugin that just forwards all requests to another entity in the same enviroment, mapping all the column/attribute names.

Might be useful if you want to convert a virtual entity into a real entity, keep the option open in future, or as a  template/sample data provider plugin.

Very loosely tested, especially Create, Update, and Delete.

## Getting Started

1. Install the solution (alternatively: [register the plugin dll](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/tutorial-write-plug-in#register-plug-in) then follow [these steps](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/virtual-entities/sample-ve-provider-crud-operations#step-2-creating-data-provider-and-adding-plug-ins-to-the-provider))
3. Create a new entity/table
   1. Select "Virtual Entity"
   2. In the "External Name" field, enter the logical name of the entity you want to mirror.
   3. In the "External Collection Name" field, you can enter anything as it is not used by the plugin.
   4. For each field, set the "External Name" to the logical name of the attribute you are mirroring. Don't forget the Primary Name and ID fields!
