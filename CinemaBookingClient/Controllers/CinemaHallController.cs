﻿using CinemaBookingClient.HardCode;
using CinemaBookingClient.Models;
using CinemaBookingClient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
 
namespace CinemaBookingClient.Controllers
{
    [Authorize]
    public class CinemaHallController : Controller
    {
        private ICinemaSeatPlanWS seatPlan;
        private ICinemaDataService dataSevice;
        private readonly UserManager<ApplicationUser> userManager;   

        public CinemaHallController(ICinemaSeatPlanWS seatPlan, ICinemaDataService dataSevice, UserManager<ApplicationUser> userManager)
        {
            this.seatPlan = seatPlan;
            this.dataSevice = dataSevice;
            this.userManager = userManager;
        }

        [HttpGet]
        public IActionResult UserOrders()
        {
            var user = userManager.GetUserAsync(User).Result;
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }
            string userId = user.Id;
            ViewData["User"] = user.UserName;
            var orders = dataSevice.GetOrders(userId);
            return View("UserOrders", orders);
        }

        [HttpGet]
        public IActionResult CinemaHallPlan()
        {
            var user = userManager.GetUserAsync(User).Result;
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }
            string userId = user.Id;
            var cinemaHallVM = CinemaHallCreator.GetCinemaHallViewModel(seatPlan, dataSevice, userId, HardCodeValues.seanceId);
            return View("CinemaHallPlan", cinemaHallVM);
        }

        [HttpPost]
        public JsonResult Post([FromBody]Seats seats )
        {
            string url = Url.Action("CinemaHallPlan");
            if (!ModelState.IsValid)
                return Json(new { success = false, url });

            var user = userManager.GetUserAsync(User).Result;
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }
            string userId = user.Id;
            List<Position> addedSeats = seats.AddedPositions;
            List<Position> removedSeats = seats.RemovedPositions; 
            var order = dataSevice.RecompileOrders(userId, HardCodeValues.cinemaHallId, HardCodeValues.seanceId, addedSeats, removedSeats);

            return Json(new { success = true,  url });

        }       
         
        public class Seats
        {
            public List<Position> AddedPositions { get; set; }
            public List<Position> RemovedPositions { get; set; }
        }
    }


}
