using Flurl;
using JobHub.Cache;
using JobHub.Models.Out;
using JobHub.Tool;
using Newtonsoft.Json;
using ServiceStack;
using ServiceStack.Redis;
using System.Text;
using Flurl;
using Flurl.Http;
using System.Text.RegularExpressions;
using System;

namespace JobHub.Service
{
    /// <summary>
    /// 执行任务的类
    /// </summary>
    public class JobProcessor
    {
        static string RedisPrefix = AppSetting.Get("Cache:Redis:Prefix");
        static string DelayHubKey = AppSetting.Get("DelayHubKey");
        static string DelayListKey = $"{RedisPrefix}:{DelayHubKey}";
        int _maxtask; 
        LLog _log;
        RedisManager _redisManager;
        public JobProcessor(
            LLog lLog,
            RedisManager redisManager)
        {
            _log = lLog;
            _redisManager = redisManager;
            _log.Info("JobProcessor", "begin");
            string MaxTask = AppSetting.Get("JobProcessor:MaxTask");
            if (string.IsNullOrEmpty(MaxTask))
                _maxtask = 100;
            else _maxtask = Convert.ToInt32(MaxTask);
            Reg();
        }

        public void Reg()
        {
            Thread t = new Thread(new ThreadStart(Work));
            t.IsBackground = false;
            t.Start();

        }

        /// <summary>
        /// 工作线程
        /// </summary>
        private void Work()
        {
            List<Task> taskList = new List<Task>();
            long index = 0;
            while (true)
            {
                try
                {
                    var delay_datas = TakeDelayData();
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if(index%60==0 || (delay_datas != null && delay_datas.Count > 0))
                        _log.Info("TakeDelayData", $"time:{timestamp};result:{(delay_datas == null ? 0 : delay_datas.Count)}");
                    if (delay_datas != null && delay_datas.Count >0)
                    {
                        for (int i = 0; i < delay_datas.Count; i++)
                        {
                            taskList.Add(WorkDetail(delay_datas[i]));
                            if (taskList.Count > _maxtask)
                            {
                                Task.WaitAny(taskList.ToArray(), new TimeSpan(0, 0, 10));
                                taskList = taskList.Where(t => t.Status != TaskStatus.RanToCompletion).ToList();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("JobProcessor", $"任务调度异常：{ex.Message}\r\n{ex.StackTrace}");
                    continue;
                }
                finally
                {
                    Thread.Sleep(1000);
                    index++;
                }
            }
        }

        /// <summary>
        /// 弹出当前应该执行的任务
        /// </summary>
        /// <returns></returns>
        public List<oDelay> TakeDelayData()
        {
            List<oDelay> result = new List<oDelay>();
            string selfMark;
            if (Lock(DelayListKey, out selfMark, 10))
            {
                try
                {
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    byte[][] result_;
                    using (RedisClient redis = (RedisClient)_redisManager.GetClient())
                    {
                        result_ = redis.ZRangeByScore(DelayListKey,0,timestamp,0,100);
                    }
                    if(result_!=null && result_.Length > 0)
                    {
                        for (int i = 0; i < result_.Length; i++)
                        {
                            byte[] key = result_[i];
                            byte[] value;
                            using (RedisClient redis = (RedisClient)_redisManager.GetClient())
                            {
                                value = redis.HGet($"{DelayListKey}:Table", key);
                                redis.HDel($"{DelayListKey}:Table", key);
                                redis.ZRem(DelayListKey, key);
                            }
                            if (value != null)
                                result.Add(JsonConvert.DeserializeObject<oDelay>(Encoding.UTF8.GetString(value)));
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    UnLock(DelayListKey, selfMark);
                }
            }
            return result;
        }

        /// <summary>
        /// 任务执行
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private async Task WorkDetail(oDelay info)
        {
            // 重试规则，单位秒
            var reply_sleep = new int[] { 3, 5, 15, 30, 60, 180, 180, 1800, 1800, 3600 };

            try
            {
                var post_data = new { 
                    info.id,
                    info.retry_index
                };
                _log.Info("WorkDetail", $"url:{info.callback};post_data:{JsonConvert.SerializeObject(post_data)}");
                var response = await info.callback
                    .WithTimeout(3)
                    .WithOAuthBearerToken(info.authorization)
                    .PostJsonAsync(post_data);
                if (response == null) throw new Exception("回调地址无响应");
                string result = (await response.GetStringAsync()) ??"";
                _log.Info("WorkDetail", $"url:{info.callback};result:{result}");
                if (!result.ToLower().Contains("success")) throw new Exception($"响应错误：{result}");
            }
            catch (Exception ex)
            {
                _log.Error("WorkDetail", $"{ex.Message}\n{ex.StackTrace}");
                string pattern = @"^(http|https)\://([a-zA-Z0-9\-\.]+)\.[a-zA-Z0-9]{2,3}(:[a-zA-Z0-9]*)?";
                Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                bool isValid = reg.IsMatch(info.callback??"");
                if (!isValid)
                    _log.Info("retry", "no url :"+ info.callback);
                // 进行重试
                if (info.retry_index < reply_sleep.Count() && isValid)
                {
                    info.retry_index++;
                    // 计算下次重试的时间
                    info.timestamp += reply_sleep[info.retry_index];
                    var itemvalue = JsonConvert.SerializeObject(info);
                    using (RedisClient redis = (RedisClient)_redisManager.GetClient())
                    {
                        redis.ZAdd(DelayListKey, info.timestamp, Encoding.UTF8.GetBytes(info.id));
                        redis.HSet($"{DelayListKey}:Table", Encoding.UTF8.GetBytes(info.id), Encoding.UTF8.GetBytes(itemvalue));
                    }
                }
            }
        }

        /// <summary>
        /// 获取一个redis分布式锁
        /// </summary>
        /// <param name="key"></param>
        /// <param name="selfMark"></param>
        /// <param name="lockExpirySeconds"></param>
        /// <returns></returns>
        private bool Lock(string key, out string selfMark, int lockExpirySeconds = 10)
        {
            bool result = false;
            selfMark = Guid.NewGuid().ToString("N");
            
            using (RedisClient redisClient = (RedisClient)_redisManager.GetClient())
            {
                string lockKey = key + ":Lock" ;

                string script = string.Format("if redis.call('SETNX', KEYS[1], ARGV[1]) == 1 then redis.call('PEXPIRE',KEYS[1],{0}) return 1 else return 0 end", lockExpirySeconds * 1000);

                if (redisClient.ExecLuaAsInt(script, new[] { lockKey }, new[] { selfMark }) == 1)
                {
                    result = true;
                }
            }
            return result; 
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="key">锁键</param>
        /// <param name="selfMark">释放锁的标记</param>
        public string UnLock(string key, string selfMark)
        {
            string result = "";
            string lockKey = key + ":Lock";
            using (RedisClient redisClient = (RedisClient)_redisManager.GetClient())
            {
                var script = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
                result = redisClient.ExecLuaAsString(script, new[] { lockKey }, new[] { selfMark });
            }
            return result;
        }
    }
}
