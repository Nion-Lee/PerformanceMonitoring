namespace WorkerService.Workers
{
    public class Regulation
    {
        public (bool, string) Examine(double value, TypeEnum type)
        {
            return type switch
            {
                TypeEnum.Cpu when value > Threshold.Cpu => (false, Warn(type)),
                TypeEnum.Ram when value > Threshold.Ram => (false, Warn(type)),
                TypeEnum.Disk when value > Threshold.Disk => (false, Warn(type)),
                _ => (true, null)
            };
        }

        private string Warn(TypeEnum type)
        {
            return type switch
            {
                TypeEnum.Cpu => nameof(TypeEnum.Cpu),
                TypeEnum.Ram => nameof(TypeEnum.Ram),
                TypeEnum.Disk => nameof(TypeEnum.Disk),
                _ => null
            }
            + " has exceeded the range of safety! Please be cautious.";
        }
    }
}
