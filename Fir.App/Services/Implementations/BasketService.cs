﻿using Fir.App.Context;
using Fir.App.Services.Interfaces;
using Fir.App.ViewModels;
using Fir.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Fir.App.Services.Implementations
{
    public class BasketService : IBasketService
    {
        private readonly FirDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly UserManager<AppUser> _userManager;
        public BasketService(FirDbContext context, IHttpContextAccessor httpContext, UserManager<AppUser> userManager)
        {
            _context = context;
            _httpContext = httpContext;
            _userManager = userManager;
        }
        public async Task AddBasket(int id,int?count)
        {
            if (!await _context.Products.AnyAsync(x => x.Id == id))
            {
                throw new Exception("Item is not found!");
            }
            if (_httpContext.HttpContext.User.Identity.IsAuthenticated && _httpContext.HttpContext.User.IsInRole("User"))
            {
                AppUser appUser = await _userManager.FindByNameAsync(_httpContext.HttpContext.User.Identity.Name);
                Basket? basket = await _context.Baskets.
                  Include(x => x.basketItems.Where(y=>!y.IsDeleted)).ThenInclude(x=>x.Product)
                  .Where(x => !x.IsDeleted && x.AppUser.Id == appUser.Id).FirstOrDefaultAsync();
                if (basket == null)
                {
                    basket = new Basket
                    {
                        AppUserId = appUser.Id,
                        CreatedDate = DateTime.Now
                    };
                    await _context.AddAsync(basket);
                    BasketItem basketItem = new BasketItem
                    {
                        Basket = basket,
                        ProductId = id,
                        ProductCount = count??1

                    };
                    await _context.AddAsync(basketItem);
                }
                else
                {
                    BasketItem basketItem = basket.basketItems.FirstOrDefault(x => x.ProductId == id);
                    if (basketItem != null)
                    {
                        basketItem.ProductCount+=count??+1;
                    }
                    else
                    {
                         basketItem = new BasketItem
                        {
                            Basket = basket,
                            ProductId = id,
                            ProductCount = count??1

                        };
                        await _context.AddAsync(basketItem);

                    }

                }
                await _context.SaveChangesAsync();  
            }
            else
            {
            var CookieJson = _httpContext?.HttpContext?.Request.Cookies["basket"];
            if (CookieJson == null)
            {
                List<BasketViewModel> basketViewModels = new List<BasketViewModel>();
                BasketViewModel basketViewModel = new BasketViewModel
                {
                    ProductId = id,
                    Count = count??1
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
                    model.Count+=count??1 ;
                }
                else
                {
                    BasketViewModel basketViewModel = new BasketViewModel
                    {
                        ProductId = id,
                        Count = count??1
                    };
                    basketViewModels.Add(basketViewModel);
                }
                CookieJson = JsonConvert.SerializeObject(basketViewModels);
                _httpContext?.HttpContext?.Response.Cookies.Append("basket", CookieJson);
            }
            }
        }
        public async Task<List<BasketItemViewModel>> GetAllBaskets() 
        {
          
            if(_httpContext.HttpContext.User.Identity.IsAuthenticated && _httpContext.HttpContext.User.IsInRole("User"))
            {
                AppUser appUser = await _userManager.FindByNameAsync(_httpContext.HttpContext.User.Identity.Name);
              
                Basket? basket = await _context.Baskets.Include(x => x.basketItems.Where(y => !y.IsDeleted))
                          .ThenInclude(x => x.Product)
                           .ThenInclude(x => x.ProductImages)
                           .Include(x => x.basketItems)
                           .ThenInclude(x => x.Product)
                           .ThenInclude(x => x.Discount)
                             .Where(x => !x.IsDeleted && x.AppUserId == appUser.Id).FirstOrDefaultAsync();


                if (basket != null)
                {
                    List<BasketItemViewModel> basketItemViewModels= new();    
                    foreach(var item in basket.basketItems)
                    {
                        basketItemViewModels.Add(new BasketItemViewModel
                        {
                            ProductId=item.ProductId,
                            Image = item.Product.ProductImages.FirstOrDefault(x => x.isMain).Image,
                            Count = item.ProductCount,
                            Name = item.Product.Name,
                            Price = item.Product.Discount == null ? item.Product.Price :
                                (item.Product.Price - (item.Product.Price * ((decimal)item.Product.Discount.Percent / 100)))
                        });

                    }
                    return basketItemViewModels;
                }
            }
            else
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
                                Price = product.Discount == null ? product.Price :
                                (product.Price - (product.Price * ((decimal)product.Discount.Percent / 100)))
                            });

                        }
                    }
                    return basketItemViewModels;
                }
            }
            return new List<BasketItemViewModel>();
        }
        public async Task Remove(int id)
        {
            if (_httpContext.HttpContext.User.Identity.IsAuthenticated && _httpContext.HttpContext.User.IsInRole("User"))
            {
                AppUser user = await _userManager.FindByNameAsync(_httpContext.HttpContext.User.Identity.Name);
                Basket? basket = await _context.Baskets.Include(x => x.basketItems.Where(y => !y.IsDeleted))
                  .Where(x => !x.IsDeleted && x.AppUser.Id == user.Id).
                  FirstOrDefaultAsync();

                if (basket != null)
                {
                    BasketItem basketItem = basket.basketItems.FirstOrDefault(x => x.ProductId == id);
                    //basketitem null gelir
                    if (basketItem != null)
                    {
                        basketItem.IsDeleted = true;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            else
            {
                var basketJson = _httpContext?.HttpContext?
                          .Request.Cookies["basket"];
                if (basketJson != null)
                {
                    List<BasketViewModel>? basketViewModels = JsonConvert
                             .DeserializeObject<List<BasketViewModel>>(basketJson);

                    BasketViewModel basketViewModel = basketViewModels.FirstOrDefault(x => x.ProductId == id);
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
}
