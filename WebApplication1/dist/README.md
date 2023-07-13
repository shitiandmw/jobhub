# 任务调度系统
这是一个轻量级的任务调度系统，用于执行延时任务。创建任务使用http协议，任务执行后会回调指定的http地址。

## 添加任务
post /delay/create application/json {id:任务id, timestamp: 啥时候触发, authorization: 任务密钥 , callback: 回调的http地址(post ，如果任务执行成功必须返回包含success的字符串，否则会重试)}

## 取消任务
post /delay/cancel application/json {id:任务id} 