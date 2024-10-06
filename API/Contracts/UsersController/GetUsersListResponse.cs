namespace API.Contracts.UsersController
{
    public class GetUsersListResponse
    {
        public List<User> Users { get; set; } = new();

        public int TotalCount { get; set; } = 0;
    }

    public class User
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}
