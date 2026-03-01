using GourmetApi.Entities;

namespace GourmetApi.Dtos
{
    public class UpdateOrderStatusRequestDto
    {
        public OrderStatus Status { get; set; }
    }
}