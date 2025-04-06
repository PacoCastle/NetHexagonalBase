using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class KafkaController : ControllerBase
{
    private readonly KafkaService _kafkaService;

    public KafkaController(KafkaService kafkaService)
    {
        _kafkaService = kafkaService;
    }

    [HttpPost("message")]
    public async Task<IActionResult> PostMessage([FromBody] string message)
    {
        await _kafkaService.ProduceAsync("test", message);
        return Ok(new { Status = "Message sent to Kafka topic 'test'" });
    }
}
