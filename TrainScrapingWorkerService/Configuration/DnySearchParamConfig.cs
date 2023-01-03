namespace TrainScrapingWorkerService.Configuration
{
    public struct DnySearchParamConfig
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public int? MinValue { get; set; }

        public int? MaxValue { get; set; }
    }
}
