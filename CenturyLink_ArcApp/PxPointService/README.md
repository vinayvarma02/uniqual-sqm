## Additional Instructions for PxPointWinSvc

1. Edit the sample-pxpointwinsvc-config.json file to set the correct paths to datasets, license file, and PxPointReference.ref. Also set the license code and logging information as appropriate.
2. Edit PxPointWinSvc.exe.config to point to the JSON config file.
3. To add shape layers, add a `"layers"` key to the `"initializeRequest"` block in the JSON config file. See documentation for the Initialize call in the PxPoint4S API Reference.