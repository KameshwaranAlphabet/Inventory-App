using AutoMapper;
using Inventree_App.Dto;
using Inventree_App.Models;

namespace Inventree_App.Mapping
{
    public class CartItemProfile : Profile
    {
        public CartItemProfile()
        {
            CreateMap<CartItem, CartItemDto>(); // Define mapping
        }
    }
}
