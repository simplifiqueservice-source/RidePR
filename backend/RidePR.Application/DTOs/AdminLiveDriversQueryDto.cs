namespace RidePR.Application.DTOs;

public class AdminLiveDriversQueryDto
{
    public bool OnlineOnly { get; set; } = true;

    public int Limit { get; set; } = 100;
}
