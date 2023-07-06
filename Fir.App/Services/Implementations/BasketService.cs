﻿using Fir.App.Context;
using Fir.App.Services.Interfaces;
using Fir.App.ViewModels;
using Fir.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Fir.App.Services.Implementations
{
    public class BasketService : IBasketService
    {
        private readonly FirDbContext _context;
        private readonly IHttpContextAccessor _httpContext;

        public BasketService(FirDbContext context, IHttpContextAccessor httpContext)
        {
            _context = context;
            _httpContext = httpContext;
        }
        public async Task AddBasket(int id)
        {
            if (!await _context.Products.AnyAsync(x => x.Id == id))
            {
                throw new Exception("Item is not found!");
            }
          
            var CookieJson = _httpContext?.HttpContext?.Request.Cookies["basket"];
            if (CookieJson == null)
            {
                List<BasketViewModel> basketViewModels = new List<BasketViewModel>();
                BasketViewModel basketViewModel = new BasketViewModel
                {
                    ProductId = id,
                    Count = 1
                };
                basketViewModels.Add(basketViewModel);
                CookieJson = JsonConvert.SerializeObject(basketViewModels);
                _httpContext?.HttpContext?.Response.Cookies.Append("basket", CookieJson);

            }
            else
            {
                List<BasketViewModel> basketViewModels = JsonConvert.DeserializeObject<List<BasketViewModel>>(CookieJson);
                BasketViewModel model = basketViewModels.FirstOrDefault(x => x.ProductId == id);
                if (model != null)
                {
                    model.Count++;
                }
                else
                {
                    BasketViewModel basketViewModel = new BasketViewModel
                    {
                        ProductId = id,
                        Count = 1
                    };
                    basketViewModels.Add(basketViewModel);
                }
                CookieJson = JsonConvert.SerializeObject(basketViewModels);
                _httpContext?.HttpContext?.Response.Cookies.Append("basket", CookieJson);
            }
        }

        public async Task<List<BasketItemViewModel>> GetAllBaskets()
        {
            var jsonBasket = _httpContext?.HttpContext?.Request.Cookies["basket"];

            if (jsonBasket != null)
            {
                List<BasketViewModel>? basketViewModels = JsonConvert
                         .DeserializeObject<List<BasketViewModel>>(jsonBasket);
                List<BasketItemViewModel> basketItemViewModels = new();
                foreach (var item in basketViewModels)
                {
                    Product? product = await _context.Products
                                      .Where(x => !x.IsDeleted && x.Id == item.ProductId)
                                       .Include(x => x.ProductImages)
                                       .Include(x => x.Discount)
                                       .FirstOrDefaultAsync();

                    if (product != null)
                    {
                        basketItemViewModels.Add(new BasketItemViewModel
                        {
                            ProductId = item.ProductId,
                            Count = item.Count,
                            Image = product.ProductImages.FirstOrDefault(x => x.isMain).Image,
                            Name = product.Name,
                            Price = product.Discount == null ? product.Price : (product.Price-(product.Price*((decimal)product.Discount.Percent/100)))
                        });

                    }
                }
                return basketItemViewModels;
            }
            return new List<BasketItemViewModel>();
        }


        public async Task Remove(int id)
        {
            var basketJson = _httpContext?.HttpContext?
                            .Request.Cookies["basket"];
            if (basketJson != null)
            {
                List<BasketViewModel>? basketViewModels = JsonConvert
                         .DeserializeObject<List<BasketViewModel>>(basketJson);
     


                BasketViewModel basketViewModel =  basketViewModels.FirstOrDefault(x => x.ProductId == id);
                if (basketViewModel != null)
                {
                    basketViewModels.Remove(basketViewModel);
                      basketJson = JsonConvert.SerializeObject(basketViewModels);
                    _httpContext?.HttpContext?.Response.Cookies.Append("basket", basketJson);
                }
            }
        }
    }
}