using BusinessLayer.Interface;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.Entity;

namespace UserAddressBokk.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AddressBookController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressBookController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Address>>> GetAllContacts()
        {
            var addresses = await _addressService.GetAllAddressesAsync();
            return Ok(addresses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Address>> GetContactById(int id)
        {
            var address = await _addressService.GetAddressByIdAsync(id);
            if (address == null) return NotFound();
            return Ok(address);
        }

        [HttpPost]
        public async Task<ActionResult<Address>> AddContact(Address address)
        {
            var newAddress = await _addressService.AddAddressAsync(address);
            return CreatedAtAction(nameof(GetContactById), new { id = newAddress.Id }, newAddress);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateContact(int id, Address address)
        {
            if (id != address.Id) return BadRequest();
            await _addressService.UpdateAddressAsync(address);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteContact(int id)
        {
            var result = await _addressService.DeleteAddressAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}
