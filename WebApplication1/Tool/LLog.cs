using ServiceStack.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace JobHub.Tool
{
    public class LLog
    {
        private static string DIRECTORY_SEPARATOR
        {
            get
            {
                var result = AppSetting.Get("DIRECTORY_SEPARATOR");
                if (string.IsNullOrEmpty(result))
                    result = "/";
                return result;
            }
        }
        /// <summary>
        /// 等待更新的队列
        /// </summary>
        private readonly ConcurrentQueue<iLLog2> _waitQueue;

        /// <summary>
        /// 信号
        /// </summary>
        private readonly ManualResetEvent _mreextusers;


        private static LLog _bQueueEmail = new LLog();
        public LLog()
        {
            path = $"{AppDomain.CurrentDomain.BaseDirectory}LLogs2";
            _waitQueue = new ConcurrentQueue<iLLog2>();
            _mreextusers = new ManualResetEvent(false);

            Reg();
        }

        /// <summary>
        /// 最后一次写入日志的时间
        /// </summary>
        private static DateTime _lastlogtime = default;

        /// <summary>
        /// 等待超时时间 秒
        /// </summary>
        const int wait_timeout = 15;
        /// <summary>
        /// 等待一次性刷入队列的日志内容
        /// </summary>
        private static Dictionary<string, List<iLLog2>> waitWrite = new Dictionary<string, List<iLLog2>>();
        /// <summary>
        /// 守护进程的时钟 处理等待超时的日志
        /// </summary>
        private static Dictionary<string, List<string>> grard_timer_wait = new Dictionary<string, List<string>>();
        private static object grard_timer_wait_lock = new object();

   

        /// <summary>
        /// 另一个线程记录日志，只在程序初始化时调用一次
        /// </summary>
        public void Reg()
        {
            //启动工作线程
            Thread t = new Thread(new ThreadStart(Work));
            t.IsBackground = false;
            t.Start();

            //启动守护进程用来提醒长时间未输入数据
            Thread t1 = new Thread(new ThreadStart(Guard));
            t1.IsBackground = false;
            t1.Start();
        }

        /// <summary>
        /// 守护进程
        /// </summary>
        private void Guard()
        {
            while (true)
            {
                try
                {
                    #region //处理超时没有结束的日志串，自动打一个结束标记
                    List<string> datas = new List<string>();
                    var grard_timer_wait_key = DateTime.Now.ToString("yyyyMMddHHmmss");
                    lock (grard_timer_wait_lock)
                    {
                        if (grard_timer_wait.ContainsKey(grard_timer_wait_key))
                        {
                            datas = grard_timer_wait[grard_timer_wait_key];
                            grard_timer_wait.Remove(grard_timer_wait_key);
                        }
                    }
                    if (datas != null)
                        for (int i = 0; i < datas.Count; i++)
                        {
                            if (waitWrite.ContainsKey(datas[i]))
                            {
                                _AddQueue(new iLLog2()
                                {
                                    adddate = DateTime.Now,
                                    className = "Notice",
                                    content = "system end",
                                    threadId = datas[i],
                                    type = "debug"
                                });
                            }
                        }
                    #endregion

                    //若15秒没处理过数据，并且队列中存在等待刷入的日志
                    if (_lastlogtime.AddSeconds(15) < DateTime.Now && _waitQueue.Count > 0)
                        _SetEvent();
                }
                catch (Exception ex)
                {
                    throw;
                }
                Thread.Sleep(1000);
            }
        }
        /// <summary>
        /// 工作线程
        /// </summary>
        private void Work()
        {
            while (true)
            {
                // 等待信号通知
                _mreextusers.WaitOne();
                iLLog2 queue;

                List<iLLog2> queues = new List<iLLog2>();
                // 从列队中获取任务并处理
                while (_waitQueue.Count > 0 && _waitQueue.TryDequeue(out queue))
                {
                    queues.Add(queue);
                }

                //执行任务
                WorkDetail(queues);

                _lastlogtime = DateTime.Now;


                // 重新设置信号       
                _mreextusers.Reset();

                //Task.WaitAny(Task.Delay(1));

                Thread.Sleep(1);
            }
        }
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="queues"></param>
        /// <returns></returns>
        private void WorkDetail(List<iLLog2> queues)
        {
            //处理数据
            StringBuilder logContent = new StringBuilder();
            while (queues.Count > 0)
            {
                try
                {
                    string nowlogday = queues[0].adddate.ToString("yyyyMMdd");
                    string lastlogday = "";
                    if (queues.Count > 1)
                        lastlogday = queues[1].adddate.ToString("yyyyMMdd");


                    var threadId = queues[0].threadId;
                    queues[0].guid = Guid.NewGuid().ToString().Replace("-", "");
                    if (!string.IsNullOrEmpty(queues[0].threadId))
                    {
                        if (!waitWrite.ContainsKey(threadId))
                        {
                            waitWrite.Add(threadId, new List<iLLog2>());
                            var grard_timer_wait_key = DateTime.Now.AddSeconds(wait_timeout).ToString("yyyyMMddHHmmss");
                            lock (grard_timer_wait_lock)
                            {
                                if (!grard_timer_wait.ContainsKey(grard_timer_wait_key))
                                    grard_timer_wait.Add(grard_timer_wait_key, new List<string>());
                                grard_timer_wait[grard_timer_wait_key].Add(threadId);
                            }

                        }
                        waitWrite[threadId].Add(queues[0]);

                        //收到结束通知，开始刷日志
                        if (queues[0].className.ToString() == "Notice")
                        {
                            //通知将当前请求的日志刷入文件
                            if (queues[0].content == "request end" || queues[0].content == "system end")
                            {
                                logContent.AppendLine($"###{queues[0].guid} {waitWrite[threadId][0].adddate.ToString($"yyyy-MM-dd HH:mm:ss.fff")} {queues[0].type} {queues[0].className} begin--------------------");
                                for (int i = 0; i < waitWrite[threadId].Count; i++)
                                {
                                    logContent.AppendLine($"[{waitWrite[threadId][i].adddate.ToString($"yyyy-MM-dd HH:mm:ss.fff")}][{waitWrite[threadId][i].type}][{waitWrite[threadId][i].className}]：{waitWrite[threadId][i].content}");
                                }
                                logContent.AppendLine($"end -------------------------");
                            }
                            //通知将当前请求的日志清除,无需刷入文件
                            if (queues[0].content == "log clear") { }

                            waitWrite.Remove(threadId);
                        }
                    }
                    //整理内容，准备刷入文件
                    else logContent.AppendLine($"###{queues[0].guid} {queues[0].threadId} {queues[0].adddate.ToString($"yyyy-MM-dd HH:mm:ss.fff")} {queues[0].type} {queues[0].className} {queues[0].content}");

                    //刷日志内容到文件中
                    if (nowlogday != lastlogday && logContent.Length > 0)
                    {
                        #region ##是否存在保存日志的文件夹##
                        string filename = $"{path}{DIRECTORY_SEPARATOR}{nowlogday}{DIRECTORY_SEPARATOR}{Dns.GetHostName()}_Info.log";
                        var dir = filename.Substring(0, filename.LastIndexOf(DIRECTORY_SEPARATOR));
                        if (!Directory.Exists(dir))
                        {
                            //创建今天的日志文件夹
                            Directory.CreateDirectory(dir);

                            //删除五天前的日志
                            string old_dir = $"{path}{DIRECTORY_SEPARATOR}{(DateTime.Now.AddDays(-5)):yyyyMMdd}{DIRECTORY_SEPARATOR}";
                            if (Directory.Exists(old_dir)) Directory.Delete(old_dir, true);
                        }
                        #endregion

                        //追加内容到这个文件中
                        string content = logContent.ToString();
                        StreamWriter sw = null;
                        try
                        {
                            sw = File.AppendText(filename);
                            sw.Write(content);
                        }
                        catch (Exception ex)
                        {
                            Error("LLog", "刷入日志内容错误" + ex.Message);
                        }
                        finally
                        {
                            if (sw != null)
                            {
                                sw.Flush();
                                sw.Close();
                                sw.Dispose();
                            }
                        }

                        //清空刷入内容
                        logContent.Clear();
                    }

                }
                catch (Exception ex)
                {
                    Custom("LLogError", "日志系统发生严重错误", $"ex.message:{ex.Message};ex.stacktrace:{ex.StackTrace}");
                }
                finally
                {
                    queues.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 日志目录
        /// </summary>
        public static string path;
        /// <summary>
        /// 记录数据库
        /// </summary>
        /// <param name="connname">数据库连接字符</param>
        /// <param name="content">内容</param>
        public  void DbWrite(string connname, string content)
        {
            AddQueue("DbWrite", connname, content);
        }
        /// <summary>
        /// 记录DEBUG
        /// </summary>
        /// <param name="className">方法名</param>
        /// <param name="content">内容</param>
        public  void Debug(object className, string content)
        {
            AddQueue("DEBUG", className, content);
        }
        /// <summary>
        /// 记录WX信息
        /// </summary>
        /// <param name="className">方法名</param>
        /// <param name="content">内容</param>
        public  void Debugwx(object className, string content)
        {
            AddQueue("DebugWx", className, content);
        }
        public  void Info(object className, string content, string hashcode = "")
        {
            AddQueue("INFO", className, content, hashcode);
        }
        public  void Error(object className, string content)
        {
            AddQueue("ERROR", className, content);
        }
        public  void Pay(object className, string content)
        {
            AddQueue("PAY", className, content);
        }
        /// <summary>
        /// 自定义日志
        /// </summary>
        /// <param name="name">日志文件名</param>
        /// <param name="className">方法名</param>
        /// <param name="content">内容</param>
        public  void Custom(string name, object className, string content)
        {
            AddQueue(name, className, content);
        }
        public  void Custom(string name, object className, string content, Action action)
        {
            action();
        }


        /// <summary>
        /// 往队列里添加一个内容
        /// </summary>
        /// <param name="extuser"></param>
        public void _AddQueue(iLLog2 queue)
        {
            _waitQueue.Enqueue(queue);

            // 通知线程执行任务
            if (_waitQueue.Count > 100 || _lastlogtime.AddSeconds(2) <= DateTime.Now)
                _mreextusers.Set();
        }
        public void _SetEvent()
        {
            _mreextusers.Set();
        }

        /// <summary>
        /// 添加一个任务
        /// </summary>
        /// <param name="extuser"></param>
        public void AddQueue(string type, object className, string content, string hashcode = "")
        {
            //if (string.IsNullOrEmpty(hashcode))
            //{
            //    try
            //    {
            //        if (HttpContext.Current != null)
            //            hashcode = HttpContext.Current.Request.GetHashCode().ToString();
            //    }
            //    catch
            //    {
            //    }
            //}
            var threadId = "";
            if (!string.IsNullOrEmpty(hashcode))
                threadId = $"hashcode{hashcode};";
            var newlog = new iLLog2()
            {
                adddate = DateTime.Now,
                className = className.ToString(),
                content = content,
                type = type,
                threadId = threadId
            };

            _AddQueue(newlog);

        }
    }


    /// <summary>
    /// 日志模型
    /// </summary>
    public class iLLog2
    {
        /// <summary>
        /// 日志唯一ID
        /// </summary>
        public string guid { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 所在类名
        /// </summary>
        public string className { get; set; }
        /// <summary>
        /// 日志内容
        /// </summary>
        public string content { get; set; }
        /// <summary>
        /// 日志产生时间
        /// </summary>
        public DateTime adddate { get; set; }
        /// <summary>
        /// 线程ID
        /// </summary>
        public string threadId { get; set; }
    }


}
