# ETFramework

## 一、修改部分

1、根据最新的ET9.0修改的框架，将前后端完全分离，重构了部分代码和逻辑。

2、修改了部分程序集，修改了启动时的加载方式，将原本的反射改为动态注册，比如Event事件，Invoke事件，网络消息事件等，降低使用门槛和方便理解。

3、将原本的UI框架改为我自己写的UI框架，更加灵活。

4、将原本的Excel导表工具改为鲁班导表工具，更加方便。

5、将原本的序列化方式从MemoryPack改为Nino。

6、添加了Console面板，方便打包出去查看日志

7、完全修改了Entity，Entity不再支持序列化和反序列化。

8、后续功能还在持续修改和完善中。

## 二、启动流程

1、安装.Net 8.0

2、服务端使用Rider或者VS打开DotNet.sln，点击构建解决方案，等待编译完成。

3、修改工作目录为根目录，改为项目的ETFramework文件夹，ET.GenerateEntity是生成EntitySystem注册代码的，
ET.Proto2CS是把Proto转成C#代码，ET.App是服务器入口。

4、客户端使用Unity打开即可，修改GlobalConfig运行模式改为编辑器下的模拟模式（把await GetVersion();注释），热更模式的话自行打AB包，
然后本地构建一个服务器，打开await GetVersion();的注释，打完AB包后，点击菜单ET/Loader/生成 config.json，保证版本号和打包的版本号一致，
把生成的json放到资源服根目录下。

5、如果是编辑器下的模拟模式，需要注释服务端的HttpGetResourceVersionsHandler脚本，
否则服务端会请求资源服的Config配置文件，如果没有安装MongoDB数据库， 需要把C2R_LoginAccountHandler中数据库相关的代码注释了，
然后启动服务器，启动客户端，随机输入账号和密码登录


