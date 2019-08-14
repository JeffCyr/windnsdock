# docker run -d --isolation process --name windnsdock --restart always -v \\.\pipe\docker_engine:\\.\pipe\docker_engine -v C:\Windows\System32\drivers\etc:C:\etc windnsdock
FROM mcr.microsoft.com/dotnet/core/runtime:2.2-nanoserver-1903

USER ContainerAdministrator

COPY publish C:/app

WORKDIR /app

ENTRYPOINT ["dotnet", "windnsdock.dll", "C:/etc/hosts"]