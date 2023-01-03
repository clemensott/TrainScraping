using TrainScrapingWorkerService;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSystemd()
    .UseWindowsService()
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
