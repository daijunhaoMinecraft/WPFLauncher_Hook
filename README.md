# WPFLauncher Hook

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/未标题-5.png" width="700" alt="未标题-5.png (1791×815)"/></div>

<p align="center" style="color: #808080">更好的网易我的世界启动器</p>

# 免责声明

**本项目仅供学习交流使用, 严禁用于非法用途/商业/倒卖等多类用途使用**

本项目使用 GPL 3 开源, 也就是说你可以发布你修改本项目后的文件, 需要开源(**不得以任何方式阻止玩家获取源代码,包括但不限于倒卖等行为**), 当然可以在你的项目贴上你的赞助地址, 只不过需要贴上项目引用的是[这个项目的开源地址]([daijunhaoMinecraft/WPFLauncher_Hook: WPFLauncher Hook](https://github.com/daijunhaoMinecraft/WPFLauncher_Hook))与作者名称

如遇到软件功能类bug/提交一些建议等建议你提交[Issues](https://github.com/daijunhaoMinecraft/WPFLauncher_Hook/issues)同时提交功能的时候请确保这个功能可以被实现(推荐提交的是PR,作者实现不了功能的会直接关闭Issue)

# 功能

#### 1.(重点)发烧平台绕过

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220180154495.png" width="700" alt="image-20260220180154495"/></div>

<p align="center" style="color: #808080">以前的我的世界启动器会显示的登录界面</p>

发烧平台绕过的好处

- 解决启动器启动启动器的问题

- 减少发烧平台在后台的损耗(比如内存占用大)
- 减少磁盘占用(可能你压根没有玩发烧平台的游戏, 因此会占用你俩份磁盘空间(一份是网易我的世界启动器本体, 另一份是发烧平台))
- 支持32位电脑启动网易我的世界启动器(可能仅此启动,估计Java等可能是x64位,并且基岩版网易放弃了x86,也就是熟知的windowsmc)

#### 2. 控制台显示

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220174956351.png" width="700" alt="image-20260220174956351"/></div>

<p align="center" style="color: #808080">控制台界面(可在WPFLauncher启动器根目录创建"DisableConsole"文件来禁用控制台)</p>

- 支持网易相关的日志显示
- 支持显示相关的事件信息(比如拦截反作弊发包、联机大厅进入房间的详细信息等)
- 支持显示Sauth(Cookie)内容等(因此提出Issue的时候需抹除掉重要信息如Sauth)...

#### 3.配置文件

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220175414226.png" width="700" alt="image-20260220175414226"/></div>

<p align="center" style="color: #808080">配置面板界面</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220182247700.png" width="700" alt="image-20260220182247700"/></div>

<p align="center" style="color: #808080">保存过配置后每次启动的弹窗</p>

- 支持添加配置模板(配置模板位置:MCLauncher/ConfigTemplate, 文件名称:Name.json, json内容格式为MCLauncher/config.json)
- 支持导入上次调试的配置

#### 4.登录

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220175726329.png" width="700" alt="image-20260220175726329"/></div>

<p align="center" style="color: #808080">输入网易账号登录后的弹窗</p>

<div align="center"><img src="C:\Users\Administrator\AppData\Roaming\Typora\typora-user-images\image-20260220181855955.png" width="700" alt="image-20260220181855955"/></div>

<p align="center" style="color: #808080">关闭网易登录界面的弹窗(网易登录界面点击右上角"X"), 适用于没有网易邮箱账号的玩家</p>

- 支持4399账号登录/[网易手机号登录](https://github.com/daijunhaoMinecraft/WPFLauncher_Hook/blob/main/PhoneLogin.md)/Sauth(Cookie)登录
- 自动导入上次使用的Sauth/4399账号信息

#### 5.联机大厅

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220181140851.png" width="700" alt="image-20260220181140851"/></div>

<p align="center" style="color: #808080">创建房间截图</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220181624097.png" width="700" alt="image-20260220181624097"/></div>

<p align="center" style="color: #808080">web房间管理器(需前往配置开启Web服务器)</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220182611828.png" width="700" alt="image-20260220182611828"/></div>

<p align="center" style="color: #808080">当玩家有权限保存存档时候每次退出的截图</p> 

<div align="center"><img src="https://camo.githubusercontent.com/2cce676634a2207873a5a48b86394c88f387671d9333d01f162984d3287746a1/68747470733a2f2f7831392e66702e70732e6e6574656173652e636f6d2f66696c652f3638646433333530333462333036326236313138333937644a7a69675a4d72343036" width="700" alt="Screen"/></div>

<p align="center" style="color: #808080">来自3.0.5-DLL-Public的截图(支持启动时更改IP端口)</p>

<div align="center"><img src="C:\Users\Administrator\AppData\Roaming\Typora\typora-user-images\image-20260220185947702.png" width="700" alt="image-20260220185947702"/></div>

<p align="center" style="color: #808080">不限数字的设置密码(支持进入房间时房间密码也可输入不限数字的密码)</p>

- 没开启Web端时拥有的功能
- - 支持查看房间信息(控制台输出)
  - 退出房间前提醒存档保存(可在配置文件中设置禁用)
  - 支持更改启动时的IP端口(需前往配置文件中设置)
  - 联机大厅密码(加入房间/创建房间)支持任意字符(包括但不限于中文/字母等可以被识别的符号)而非单纯的数字
- 开启Web端时候
- - Web端房间管理界面
  - 支持UserID拉黑玩家/玩家名称正则表达式拉黑玩家
- and so on...

#### 6.基岩版

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220183145651.png" width="700" alt="image-20260220183145651"/></div>

<p align="center" style="color: #808080">每次启动基岩版游戏最后一步支持路径选择</p>

- 支持基岩版游戏选择(可选择启动不同类型的基岩版,比如不同材质/不同版本的基岩版)
- 支持屏蔽基岩版文件校验(校验Minecraft.Windows.exe/.checkInfo文件等)

#### 7.(重点)反作弊的屏蔽

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220183607156.png" width="700" alt="image-20260220183607156"/></div>

<p align="center" style="color: #808080">网易我的世界启动器特有的窗口名称反作弊检测(当然不止这一个,还有DLL注入检测等)</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220184617688.png" width="700" alt="image-20260220184617688"/></div>

<p align="center" style="color: #808080">MAC地址随机化</p>



- 防止在任何世界(如单人世界/多人游戏)因开如鼠大侠/按键精灵等第三方软件导致封号问题(说实话单人世界也检测我是真没绷住)
- 随机化mac地址/硬盘机器码

#### 8.(NEW!) 本地联机引流支持

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/68747470733a2f2f7831392e66702e70732e6e6574656173652e636f6d2f66696c652f3639393264363763613763613836356131643334343632324343454b496a51693037.png" width="700" alt="'68747470733a2f2f7831392e66702e70732e6e6574656173652e636f6d2f66696c652f3639393264363763613763613836356131643334343632324343454b496a51693037'"/></div>

<p align="center" style="color: #808080">此截图来源于3.0.9-DLL-Public截图</p>

- 此功能的添加来源于Issue #7 没错,相比[Koud-Wind/Netease-minecraft-LAN-connects-to-Server](https://github.com/Koud-Wind/Netease-minecraft-LAN-connects-to-Server)项目有一点不同的是本程序将该步骤简化了,玩家只需要启动服务器,然后再输入服务器的内网IP端口就可以了

#### 9.(重点) HTTP服务端/WebSocket服务端(开发者的喜爱,可用于开发插件等功能)

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220184537461.png" width="700" alt="image-20260220184537461"/></div>

<p align="center" style="color: #808080">HTTP 服务器的监听地址等信息</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220184744452.png" width="700" alt="image-20260220184744452"/></div>

<p align="center" style="color: #808080">API 文档</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220184935472.png" width="700" alt="image-20260220184935472"/></div>

<p align="center" style="color: #808080">API Endpoints 和 API Root GET请求后的返回内容</p>

<div align="center"><img src="https://camo.githubusercontent.com/3402666cc154f8fde63e68352f333e58b8e34dd3d6014f9e65f066611ac2457d/68747470733a2f2f7831392e66702e70732e6e6574656173652e636f6d2f66696c652f36386231623133356534623162623936393538653237303075794968575065753036" width="700" alt="Screen"/></div>

<p align="center" style="color: #808080">来自3.0.4-DLL的截图(支持WebSocket协议)</p>

- 具体功能请见[NeteaseMinecraftHook API文档](https://wpfhook.theconsole.top/

#### 10. (重点) 多开网易我的世界启动器

<div align="center"><img src="https://camo.githubusercontent.com/3ad8cd6596243c70237849cf6591f48d91e45b486d255f89696b7762c7dba4ef/68747470733a2f2f7831392e66702e70732e6e6574656173652e636f6d2f66696c652f36386131623861393865303564623237313231323538393457674f65695550693036" width="700" alt="d13f74318acfd6eb47e66aad5b21b675.png"/></div>

<p align="center" style="color: #808080">来自3.0.3-MultiOpen的截图,使用双显示器截图</p>

- 去除网易我的世界启动器禁用多开的问题

#### 11. 禁用活动广告

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/image-20260220190722427.png" width="700" alt="image-20260220190722427"/></div>

<p align="center" style="color: #808080">禁用此广告</p>

- 禁用此类型的广告

#### 12. 其他

- DLL替换,简单操作即可安装
- 去除敏感词检测(部分在线类功能可能还是会有云端检测的)
- 去除发包之日志类
- x86/x64基岩版可任意切换(x86版本没用了,目前诸如联机大厅/本地联机/网络游戏等使用x86基岩版进入的时候会显示"服务器发送了破损的数据包")
- 防沉迷绕过(不过账号还是需要实名的,但是无需人脸识别,适用于如小白Cookie生成出来的Cookie为防沉迷阶段)
- 加入房间后防止被房主自动踢出房间(自动加入房间)
- 解锁所有灰度测试功能(除a50setup外)
- (and more)...

## 安装

### 1. 前置条件
在开始之前，请确保您的系统是支持.NET Framework 4.8.1支持库
### 2. 快速开始 (Quick Start)

第一步: [下载网易我的世界启动器](https://adl.netease.com/d/g/mc/c/pe?type=windows)

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/1.png" width="700" alt="1"/></div>

<p align="center" style="color: #808080">这一步已废弃,原因是无法找到下载入口,请打开第一步的链接直接下载</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/2.png" width="700" alt="2"/></div>

<p align="center" style="color: #808080">点击"开始下载"</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/3.png" width="700" alt="3"/></div>

<p align="center" style="color: #808080">下载完成后打开文件</p>

第二步: 安装网易我的世界启动器

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/4.png" width="700" alt="4"/></div>

<p align="center" style="color: #808080">先点击同意协议,之后点击快速安装(可自定义安装路径,只需要点击"自定义安装"然后选择文件路径即可)</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/5.png" width="700" alt="5"/></div>

<p align="center" style="color: #808080">点击立即体验</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/6.png" width="700" alt="6"/></div>

<p align="center" style="color: #808080">关闭网易我的世界启动器</p>

第三步: 更新网易我的世界启动器

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/7.png" width="700" alt="7"/></div>

<p align="center" style="color: #808080">打开网易我的世界启动器安装路径</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/8.png" width="700" alt="8"/></div>

<p align="center" style="color: #808080">找到Updater文件夹</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/9.png" width="700" alt="9"/></div>

<p align="center" style="color: #808080">双击打开MCLauncherUpdater.exe可执行文件</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/10.png" width="700" alt="10"/></div>

<p align="center" style="color: #808080">点击立即更新(如果出现启动了网易我的世界启动器(弹窗前往发烧平台)则代表你当前的我的世界版本是最新的)这个时候你就应该跳转到第四步了</p>

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/11.png" width="700" alt="11"/></div>

<p align="center" style="color: #808080">更新完成后点击确定</p>

第四步: 替换

<div align="center"><img src="https://raw.githubusercontent.com/daijunhaoMinecraft/WPFLauncher_Hook/main/assets/12.png" width="700" alt="12"/></div>

<p align="center" style="color: #808080">到Release下载最新版本的DLL后拖拽到网易我的世界启动器安装路径进行替换Mcl.Core.dll文件</p>

至此, 你成功安装了这个更好的网易我的世界启动器

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=daijunhaoMinecraft/WPFLauncher_Hook&type=Date)](https://www.star-history.com/#daijunhaoMinecraft/WPFLauncher_Hook&Date)
