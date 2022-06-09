# 2ndbot

Second Life Bot System.

using libremetaverse

Chat API

https://www.chaplus.jp/

https://mebo.work/

https://openai.com/

```
$ wget https://packages.microsoft.com/config/ubuntu/21.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb
$ rm packages-microsoft-prod.deb

$ sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-6.0

$ dotnet new console -o 2ndbot
$ cd 2ndbot
$ dotnet add package LibreMetaverse

$ copy .setting_sample.xml .settings.xml
$ vim .setting.xml
$ dotnet run

$ dotnet publish
```
