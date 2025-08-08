FROM mcr.microsoft.com/dotnet/framework/sdk:4.8 AS build
WORKDIR /app

# Copy solution and project files first for better layer caching
COPY *.sln ./
COPY app/*/*.csproj ./app/*/

# Restore packages
RUN nuget restore

# Copy source code
COPY . ./

# Build and publish
RUN msbuild app/Bookstore.Web/Bookstore.Web.csproj /p:DeployOnBuild=true /p:PublishProfile=FolderProfile.pubxml

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019 AS runtime

# Setup LogMonitor
WORKDIR /LogMonitor
ADD https://github.com/microsoft/windows-container-tools/releases/download/v2.0.2/LogMonitor.exe .
COPY LogMonitorConfig.json .

# Copy application
WORKDIR /inetpub/wwwroot
COPY --from=build /app/app/Bookstore.Web/obj/Docker/publish/ .

ENTRYPOINT ["C:\\LogMonitor\\LogMonitor.exe", "C:\\ServiceMonitor.exe", "w3svc"]
