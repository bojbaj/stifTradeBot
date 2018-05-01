using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace tradeBot.Controllers
{
    public class PublicController : Controller
    {
        public IActionResult Home()
        {
            return Json(new { status = true });
        }
    }
}