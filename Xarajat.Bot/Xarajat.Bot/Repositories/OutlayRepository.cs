using Microsoft.EntityFrameworkCore;
using Xarajat.Data.Context;
using Xarajat.Data.Entities;

namespace Xarajat.Bot.Repositories
{
    public class OutlayRepository
    {
        private readonly XarajatDbContext _context;

        public OutlayRepository(XarajatDbContext context)
        {
            _context = context;
        }

        public async Task AddOutlay(Outlay outlay)
        {
            await _context.Outlays.AddAsync(outlay);
            await _context.SaveChangesAsync();
        }

        public async Task<string?> OutlaysByRoomId(int roomId)
        {
            var sum = 0;
            var outlaysText = "";
            var roomList = _context.Rooms
                .Include(r => r.Users).ToList();
            var room = roomList.First(r=>r.Id == roomId);
            var outlays = _context.Outlays.Where(x => x.RoomId == roomId).ToList();

            foreach (var outlay in outlays)
            {
                outlaysText += outlay.Description + "\t" + outlay.Cost + "\n";
                sum += outlay.Cost;
            }

            var usersCount = room.Users!.Count;
            var outlayPerUser = (double)sum / (double)usersCount;

            outlaysText += "\nTotal cost: " + sum + "\n";
            outlaysText += "Users: " + usersCount + "\n";
            outlaysText += $"Per User: {outlayPerUser}";

            return outlaysText;
        }
    }
}
