using JobHub.Tool;
using ServiceStack.Redis;
namespace JobHub.Cache
{
    public class RedisManager
    {
        LLog _log;
        /// <summary>  
        /// redis配置文件信息  
        /// </summary>  
        string RedisPath = AppSetting.Get("Cache:Redis:Path");
        PooledRedisClientManager _prcm;

        /// <summary>  
        /// 静态构造方法，初始化链接池管理对象  
        /// </summary>  
        public RedisManager(LLog lLog)
        {
            _log = lLog;
            CreateManager();
        }

        /// <summary>  
        /// 创建链接池管理对象  
        /// </summary>  
        private  void CreateManager()
        {
            _log.Info("GetClient", RedisPath);
            _prcm = CreateManager(new string[] { RedisPath }, new string[] { RedisPath });

        }

        private static PooledRedisClientManager CreateManager(string[] readWriteHosts, string[] readOnlyHosts)
        {
            //WriteServerList：可写的Redis链接地址。  
            //ReadServerList：可读的Redis链接地址。  
            //MaxWritePoolSize：最大写链接数。  
            //MaxReadPoolSize：最大读链接数。  
            //AutoStart：自动重启。  
            //LocalCacheTime：本地缓存到期时间，单位:秒。  
            //RecordeLog：是否记录日志,该设置仅用于排查redis运行时出现的问题,如redis工作正常,请关闭该项。  
            //RedisConfigInfo类是记录redis连接信息，此信息和配置文件中的RedisConfig相呼应  
            try
            {

                // 支持读写分离，均衡负载   
                return new PooledRedisClientManager(readWriteHosts, readOnlyHosts, new RedisClientManagerConfig
                {
                    MaxWritePoolSize = 1000, // “写”链接池链接数   
                    MaxReadPoolSize = 1000, // “读”链接池链接数   
                    AutoStart = true,
                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static IEnumerable<string> SplitString(string strSource, string split)
        {
            return strSource.Split(split.ToArray());
        }

        /// <summary>  
        /// 客户端缓存操作对象  
        /// </summary>  
        public  IRedisClient GetClient()
        {
            //if (_prcm == null)
            //{
            //    CreateManager();
            //}
            IRedisClient result = _prcm.GetClient();
            result.ConnectTimeout = 1000;
            //result.Password = System.Configuration.ConfigurationSettings.AppSettings["RedisKey"];
            return result;
        }

    }
}
