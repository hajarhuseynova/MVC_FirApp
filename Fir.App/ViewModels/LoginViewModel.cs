﻿using System.ComponentModel.DataAnnotations;

namespace Fir.App.ViewModels
{
    public class LoginViewModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool isRememberMe { get; set; }
    }
}
