# 延时任务调度系统

<div style="font-size: 1.5rem;">
  <a href="./README.md">English</a> |
  <a href="./README_ZH.md">中文</a>
</div>
</br>

这是一个使用.net6开发的轻量级的任务调度系统，用于执行延时任务。创建任务使用http协议，任务执行后会回调指定的http地址，方便跨语言调用
## api说明

### 添加任务
post /delay/create application/json {id:任务id, timestamp: 啥时候触发, authorization: 任务密钥 , callback: 回调的http地址(post ，如果任务执行成功必须返回包含success的字符串，否则会重试很多次)}，示例：
```
curl -X 'POST' \
  'https://localhost:7008/Delay/create' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "id": "stringstri",
  "callback": "string",
  "authorization": "string",
  "timestamp": 0
}'
```
### 取消任务
post /delay/cancel application/json {id:任务id} ，示例：
```
curl -X 'POST' \
  'https://localhost:7008/Delay/cancel' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "id": "string"
}'
```

## 启动方式 

定位到WebApplication1/dist目录下，`docker-compose up -d` 即可 , 注意，请赋予此目录的docker权限，或将此目录权限设置为777

## 运行日志

如果你在WebApplication1/dist目录下用docker-compose启动服务，那么会在此目录下生成一个log文件夹，可以在这里找到程序的运行日志，格式如下：

```
###cdf1aed1450a4e5b931e0810c89c1938  2023-07-13 07:44:35.377 INFO CreateTest 1799s
###911fbc2db99e420fa96566691517aea4  2023-07-13 07:45:27.000 INFO TakeDelayData time:1689234327;result:0
###30593d713b7245359b17f7c8b458ac86  2023-07-13 07:46:27.028 INFO TakeDelayData time:1689234387;result:0
###049e289c0b194382a46ad7802ed6c9ff  2023-07-13 07:46:41.035 INFO TakeDelayData time:1689234401;result:1
###fa30a7732dfa41ff96e433d642bc8bd4  2023-07-13 07:46:41.035 INFO WorkDetail url:http://172.20.0.166/jobhub/callback;post_data:{"id":"cancel:2023071349495753","retry_index":7}
###351a21be5e5c4e09b24d8829f7f15579  2023-07-13 07:46:41.061 INFO WorkDetail url:http://172.20.0.166/jobhub/callback;result:{"code":200,"msg":"success","data":"success"}
###3dc08ea61fad407884a3f55207127fc7  2023-07-13 07:47:27.057 INFO TakeDelayData time:1689234447;result:0
###d63b0b7e66eb48d89cfc791b71a3c022  2023-07-13 07:47:57.346 INFO Create input {"id":"cancel:2023071399102505","callback":"http://172.20.0.166/jobhub/callback","authorization":"4cfbad35-2132-11ee-808a-0242ac14005c","timestamp":1689236276,"retry_index":0}
```

## 性能

我在一台4核的服务器上做了个30s的简单压力测试，结果是创建任务的吞吐量大约是9000qps，任务执行的话取决于你开启的任务线程数，以及回调地址的响应速度

测试的具体数据如下

```
8 threads and 400 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency    45.07ms   18.75ms 226.57ms   81.87%
    Req/Sec     1.13k   257.69     1.87k    66.43%
  Latency Distribution
     50%   40.93ms
     75%   51.65ms
     90%   65.46ms
     99%  120.00ms
  271262 requests in 30.09s, 50.45MB read
  Socket errors: connect 0, read 97, write 92, timeout 0
Requests/sec:   9013.75
Transfer/sec:      1.68MB
```

## 注意

此项目引用了一些其他开源项目的模块修改版，仅用于学习交流，若将此项目用于商用，有可能产生版权纠纷，请自行斟酌使用 

## 作者

Copyright (c) 2023 lushitian404@gmail.com