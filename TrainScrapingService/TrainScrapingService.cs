﻿using System;
using System.Linq;
using System.ServiceProcess;
using TrainScraping.Configuration;

namespace TrainScraping
{
    public partial class TrainScrapingService : ServiceBase
    {
        private Config config;
        private DnyScraper[] dnyScrapers;

        public TrainScrapingService()
        {
            InitializeComponent();
        }

        protected async override void OnStart(string[] args)
        {
            try
            {
                config = Config.Load();
                Logger.Log($"TrainScrapingService:OnStart:config_loaded:{config.DNYs.Length}");

                dnyScrapers = config.DNYs.Select(dny => new DnyScraper(dny)).ToArray();
                Logger.Log("TrainScrapingService:OnStart:dnyScrapers_created");

                foreach (DnyScraper scraper in dnyScrapers)
                {
                    scraper.StartTimer();
                }
                Logger.Log("TrainScrapingService:OnStart:dnyScrapers_timer_started");

                foreach (DnyScraper scraper in dnyScrapers)
                {
                    await scraper.Scrape();
                }
                Logger.Log("TrainScrapingService:OnStart:dnyScrapers_scraped");
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                throw;
            }
        }

        protected override void OnStop()
        {
            foreach (DnyScraper scraper in dnyScrapers)
            {
                scraper.Dispose();
            }
        }
    }
}
