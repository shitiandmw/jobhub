namespace JobHub.Models.Out
{
    public class oData<T>
    {
        public int code { get; set; } = 200;
        public T data { get;set; }
        public string message { get; set; }
    }
}
