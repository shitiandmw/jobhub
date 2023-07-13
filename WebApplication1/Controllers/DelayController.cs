using JobHub.Cache;
using JobHub.Models.Input;
using JobHub.Models.Out;
using JobHub.Tool;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ServiceStack.Redis;
using System.Text;

namespace JobHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DelayController : ControllerBase
    {
        static string RedisPrefix = AppSetting.Get("Cache:Redis:Prefix");
        static string DelayHubKey = AppSetting.Get("DelayHubKey");
        static string DelayListKey = $"{RedisPrefix}:{DelayHubKey}";
        RedisManager _redisManager;
        LLog _log;

        public DelayController(RedisManager redisManager, LLog lLog)
        {
            _redisManager = redisManager;
            _log = lLog;
        }
        /// <summary>
        /// 生成一个随机数
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        int RandomN(int min, int max)
        {
            int rtn = 0;
            Random r = new Random();
            byte[] buffer = Guid.NewGuid().ToByteArray();
            int iSeed = BitConverter.ToInt32(buffer, 0);
            r = new Random(iSeed);
            rtn = r.Next(min, max + 1);
            return rtn;
        }
        /// <summary>
        /// 添加延时任务【测试】
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpGet("create_test")]
        public oData<int> CreateTest()
        {
            iDelayCreate input = new iDelayCreate();
            input.id = Guid.NewGuid().ToString();
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sleep_s = RandomN(1, 3600);
            timestamp += sleep_s;
            input.callback = "";
            input.timestamp = timestamp;
            input.retry_index = 0;
            _log.Info("CreateTest", $"延时{sleep_s}秒");
            var itemvalue = JsonConvert.SerializeObject(input);
            using (RedisClient redis = (RedisClient)_redisManager.GetClient())
            {
                redis.ZAdd(DelayListKey, input.timestamp, Encoding.UTF8.GetBytes(input.id));
                redis.HSet($"{DelayListKey}:Table", Encoding.UTF8.GetBytes(input.id), Encoding.UTF8.GetBytes(itemvalue));
            }
            return new oData<int>();
        }
        /// <summary>
        /// 添加延时任务
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("create")]
        public oData<int> Create(iDelayCreate input)
        {
            input.retry_index = 0; 
            var itemvalue = JsonConvert.SerializeObject(input);
            _log.Info("Create input", itemvalue);
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _log.Info("Create time", $"{ input.timestamp - timestamp}秒后执行");
            using (RedisClient redis = (RedisClient)_redisManager.GetClient())
            {
                redis.ZAdd(DelayListKey, input.timestamp, Encoding.UTF8.GetBytes(input.id));
                redis.HSet($"{DelayListKey}:Table", Encoding.UTF8.GetBytes(input.id), Encoding.UTF8.GetBytes(itemvalue));
            }
            return new oData<int>();
        }

        /// <summary>
        /// 取消延时任务
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("cancel")]
        public oData<int> Cancel(iDelayCancel input)
        {
            _log.Info("Cancel input", input.id);
            using (RedisClient redis = (RedisClient)_redisManager.GetClient())
            {
                var value = redis.HGet($"{DelayListKey}:Table", Encoding.UTF8.GetBytes(input.id));
                if (value != null)
                {
                    redis.HDel($"{DelayListKey}:Table", Encoding.UTF8.GetBytes(input.id));
                    redis.ZRem(DelayListKey, Encoding.UTF8.GetBytes(input.id));
                }
            }
            return new oData<int>();
        }


        /// <summary>
        /// 还有哪些延时任务（列表）最多显示100条
        /// </summary>
        /// <returns></returns>
        [HttpGet("list")]
        public oData<List<oDelay>> List()
        {
            List<oDelay> result = new List<oDelay>();
            byte[][] result_;
            using (RedisClient redis = (RedisClient)_redisManager.GetClient())
            {
                result_ = redis.ZRangeWithScores(DelayListKey, 0, 100 - 1);
            }
            var i = 0;
            while (true)
            {
                if (i >= result_.Length) break;
                var key = result_[i];
                byte[] value;
                using (RedisClient redis = (RedisClient)_redisManager.GetClient())
                {
                    value = redis.HGet($"{DelayListKey}:Table", key);
                }
                if (value != null)
                    result.Add(JsonConvert.DeserializeObject<oDelay>(Encoding.UTF8.GetString(value)));
                i += 2;
            }
            return new oData<List<oDelay>>()
            {
                data = result
            };
        }
    }
}
