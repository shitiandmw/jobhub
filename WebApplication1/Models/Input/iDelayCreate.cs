using System.ComponentModel.DataAnnotations;

namespace JobHub.Models.Input
{
    /// <summary>
    /// 创建延时任务
    /// </summary>
    public class iDelayCreate
    {
        /// <summary>
        /// 任务id
        /// </summary>
        [Required]
        [StringLength(36, MinimumLength = 10, ErrorMessage = "Id长度必须在10-36位之间。")]
        public string id { get; set; }
        /// <summary>
        /// 回调地址(回调地址会通过postjson的方式把id传过去，如果返回内容中没有包含success,会进行重试，规则是 { 3, 5, 15, 30, 60, 180, 180, 1800, 1800, 3600 }，单位秒）
        /// </summary>
        public string? callback { get; set; }
        /// <summary>
        /// 回调授权（放到header的authorization）
        /// </summary>
        public string? authorization { get; set; }
        /// <summary>
        /// 触发时间（时间戳）
        /// </summary>
        [Required]
        public long timestamp { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int retry_index { get; set; }
    }
}
