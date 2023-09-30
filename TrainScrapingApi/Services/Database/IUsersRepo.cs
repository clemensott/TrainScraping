namespace TrainScrapingApi.Services.Database
{
    public interface IUsersRepo
    {
        Task<bool> AuthAsync(string token);
    }
}
