<p align="center">
  <img width="200" src="https://github.com/bad-ch/BMapr-public/blob/master/logo.svg"><br/>
</p>


# BMapr
An AddOn for [MapServer](https://mapserver.org/), designed to enhance its functionality and provide additional features.

# Features
* MapServer Core Functionality:
  * Publishes spatial data through OGC Geoservices like WMS, WFS, etc.
  * Support for OGC API features
* BMapr Enhancements:
  * Support for WFS-T (T for Transaction not Time) on Microsoft SQL Server.
  * Integrated lightweight web server.

# Installation

## Prerequisits
* [ASP.NET Core Runtime 9.0.xx: Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
* [Microsoft Visual C++ Redistributable (x64)](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#latest-microsoft-visual-c-redistributable-version) v14.40 or higher

## Setup
1. Download binaries from releases.
2. Extract all files (C:\BMapr\)

# Run as hosting app

Use following command (run as adminstrator):
``` bat
C:\BMapr\BMapr.GDAL.WebApi.exe port=8080
```

# Run as IIS website

Create a new website in IIS, which points to the root folder => use the following endpoints, please change the port according your configuration.

# Configuration BMapr
* Possibility to change configurations in appsettings.json 
# Configuration MapServer
* Configure projects and services using a single map file (.map) located in project folders.
* Adjust settings for layer definitions, service connections, and metadata as needed.

# Endpoints
* Swagger UI: Explore APIs at http://localhost:8080/swagger
* Health Check: Use the http://localhost:8080/api/server/ping endpoint for monitoring
* Version Info: Check installed versions at http://localhost:8080/api/server/version?token=mysecret please have a look in the appsettings.json Settings|Token

# Credits
Main contributor is [bad-ch](https://github.com/bad-ch). :ok_man:
