﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CinemaBookingClient.Services;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CinemaBookingClient.Controllers
{    
    public class CinemaHallController : Controller
    {
        private ICinemaSeatPlanWS seatPlan;

        public CinemaHallController(ICinemaSeatPlanWS seatPlan)
        {
            this.seatPlan = seatPlan;
        }
        
        [HttpGet]
        public IActionResult CinemaHallPlan()
        {
            var result = seatPlan.GetCinemaSeatPlanAsync().Result;
            return View();
        }

        //// GET api/<controller>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/<controller>
        //[HttpPost]
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/<controller>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/<controller>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}