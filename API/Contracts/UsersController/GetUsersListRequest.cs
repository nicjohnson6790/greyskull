namespace API.Contracts.UsersController
{
    public class GetUsersListRequest
    {
        public int StartIndex { get; set; } = 0;

        public int EndIndex { get; set; } = 9;
    }
}
