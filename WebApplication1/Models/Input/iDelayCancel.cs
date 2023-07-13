using System.ComponentModel.DataAnnotations;

namespace JobHub.Models.Input
{
    public class iDelayCancel
    {
        /// <summary>
        /// 任务id
        /// </summary>
        [Required]
        public string id { get; set; }
    }
}
