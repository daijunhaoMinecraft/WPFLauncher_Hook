# WPFLauncher_Hook

手机号登录教程 [PhoneLogin.md](https://github.com/daijunhaoMinecraft/WPFLauncher_Hook/blob/main/PhoneLogin.md)<br/>
网易我的世界启动器修改版<br />
**有问题/建议记得去提[Issue](https://github.com/daijunhaoMinecraft/WPFLauncher_Hook/issues)**<br />
**代码全部开源,欢迎各位积极提交PR去改进代码**<br />
**github src中bin里面的Mcl.Core.dll为测试版本(供想尝鲜的用户使用)**<br />

## 使用须知
**项目仅供学习交流使用，严禁用于非法用途等多类用途使用(不止这点，还有需遵守[使用条款](#使用条款))，否则后果自负**

# 特点
 - DLL替换,简单操作即可安装
 - 支持多开网易我的世界启动器
 - 有Http/Websocket协议,开发者的喜爱(可用于开发插件/辅助功能)
 - **可绕更新,可绕发烧平台**
 - 联机大厅反锁服(黑名单系统)
 - 强大的联机大厅房间管理神器
 - 去除敏感词检测(部分在线类功能可能还是会有云端检测的)
 - 调出控制台输出(可供开发者查看/普通玩家查看问题)
 - 支持4399登录
 - 支持Cookie登录
 - 去除活动广告
 - 支持外进IP(基岩版)
 - 去除网易内置进程检测(salog-new包)
 - 去除发包之日志类
 - 随机化mac地址/硬盘机器码
 - 去除网易我的世界基岩版更新
 - x86/x64基岩版可任意切换(x86版本没用了,目前诸如联机大厅/本地联机/网络游戏等使用x86基岩版进入的时候会显示"服务器发送了破损的数据包")
 - 防沉迷绕过(不过账号还是需要实名的,但是无需人脸识别,适用于如小白Cookie生成出来的Cookie为防沉迷阶段)
 - 加入房间后防止被房主自动踢出房间(自动加入房间)
 - 详细化房间信息输出
 - 解锁所有灰度测试功能(除a50setup外)
 - 联机大厅密码(加入房间/创建房间)支持任意字符(包括但不限于中文/字母等可以被识别的符号)而非单纯的数字
 - 每次退出房间的时候会提醒用户保存房间(用户自主选择是否保存房间,当然不让保存的房间是不会提醒的)
 - (and more)...<br />

**屏幕截图可看Release发布历史**<br />
# 赞助作者
[爱发电地址](https://afdian.com/a/daijunhao)<br/>
![PixPin_2025-10-02_20-49-55.png](https://x19.fp.ps.netease.com/file/68de74e6ff2cf8b94b493df2SAlevXfG06)
 # 注意事项
**使用此软件需要先安装[.NET6.0运行时](https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=x64&rid=win10-x64&apphost_version=6.0.36)才能正常使用,否则会遇到如下报错:**
<br />
App: C:\Users\Administrator\Desktop\bin\Debug\net48\WPFLauncher_Injet.exe<br />
Architecture: x64<br />
App host version: 6.0.36<br />
.NET location: Not found<br /><br />
Learn about runtime installation:<br />
https://aka.ms/dotnet/app-launch-failed<br /><br />

Download the .NET runtime:<br />
https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=x64&rid=win10-x64&apphost_version=6.0.36<br />

# 使用方法
Step 1:前往[网易我的世界官网](https://mc.163.com/)上去下载32位包体<br/>
**2025/8/12:Latest:目前网易我的世界启动器官网只有一个发烧平台的包体，[32位包体下载链接](https://adl.netease.com/d/g/mc/c/pe?type=windows)**<br/>

![PixPin_2024-08-05_11-01-03](https://github.com/user-attachments/assets/513eb0b8-e6b3-430e-bfd5-f04ea80789ee)<br />
![PixPin_2024-08-05_11-02-00](https://github.com/user-attachments/assets/5ad49668-ae2a-4692-8fc7-471a8ff65f3f)<br />
![PixPin_2024-08-05_11-02-56](https://github.com/user-attachments/assets/4408f236-d421-4a01-b7a3-59ee060bbd7c)<br />

Step 2:安装网易我的世界启动器(32位包体)<br />

![PixPin_2024-08-05_11-03-49](https://github.com/user-attachments/assets/10fdb62b-c310-44ec-a697-2638df66c5de)<br />
![PixPin_2024-08-05_11-05-37](https://github.com/user-attachments/assets/0f14ca32-98a3-4809-85f9-2e2ec539520f)<br />
Step 3:更新网易我的世界启动器盒子<br />

![PixPin_2024-08-05_11-06-59](https://github.com/user-attachments/assets/873b22b3-d471-4153-b52d-c6b99329d364)<br />
![PixPin_2024-08-05_11-12-27](https://github.com/user-attachments/assets/acdef99d-9dd5-43da-82c1-23d213f99bc7)<br />
![PixPin_2024-08-05_11-13-36](https://github.com/user-attachments/assets/9364af33-729c-4b37-9873-b34e037f02ed)<br />
![PixPin_2024-08-05_11-14-00](https://github.com/user-attachments/assets/354164c4-ede6-4b0e-82b8-68a0be37c4a5)<br />
![PixPin_2024-08-05_11-14-35](https://github.com/user-attachments/assets/604b932e-5a40-49c1-8bc5-ce0f3c313a87)<br />
![PixPin_2024-08-05_11-15-13](https://github.com/user-attachments/assets/f868f512-4135-4c7f-86de-cb89dd363e4d)<br />
~~Step 4:Hook盒子~~(目前已废弃,接下来的方法均为替换文件)<br/>

![PixPin_2024-09-21_11-15-48](https://github.com/user-attachments/assets/84324db8-2288-4db8-aec8-b30bd02309ad)<br />
![PixPin_2024-09-21_11-18-03](https://github.com/user-attachments/assets/24321f50-d3a2-4a51-b031-57d7833f253d)<br />

Tips:如果这里一直失败的话那证明了你的电脑配置非常的好,请到该软件目录下修改StartConfig.json的注入时间(单位毫秒)<br />
也有可能是你没安装.NET 6.0运行时,请去到这个ReadME的开头，有提供下载链接<br />

[Latest]Step 4 替换文件(实际上是[WPFLauncher_fever_Bypass_1](https://github.com/daijunhaoMinecraft/WPFLauncher_fever_Bypass_1)的步骤,不发布在此处的原因是这是属于Hook的部分)<br />
![354961211-5ce983b1-23c4-4bc8-bcab-32118194d862.png](https://x19.fp.ps.netease.com/file/689dbf0c585717a8691687a4F0kw1w3l06)

# 效果展示
![PixPin_2024-09-21_11-18-46](https://github.com/user-attachments/assets/d2c9da80-64c7-47b1-9980-fb4cbc5a2eca)<br />
![PixPin_2024-09-21_11-19-11](https://github.com/user-attachments/assets/ffdb2f8d-e303-4a35-9ec4-09ef4e71cb4b)<br />
![PixPin_2024-09-21_11-19-29](https://github.com/user-attachments/assets/4aa77c4f-f1d6-4eaf-8569-81d258b941fe)<br />
盒子可正常登录,不会修改盒子本身,同时也不会修改盒子其他文件,相当于[WPFLauncher_fever_Bypass](https://github.com/daijunhaoMinecraft/WPFLauncher_fever_Bypass)的升级版,只不过这是通过网络拦截的
**(Hook盒子出现还是有提示发烧平台这时候就要多尝试几次了,若尝试几次无果后可以提交bug(这可能代表了新版本把命名空间给重命名了))**
**若发现软件bug请到Issues处进行提交bug**

## 许可证

本项目使用 [GNU General Public License v3.0 (GPL-3.0)](https://www.gnu.org/licenses/gpl-3.0.html) 许可证。

## 使用条款

我们希望用户能够自由使用、修改和分发本软件，但请注意以下几点：

1. **源代码公开**：根据 GPL-3.0 许可证，任何人分发本软件的修改版本时，必须公开源代码。这意味着你不能将本软件或其修改版本作为封闭源代码的商业产品出售。

2. **禁止倒卖**：我们强烈反对任何形式的倒卖行为。任何试图将本软件打包并以付费形式分发的人，必须遵循 GPL-3.0 许可证的要求，公开源代码并遵循相同的开源条款。

3. **商业使用**：虽然本软件允许商业使用，但任何商业用途都必须遵循 GPL-3.0 的条款。你可以提供增值服务或支持，但不能限制他人获取源代码或使用软件。

4. **反馈与贡献**：我们欢迎社区的反馈和贡献。如果你对本软件有任何改进建议，欢迎提出！

感谢你的理解与支持，确保开源软件的自由与透明！

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=daijunhaoMinecraft/WPFLauncher_Hook&type=Date)](https://www.star-history.com/#daijunhaoMinecraft/WPFLauncher_Hook&Date)
