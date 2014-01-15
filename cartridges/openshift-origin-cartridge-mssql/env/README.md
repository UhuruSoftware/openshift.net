Folder contains erb templates having the following file format : *.erb

The erb templates can be used to provide flexible configuration and environment variables.

The erb templates are processed in 2 phases:
1. The first pass processes any entries in your env directory. This pass happens before **bin/setup.ps1** is called and is mandatory.
2. The second pass processes any entries specified in the processed_templates entry of **metadata/managed_files.yml**. This pass happens after **bin/setup.ps1** but before **bin/install.ps1**. This allows **bin/setup.ps1** to create or modify ERB templates if needed. It also allows for **bin/install.ps1** to use these values or processed files.
