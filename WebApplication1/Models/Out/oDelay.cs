namespace JobHub.Models.Out
{
    public class oDelay
    {

        /// <summary>
        /// 任务id
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 回调地址
        /// </summary>
        public string? callback { get; set; }
        /// <summary>
        /// 回调授权（放到header的authorization）
        /// </summary>
        public string? authorization { get; set; }
        /// <summary>
        /// 触发时间（时间戳）
        /// </summary>
        public long timestamp { get; set; }
        /// <summary>
        /// 重试次数
        /// </summary>
        public int retry_index { get; set; }
    }
}
