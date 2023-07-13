# Delayed Task Scheduling System

<div style="font-size: 1.5rem;">
  <a href="./README.md">English</a> |
  <a href="./README_ZH.md">??</a>
</div>
</br>

This is a lightweight task scheduling system developed using .net6 for executing delayed tasks. Tasks are created using the HTTP protocol and, upon execution, will callback to a specified HTTP address, making it convenient for cross-language invocation.
## API Description

### Add Task
post /delay/create application/json {id: task id, timestamp: when to trigger, authorization: task key, callback: callback HTTP address (post, if task execution is successful, it must return a string containing 'success', otherwise, it will retry many times)},Example:
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

### Cancel Task
post /delay/cancel application/json {id: task id},Example:
```
curl -X 'POST' \
  'https://localhost:7008/Delay/cancel' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "id": "string"
}'
```
## Startup Method 

Navigate to the WebApplication1/dist directory, then run `docker-compose up -d`. Please note, grant docker permissions to this directory, or set the directory permissions to 777.

## Running Logs

If you start the service with docker-compose in the WebApplication1/dist directory, a log folder will be generated in this directory. You can find the running logs of the program here, in the following format:

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

## Performance

I ran a simple 30-second stress test on a 4-core server, the result is that the throughput of creating tasks is about 9000 QPS. As for task execution, it depends on the number of task threads you have opened, as well as the response speed of the callback address.

The specific test data is as follows:

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

## Notice

This project references modified modules of some other open-source projects and is for learning and communication purposes only. If this project is used for commercial purposes, it may lead to copyright disputes, so please use it carefully.

## Author

Copyright (c) 2023 lushitian404@gmail.com
