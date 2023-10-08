using TrainScrapingWorkerService.Configuration;

namespace TrainScrapingWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Config config = Config.Load();
            RegularTask[] tasks = Array.Empty<RegularTask>()
                .Concat(config.DNYs.Select(c => new DnyScraper(c, _logger)))
                .Concat(new RegularTask[] { new DnyUploader(config, _logger) })
                .ToArray();

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (RegularTask regularTask in tasks)
                {
                    await regularTask.Tick();
                }

                await Task.Delay(300, stoppingToken);
            }

            foreach (RegularTask regularTask in tasks)
            {
                regularTask.Dispose();
            }
        }
    }
}