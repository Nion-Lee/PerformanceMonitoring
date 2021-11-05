using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkerService.Workers;
using perfCter = System.Diagnostics.PerformanceCounter;

namespace WorkerService
{
    public class PerformanceCounter : BackgroundService
    {
        private int _driveCounts;
        private readonly double[] _disks;
        private int _largestNumberOfLines;
        private static string _outputData;
        private StringBuilder _dataBuilder;
        private static string _outputWarning;
        private StringBuilder _warningBuilder;
        private readonly Regulation _monitoring;
        private const string _textSecure = "Secure.";
        private readonly ILogger<PerformanceCounter> _logger;
        private readonly perfCter _cpu, _ram, _fileRead, _fileWrite, _process;
        private (double cpu, double ram, double fileRead, double fileWrite, double process) _factor;

        public PerformanceCounter(ILogger<PerformanceCounter> logger)
        {
            _logger = logger;

            _disks = new double[20];
            _monitoring = new Regulation();
            _dataBuilder = new StringBuilder();
            _warningBuilder = new StringBuilder();

            _cpu = new perfCter("Processor", "% Processor Time", "_Total");
            _ram = new perfCter("Memory", "% Committed Bytes In Use");
            _fileRead = new perfCter("System", "File Read Operations/sec");
            _fileWrite = new perfCter("System", "File Write Operations/sec");
            _process = new perfCter("System", "Processes");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    CallbackPrintOut();
                    CallbackSafetyCheck();
                    CallbackRecord();

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }

            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        public void CallbackPrintOut()
        {
            var drives = DriveInfo.GetDrives();
            _driveCounts = drives.Length;

            SetPerfData(drives);
            SetOutputData(drives);
        }

        private void SetPerfData(DriveInfo[] drives)
        {
            for (int i = 0; i < drives.Length; i++)
            {
                if (drives[i].IsReady)
                    _disks[i] = GetDiskUsage(drives, in i);
            }
            _factor.cpu = Math.Round(_cpu.NextValue(), 2);
            _factor.ram = Math.Round(_ram.NextValue(), 2);
            _factor.fileRead = Math.Round(_fileRead.NextValue(), 2);
            _factor.fileWrite = Math.Round(_fileWrite.NextValue(), 2);
            _factor.process = _process.NextValue();
        }

        private double GetDiskUsage(DriveInfo[] drives, in int i)
        {
            return
                Math.Round(
                (double)(drives[i].TotalSize - drives[i].AvailableFreeSpace) /
                (double)(drives[i].TotalSize) * 100, 2);
        }

        private void SetOutputData(DriveInfo[] drives, bool toJson = false)
        {
            if (toJson)
                SetDataToJson(drives);
            else
                SetDataToPlain(drives);
        }

        private void SetDataToJson(DriveInfo[] drives)
        {
            var jObject = (JObject)JToken.FromObject(
                new JsonTemplate()
                {
                    Cpu = _factor.cpu,
                    Ram = _factor.ram,
                    FileRead = _factor.fileRead,
                    FileWrite = _factor.fileWrite,
                    Process = (int)_factor.process
                });

            for (int i = 0; i < drives.Length; i++)
                _dataBuilder.Append($"'{drives[i].Name[..^2]}': {_disks[i]},");

            _dataBuilder.Remove(_dataBuilder.Length - 1, 1);
            jObject["Disks"] = JObject.Parse("{" + _dataBuilder.ToString() + "}");

            _outputData = jObject.ToString();
            _dataBuilder.Clear();
        }

        private void SetDataToPlain(DriveInfo[] drives)
        {
            for (int i = 0; i < drives.Length; i++)
                _dataBuilder.AppendLine($"Usage {drives[i].Name}: {_disks[i]}%");

            _dataBuilder.Append(
                $"CPU: {_factor.cpu}%".PadRight(30) + "\n" +
                $"RAM: {_factor.ram}%".PadRight(30) + "\n" +
                $"File read: {_factor.fileRead} reqs/s".PadRight(30) + "\n" +
                $"File write: {_factor.fileWrite} reqs/s".PadRight(30) + "\n" +
                $"Process: {_factor.process} counts".PadRight(30) + "\n");

            _outputData = _dataBuilder.ToString();
            _dataBuilder.Clear();
        }

        public void CallbackSafetyCheck()
        {
            if (_factor == default)
                return;
            GetWarningMessage();
            ToStringWhileError();
        }

        private void GetWarningMessage()
        {
            bool isSafe; string warning;
            int lines = 0;

            (isSafe, warning) = _monitoring.Examine(_factor.cpu, TypeEnum.Cpu);
            if (!isSafe) { _warningBuilder.AppendLine(warning.PadRight(65)); lines++; }

            (isSafe, warning) = _monitoring.Examine(_factor.ram, TypeEnum.Ram);
            if (!isSafe) { _warningBuilder.AppendLine(warning.PadRight(65)); lines++; }

            (isSafe, warning) = _monitoring.Examine(_factor.fileRead, TypeEnum.FileRead);
            if (!isSafe) { _warningBuilder.AppendLine(warning.PadRight(65)); lines++; }

            (isSafe, warning) = _monitoring.Examine(_factor.fileWrite, TypeEnum.FileWrite);
            if (!isSafe) { _warningBuilder.AppendLine(warning.PadRight(65)); lines++; }

            (isSafe, warning) = _monitoring.Examine(_factor.process, TypeEnum.Process);
            if (!isSafe) { _warningBuilder.AppendLine(warning.PadRight(65)); lines++; }

            for (int i = 0; i < _driveCounts; i++)
            {
                (isSafe, warning) = _monitoring.Examine(_disks[i], TypeEnum.Disk);
                if (!isSafe) { _warningBuilder.AppendLine(warning.PadRight(65)); lines++; }
            }

            if (_largestNumberOfLines < lines)
                _largestNumberOfLines = lines;

            for (int i = 0; i < _largestNumberOfLines - lines; i++)
                _warningBuilder.AppendLine(string.Empty.PadRight(65));
        }

        private void ToStringWhileError()
        {
            if (_warningBuilder.Length != 0)
            {
                _outputWarning = _warningBuilder.ToString();
                _warningBuilder.Clear();
            }
            else
            {
                _outputWarning = _textSecure;
            }
        }

        public void CallbackRecord()
        {

        }

        public static string GetPrintOutData()
        {
            return _outputData;
        }

        public static string GetPrintOutError()
        {
            return _outputWarning;
        }
    }
}
