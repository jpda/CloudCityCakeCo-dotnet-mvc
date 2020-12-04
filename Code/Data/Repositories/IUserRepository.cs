using System.Threading.Tasks;
using CloudCityCakeCo.Models.Entities;

namespace CloudCityCakeCo.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User> AddUserAsync(User user);
        Task<User> GetUserByPhoneNumberAsync(string phoneNumber);
    }

    // public class AzureAdB2CUserRepository : IUserRepository
    // {
    //     public Task<User> AddUserAsync(User user)
    //     {
    //         throw new System.NotImplementedException();
    //     }

    //     public Task<User> GetUserByPhoneNumberAsync(string phoneNumber)
    //     {
    //         throw new System.NotImplementedException();
    //     }
    // }
}