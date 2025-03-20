using BusinessLayer.Interface;
using Microsoft.AspNetCore.Mvc;
using ModelLayer.DTOs;
using RepositoryLayer.Entity;

namespace UserAddressBokk.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AddressBookController : ControllerBase
    {
        private readonly IAddressBookService _addressBookService;

        public AddressBookController(IAddressBookService addressBookService)
        {
            _addressBookService = addressBookService;
        }

        // GET /api/addressbook
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AddressBookDTO>>> GetAllContacts()
        {
            var addresses = await _addressBookService.GetAllContactsAsync();
            return Ok(addresses);
        }

        // GET /api/addressbook/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AddressBookDTO>> GetContactById(int id)
        {
            var address = await _addressBookService.GetContactByIdAsync(id);
            if (address == null) return NotFound();
            return Ok(address);
        }

        // POST /api/addressbook
        [HttpPost]
        public async Task<ActionResult<AddressBookDTO>> AddContact(AddressBookDTO addressBookDto)
        {
            var newAddress = await _addressBookService.AddContactAsync(addressBookDto);
            return CreatedAtAction(nameof(GetContactById), new { id = newAddress.Id }, newAddress);
        }

        // PUT /api/addressbook/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateContact(int id, AddressBookDTO addressBookDto)
        {
            if (id != addressBookDto.Id) return BadRequest();
            var result = await _addressBookService.UpdateContactAsync(id, addressBookDto);
            if (!result) return NotFound();
            return NoContent();
        }

        // DELETE /api/addressbook/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteContact(int id)
        {
            var result = await _addressBookService.DeleteContactAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}
