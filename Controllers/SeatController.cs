using API_Project.Models.DTOs;
using API_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Project.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SeatController : ControllerBase
    {
        private readonly RedisService _redisService;
        private readonly SeatLayoutService _seatLayoutService;
        private readonly IWebHostEnvironment _env;

        public SeatController(RedisService redisService, SeatLayoutService seatLayoutService, IWebHostEnvironment env)
        {
            _redisService = redisService;
            _seatLayoutService = seatLayoutService;
            _env = env;
        }
        public class HoldSeatRequest
        {
            public int ShowtimeId { get; set; }
            public List<string> SeatCodes { get; set; }
        }
        // GET: api/Seat/held-list/101
        [HttpGet("held-list/{showtimeId}")]
        public async Task<IActionResult> GetHeldSeats(int showtimeId)
        {
            var list = await _redisService.GetHeldSeatsAsync(showtimeId);
            return Ok(list);
        }

        // GET: api/Seat/get-layout-price/{idSuatChieu}
        [HttpGet("get-layout-price/{idSuatChieu}")]
        public async Task<IActionResult> GetLayoutWithPrices(int idSuatChieu)
        {
            var result = await _seatLayoutService.GetFullSeatLayoutWithPricesAsync(idSuatChieu);
            return Ok(result);
        }

        // GET: api/Seat/check?showtimeId=101&seatCode=A5
        [HttpGet("check")]
        public async Task<IActionResult> CheckSeatStatus(int showtimeId, string seatCode)
        {
            var isHeld = await _redisService.IsSeatLockedAsync(showtimeId, seatCode);
            return Ok(new { Seat = seatCode, IsHeld = isHeld });
        }
        // GET: api/Seat/room-layout/{idRoom}
        [HttpGet("room-layout/{idRoom}")]
        public async Task<IActionResult> GetRoomLayout(int idRoom)
        {
            try
            {
                var result = await _seatLayoutService.GetSeatLayoutByRoomAsync(idRoom);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        
        [HttpPost("hold-multiple")]
        public async Task<IActionResult> HoldMultipleSeats([FromBody] HoldSeatRequest request)
        {
            var results = new Dictionary<string, bool>();

            foreach (var seat in request.SeatCodes)
            {
                var success = await _redisService.HoldSeatAsync(request.ShowtimeId, seat);
                results[seat] = success;
            }

            return Ok(results);
        }


        [HttpPost("Custom_Seat_Layout")]
        public async Task<IActionResult> CreateCustomSeatLayout([FromBody] CustomSeatLayout request)
        {
            var success = await _seatLayoutService.SaveCustomSeatLayoutAsync(request, _env.WebRootPath);

            if (!success)
                return BadRequest("Dữ liệu không hợp lệ hoặc không thể lưu file.");

            return Ok(new { Message = "Tạo ma trận thành công", FileName = request.FileName });
        }
        [HttpPost("set-seat-types")]
        public async Task<IActionResult> SetSeatTypes([FromBody] SetSeatTypes request)
        {
            if (request.DanhSachGhe == null || !request.DanhSachGhe.Any())
                return BadRequest(new { success = false, message = "Danh sách ghế không được để trống." });

            await _seatLayoutService.SetSeatTypesAsync(request);
            return Ok(new { success = true, message = "Cập nhật loại ghế thành công." });
        }
        // DELETE: api/Seat/release?showtimeId=101&seatCode=A5
        [HttpDelete("release")]
        public async Task<IActionResult> ReleaseSeat(int showtimeId, string seatCode)
        {
            var removed = await _redisService.ReleaseSeatAsync(showtimeId, seatCode);
            return removed ? Ok("Seat released") : NotFound("Seat was not held");
        }
    }
}
