namespace GourmetApi.Entities
{
    public enum OrderStatus
    {
        New = 0,
        Preparing = 1,
        Ready = 2,
        Delivered = 3,
        Canceled = 4,
        Finished = 5,
    }
}