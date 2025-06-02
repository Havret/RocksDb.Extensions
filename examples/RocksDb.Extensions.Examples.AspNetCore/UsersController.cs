using Microsoft.AspNetCore.Mvc;

namespace RocksDb.Extensions.Examples.AspNetCore;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly UsersStore _usersStore;

    public UsersController(UsersStore usersStore)
    {
        _usersStore = usersStore;
    }

    [HttpPost]
    public IActionResult Post([FromBody] User user)
    {
        var id = Guid.NewGuid().ToString();
        user.Id = id;
        _usersStore.Put(id, user);
        return Ok();
    }

    [HttpGet]
    public IEnumerable<User> Get()
    {
        return _usersStore.GetAllValues();
    }

    [HttpDelete]
    public IActionResult Delete(string userId)
    {
        _usersStore.Remove(userId);
        return Ok();
    }
}
