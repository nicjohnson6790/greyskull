using API.Contracts.UsersController;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : Controller
    {
        [HttpPost("List")]
        [ProducesResponseType(typeof(GetUsersListResponse), 200)]
        public IActionResult GetUserList(
            GetUsersListRequest request)
        {
            return Ok(new GetUsersListResponse());
        }
    }
}
