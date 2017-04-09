
namespace EIV.Demo.WebService.Controllers
{
    using Data.Interface;
    using Data.Repository;
    using EIV.Demo.Model;

    using System;

    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.OData;

    [EnableQuery]
    public class CountryController : ODataController
    {
        private static ICountryRepository countryRepository = null;

        public CountryController()
        {
            if (countryRepository == null)
            {
                countryRepository = new CountryRepository();
            }

        }

        [HttpGet]
        public IHttpActionResult Get()    // IList<Country>
        {
            var rst = countryRepository.GetAll();

            var response = Request.CreateResponse(HttpStatusCode.OK, rst);
            response.Headers.Add("Company-Name", "EIV.Software");

            return ResponseMessage(response);

            //return countryRepository.GetAll();
        }

        [HttpGet]
        public Country Get([FromODataUri]int key)
        {
            Country country = countryRepository.GetById(key);
            if (country == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);
            else
                return country;
        }

        [HttpPost]
        public IHttpActionResult Post([FromBody] Country country)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (country == null)
            {
                return BadRequest();
            }
            countryRepository.Add(country);

            var response = Request.CreateResponse(HttpStatusCode.Created, country);

            response.Headers.Location = Request.RequestUri;
            return ResponseMessage(response);
        }

        [HttpPut]
        public IHttpActionResult Put(int key, Country country)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            try
            {
                country.Id = key;
                countryRepository.Update(country);

                var response = Request.CreateResponse(HttpStatusCode.OK, country);

                return ResponseMessage(response);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        [HttpDelete]
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Country country = countryRepository.GetById(key);
            if (country == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            countryRepository.Delete(country);

            //return Ok();
            var response = Request.CreateResponse(HttpStatusCode.NoContent);
            return ResponseMessage(response);
        }
    }
}