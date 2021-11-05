namespace WorkerService
{
    public class SocketConfig
    {
        public static string Ip { get; set; }
        public static string Port { get; set; }
    }

    public class Threshold
    {
        public static double Cpu { get; set; }
        public static double Ram { get; set; }
        public static double Disk { get; set; }
    }
}
