# BMapr-public
BMapr is an AddOn for MapServer, designed to enhance its functionality and provide additional features.

# Features
* MapServer Core Functionality: Publishes spatial data through OGC Geoservices like WMS, WFS, etc.
* BMapr Enhancements:
  * Support for WFS-T (T for Transaction not Time) on Microsoft SQL Server.
  * Integrated lightweight web server.
# Installation
1. Download binaries from releases.
2. Extract files (C:\BMapr\)
3. Set up the appropriate version of the [ASP.NET Core Runtime 9.0.xx: Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) 

# Run
Use following command:
``` bat
C:\BMapr\BMapr.GDAL.WebApi.exe port=8080
```
# Configuration BMapr
* Possibility to change configurations in appsettings.json 
# Configuration MapServer
* Configure projects and services using a single map file (.map) located in project folders.
* Adjust settings for layer definitions, service connections, and metadata as needed.

# Endpoints
* Swagger UI: Explore APIs at http://localhost:8080/swagger
* Health Check: Use the http://localhost:8080/ping endpoint for monitoring
* Version Info: Check installed versions at http://localhost:8080/api/server/version

# Credits
Main contributor is [bad-ch](https://github.com/bad-ch). :ok_man:
