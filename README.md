# windnsdock

The windnsdock container will synchronize the local `C:\Windows\System32\drivers\etc\hosts` file with the ip addresses of running windows containers. This is useful in a dev environment to access local containers with their name as their hostname.

```
docker pull jeffcyr/windnsdock
docker run -d --isolation process --name windnsdock --restart always -v \\.\pipe\docker_engine:\\.\pipe\docker_engine -v C:\Windows\System32\drivers\etc:C:\etc windnsdock
```

https://hub.docker.com/r/jeffcyr/windnsdock
