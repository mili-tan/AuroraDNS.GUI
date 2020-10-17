
<p align="center">
          <a href='https://github.com/mili-tan/AuroraDNS.GUI'><img src='https://i.loli.net/2019/06/10/5cfdb719df5f019195.png' width="50%" height="50%"/></a>
</p>

<p align="center">
          <a href='https://github.com/mili-tan/AuroraDNS.GUI/blob/master/LICENSE.md'><img src='https://img.shields.io/github/license/mili-tan/AuroraDNS.GUI.svg' alt='license' referrerPolicy='no-referrer' /></a>
          <a href='https://ci.appveyor.com/project/mili-tan/AuroraDNS-GUI'><img src='https://img.shields.io/appveyor/ci/mili-tan/AuroraDNS-GUI.svg?&amp;logo=appveyor' alt='AppVeyor' referrerPolicy='no-referrer' /></a>
          <a href='https://github.com/mili-tan/AuroraDNS.GUI/releases/latest'><img src='https://img.shields.io/github/release/mili-tan/AuroraDNS.GUI.svg' alt='GitHub-release' referrerPolicy='no-referrer' /></a>
          <a href='https://github.com/mili-tan/AuroraDNS.GUI/releases/latest'><img src='https://img.shields.io/github/downloads/mili-tan/auroradns.gui/total.svg' alt='Github All Releases' referrerPolicy='no-referrer' /></a>
          <a href='https://www.codefactor.io/repository/github/mili-tan/AuroraDNS.GUI/overview/master'><img src='https://www.codefactor.io/repository/github/mili-tan/AuroraDNS.GUI/badge/master' alt='CodeFactor' referrerPolicy='no-referrer' /></a>
          <a href='https://app.fossa.io/projects/git%2Bgithub.com%2Fmili-tan%2FAuroraDNS.GUI?ref=badge_shield'><img src='https://app.fossa.io/api/projects/git%2Bgithub.com%2Fmili-tan%2FAuroraDNS.GUI.svg?type=shield' alt='FOSSA Status' referrerPolicy='no-referrer' /></a>
</p>


----------



## Introduction

A DNS over HTTPS client for everyone.

*A*uroraDNS is a pure, ~~crude~~simple, user-friendly and graphical local DoH client.

It converts DNS over HTTPS to traditional DNS protocol locally.

## Quick Start

**Download latest version** from [Releases](https://github.com/mili-tan/AuroraDNS.GUI/releases) and enjoy.

Microsoft [.NET Framework 4.6.1](https://docs.microsoft.com/zh-cn/dotnet/framework/install/on-windows-10) runtime may be required.

*Free of complex configuration and works out of the box.* 

------

![Screenshot](https://i.loli.net/2019/04/16/5cb5275b6c232.jpg)

![Screenshot](https://i.loli.net/2019/07/23/5d36ad44a0f3f65675.png)

------

## Using and Testing
Please close similar applications (e.g. DNSCrypt) to prevent port conflict.

**Basic Usage**
> If you are experienced with CLI, please refer to Advanced Usage.  

1. After launching the software, confirm that the toggle "Enable DNS Service" at the top left corner is enabled.
2. Click the "Set to System DNS" button at the lower left corner to enable.

**Advanced Usage**  
> Using CMD command 
 - Use the builtin local DNS service to resolve `baidu.com`
```cmd
nslookup baidu.com 127.0.0.1
```
The software is functional if resolution is successful. Otherwise, please send an Issue if the condition persists after troubleshooting.    

Then it is just needed to check if DNS has been set to `127.0.0.1`

 - Inspect all Internet configurations of all network adapters
```cmd
ipconfig /all
```
 **Attention:** DNS of the used adapter must be set to `127.0.0.1` for the changes to apply.
 
 Please refer to [this article](https://jingyan.baidu.com/article/2fb0ba40833b0a00f2ec5f28.html) for setting the first DNS to `127.0.0.1`.

 It is recommended to set the second DNS to `Tencent 119.29.29.29 or DNS by ISP`

 A DNS cache refresh would suffice if DNS is already set. (Click on the "Refresh Cache" button in the settings panel or use this command

 - Refresh DNS cache
```cmd
ipconfig /flushdns
```

## Feedback

- As a beginner, I seek your kind understanding of the issues in the project.
- In case of bugs or feature requests, please feel free to submit an issue.
- PRs of bug fixes or feature implementations are greatly appreciated.

## Acknowledgements

<img src='https://i.loli.net/2020/08/03/LWNj2BM6mxuYtRU.png' width="8%" height="8%" align="right"/>

> I have been using ReSharper, an really amazing tool that boosted my development.

Many thanks to [JetBrains](https://www.jetbrains.com/?from=AuroraDNS) for providing [ReSharper](https://www.jetbrains.com/ReSharper/?from=AuroraDNS) opensource license for this project.

## Credits 

AuroraDNS would not be here without them.

Please refer to [Credits](https://github.com/mili-tan/AuroraDNS.GUI/blob/master/CREDITS.md) for a list of collaborators and other open source projects used.

## License

Copyleft (c) 2018 Milkey Tan. Code released under the [MIT License](https://github.com/mili-tan/AuroraDNS.GUI/blob/master/LICENSE.md). 
