using Microsoft.AspNetCore.Mvc;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDR.PatientBookingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly PatientBookingContext _context;

        public BookingController(PatientBookingContext context)
        {
            _context = context;
        }

        [HttpGet("patient/{identificationNumber}/next")]
        public IActionResult GetPatientNextAppointnemtn(long identificationNumber)
        {
            var bockings = _context.Order.OrderBy(x => x.StartTime).ToList();

            // changes to the data model are needed to cater for status (like for example cancelled) as you dont want to straight up delete data in my opinion
            // There was not time for this in the av
            if (bockings.Where(x => x.Patient.Id == identificationNumber ).Count() == 0)
            {
                return StatusCode(502);
            }
            else
            {
                var bookings2 = bockings.Where(x => x.PatientId == identificationNumber);
                if (bookings2.Where(x => x.StartTime > DateTime.Now).Count() == 0)
                {
                    return StatusCode(502);
                }
                else
                {
                    var bookings3 = bookings2.Where(x => x.StartTime > DateTime.Now);
                    return Ok(new
                    {
                        bookings3.First().Id,
                        bookings3.First().DoctorId,
                        bookings3.First().StartTime,
                        bookings3.First().EndTime
                    });
                }
            }
        }



        [HttpPost()]
        public IActionResult AddBooking(NewBooking newBooking)
        {

            
            var bookingId = new Guid();
            var bookingStartTime = newBooking.StartTime;

            // Check that start time is not in the past (so already missed at booked time)
            //This should really be checked in the front end as well
            if (bookingStartTime < DateTime.Now)
                return StatusCode(400);

            var bookingEndTime = newBooking.EndTime;
            var bookingPatientId = newBooking.PatientId;
            var bookingPatient = _context.Patient.FirstOrDefault(x => x.Id == newBooking.PatientId);
            var bookingDoctorId = newBooking.DoctorId;
            var bookingDoctor = _context.Doctor.FirstOrDefault(x => x.Id == newBooking.DoctorId);
            var bookingSurgeryType = _context.Patient.FirstOrDefault(x => x.Id == bookingPatientId).Clinic.SurgeryType;

           
            if (!CheckThatBookingWindowIsAvailable(bookingStartTime, bookingEndTime) )
            { return StatusCode(400); }

            var myBooking = new Order
            {
                Id = bookingId,
                StartTime = bookingStartTime,
                EndTime = bookingEndTime,
                PatientId = bookingPatientId,
                DoctorId = bookingDoctorId,
                Patient = bookingPatient,
                Doctor = bookingDoctor,
                SurgeryType = (int)bookingSurgeryType
            };

            _context.Order.AddRange(new List<Order> { myBooking });
            _context.SaveChanges();

            return StatusCode(200);
        }

        public class NewBooking
        {
            public Guid Id { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public long PatientId { get; set; }
            public long DoctorId { get; set; }
        }

        private static MyOrderResult UpdateLatestBooking(List<Order> bookings2, int i)
        {
            MyOrderResult latestBooking;
            latestBooking = new MyOrderResult();
            latestBooking.Id = bookings2[i].Id;
            latestBooking.DoctorId = bookings2[i].DoctorId;
            latestBooking.StartTime = bookings2[i].StartTime;
            latestBooking.EndTime = bookings2[i].EndTime;
            latestBooking.PatientId = bookings2[i].PatientId;
            latestBooking.SurgeryType = (int)bookings2[i].GetSurgeryType();

            return latestBooking;
        }

        private bool CheckThatBookingWindowIsAvailable(DateTime bookingStartTime, DateTime bookingEndTime)
        {
            //check for existing appointment starting in bookingwindow (could also be checked on front end with jqeury)
            int startWindow = _context.Order.Where(appointment => appointment.StartTime > bookingStartTime && appointment.StartTime < bookingEndTime).Count();
            //check for existing appointment ending in bookingwindow (could also be checked on front end with jqeury)
            int endWindow = _context.Order.Where(appointment => appointment.EndTime > bookingStartTime && appointment.EndTime < bookingEndTime).Count();

            if (endWindow > 0 || startWindow > 0)
                return false;
            else return true;
        }

        private class MyOrderResult
        {
            public Guid Id { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public long PatientId { get; set; }
            public long DoctorId { get; set; }
            public int SurgeryType { get; set; }
        }
    }
}