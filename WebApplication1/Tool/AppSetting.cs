using Microsoft.Extensions.Configuration.Json;

namespace JobHub.Tool
{
    public class AppSetting
    {
        static IConfiguration Configuration { get; set; }
        static string ContentPath { get; set; }

        static object configLock = new object();

        public AppSetting()
        {
            string path = "appsettings.json";
            Configuration = new ConfigurationBuilder().SetBasePath(ContentPath).Add(new JsonConfigurationSource
            {
                Path = path,
                Optional = false,
                ReloadOnChange = true
            }).Build();
        }
        public AppSetting(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// 获得配置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Get(string key)
        {
            //if (Configuration == null)
            //    Init();
            return Configuration == null ? "" : Configuration[key];
        }

        ///// <summary>
        ///// 获得配置
        ///// </summary>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //public static T? Get<T>(string key)
        //{
        //    //if (Configuration == null)
        //    //    Init();
        //    T? t = default;
        //    Configuration.Bind(key,t);
        //    return t;
        //}
        public static void Init()
        {
            lock (configLock)
            {
                if (Configuration == null)
                {
                    string path = "appsettings.json";
                    Configuration = new ConfigurationBuilder().SetBasePath(ContentPath).Add(new JsonConfigurationSource
                    {
                        Path = path,
                        Optional = false,
                        ReloadOnChange = true
                    }).Build();
                }
            }
        }
    }
}
