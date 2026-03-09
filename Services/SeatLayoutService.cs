using API_Project.Data;
using API_Project.Helpers;
using API_Project.Models.DTOs;
using API_Project.Models.Entities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace API_Project.Services
{
    public class SeatLayoutService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _dbContext;
        private readonly IDatabase _redis;

        public SeatLayoutService(IWebHostEnvironment env, ApplicationDbContext dbContext, IConnectionMultiplexer redis)
        {
            _env = env;
            _dbContext = dbContext;
            _redis = redis.GetDatabase();
        }
        public async Task<object> GetFullSeatLayoutWithPricesAsync(int idSuatChieu)
        {
            if (_env.WebRootPath == null)
                throw new InvalidOperationException("WebRootPath is not set.");

            var suatChieu = await _dbContext.Showtimes
                .FirstOrDefaultAsync(s => s.MaSuatChieu == idSuatChieu);

            if (suatChieu == null)
                throw new Exception($"Không tìm thấy suất chiếu với id = {idSuatChieu}");

            int maRap = suatChieu.MaRap;
            int maPhong = suatChieu.PhongChieu;
            int typeSuatChieu = suatChieu.TypeSuatChieu;

            var phong = await _dbContext.Rooms
                .FirstOrDefaultAsync(p => p.IDRoom == maPhong);

            if (phong == null)
                throw new Exception($"Không tìm thấy phòng chiếu với id = {maPhong}");

            if (string.IsNullOrEmpty(phong.id_layout))
                throw new Exception($"Phòng chiếu id = {maPhong} chưa có layout.");

            string layoutPath = Path.Combine(_env.WebRootPath, "data", "seat-layout", $"{phong.id_layout}.json");

            if (!File.Exists(layoutPath))
                throw new FileNotFoundException($"Layout file not found: {layoutPath}");

            string layoutJson = await File.ReadAllTextAsync(layoutPath);
            var layoutData = JsonSerializer.Deserialize<SeatLayoutWrapper>(layoutJson);

            var confirmedSeats = await _dbContext.BookedSeats
                .Where(s => s.ShowtimeId == idSuatChieu && s.Status == "Đã thanh toán")
                .Select(s => s.SeatCode)
                .ToListAsync();

            string redisKey = $"suatchieu:{idSuatChieu}:held_seats";
            var heldSeats = await _redis.SetMembersAsync(redisKey);
            var heldList = heldSeats.Select(v => v.ToString()).ToList();

            var loaiSuat = await _dbContext.ShowtimeTypes
                .FirstOrDefaultAsync(x => x.Id == typeSuatChieu);

            if (loaiSuat == null)
                throw new Exception($"Không tìm thấy loại suất chiếu với type = {typeSuatChieu}");

            long giaSuatChieu = loaiSuat.Price;

            var seatPrices = await _dbContext.SeatTypesInRooms
                .Where(x => x.RoomId == maPhong && x.CinemaId == maRap)
                .ToListAsync();

            var priceByRow = seatPrices.ToDictionary(
                x => x.RowCode,
                x => new
                {
                    SeatType = x.SeatType,
                    Price = x.Price + giaSuatChieu
                }
            );

            return new
            {
                layout = layoutData.layout,
                confirmed = confirmedSeats,
                held = heldList,
                prices = priceByRow
            };
        }

        public async Task<bool> SaveCustomSeatLayoutAsync(CustomSeatLayout request, string rootPath)
        {
            var room = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.IDRoom == request.IdRoom);
            if (room == null)
                throw new Exception($"Không tìm thấy phòng chiếu với ID = {request.IdRoom}");

            var cinema = await _dbContext.Cinemas.FirstOrDefaultAsync(c => c.IDCinema == room.CinemaID);
            if (cinema == null)
                throw new Exception($"Không tìm thấy rạp chiếu với ID = {room.CinemaID}");

            string folder = Path.Combine(rootPath, "data", "seat-layout");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string rawFileName = $"{cinema.CinemaName}_{room.RoomName}";
            string safeFileName = FileName.ToSafeFileName(rawFileName);
            string fileName = $"{safeFileName}.json";
            string filePath = Path.Combine(folder, fileName);

            var layoutWrapper = new { layout = request.Layout };
            string json = JsonSerializer.Serialize(layoutWrapper, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            room.id_layout = Path.GetFileNameWithoutExtension(fileName);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetSeatTypesAsync(SetSeatTypes request)
        {
            // Xóa cấu hình cũ
            var existing = await _dbContext.SeatTypesInRooms
                .Where(x => x.RoomId == request.MaPhong)
                .ToListAsync();

            if (existing.Any())
            {
                _dbContext.SeatTypesInRooms.RemoveRange(existing);
                await _dbContext.SaveChangesAsync();
            }

            // Thêm cấu hình mới
            var newConfigs = request.DanhSachGhe.Select(g => new SeatTypeInRoom
            {
                RoomId = request.MaPhong,
                CinemaId = request.MaRap,
                RowCode = g.MaGhe,
                SeatType = g.LoaiGhe,
                Price = g.Gia
            }).ToList();

            await _dbContext.SeatTypesInRooms.AddRangeAsync(newConfigs);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<object> GetSeatLayoutByRoomAsync(int idRoom)
        {
            if (_env.WebRootPath == null)
                throw new InvalidOperationException("WebRootPath is not set.");

            var phong = await _dbContext.Rooms
                .FirstOrDefaultAsync(p => p.IDRoom == idRoom);

            if (phong == null)
                throw new Exception($"Không tìm thấy phòng chiếu với id = {idRoom}");

            if (string.IsNullOrEmpty(phong.id_layout))
                throw new Exception($"Phòng chiếu id = {idRoom} chưa có layout.");

            string layoutPath = Path.Combine(_env.WebRootPath, "data", "seat-layout", $"{phong.id_layout}.json");

            if (!File.Exists(layoutPath))
                throw new FileNotFoundException($"Layout file not found: {layoutPath}");

            string layoutJson = await File.ReadAllTextAsync(layoutPath);
            var layoutData = JsonSerializer.Deserialize<SeatLayoutWrapper>(layoutJson);

            return new
            {
                roomId = idRoom,
                roomName = phong.RoomName,
                layoutName = phong.id_layout,
                layout = layoutData.layout
            };
        }

        private class SeatLayoutWrapper
        {
            public List<List<string?>> layout { get; set; } = new();
        }
    }
}