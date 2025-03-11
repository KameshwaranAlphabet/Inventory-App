using Inventree_App.Models;

namespace Inventree_App.Service
{
    public interface ICustomerService
    {
        /// <summary>
        /// Generate JWT token for user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        string GenerateJwtToken(Customer user);
    }
}
