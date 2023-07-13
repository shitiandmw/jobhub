# redis客户端组件

这四个dll是在github开源代码 https://github.com/ServiceStack/ServiceStack 的基础上修改了每小时6000次访问的限制，简单来说就是ServiceStack.Redis 6.9.1 的破解版本

## 修改位置
1. ServiceStack\ServiceStack.Redis\src\ServiceStack.Redis\RedisNativeClient_Utils.cs 
   注释了401、402 行
2. ServiceStack\ServiceStack.Text\src\ServiceStack.Text\LicenseUtils.cs
   修改166行，变量RedisRequestPerHour的值，把6000改成了int.MaxValue

然后修改Release方式，重新生成即可在ServiceStack\ServiceStack.Redis\src\ServiceStack.Redis\bin\Release\net6.0目录看到这四个dll

## 注意
破解授权仅用于学习交流，请勿商用