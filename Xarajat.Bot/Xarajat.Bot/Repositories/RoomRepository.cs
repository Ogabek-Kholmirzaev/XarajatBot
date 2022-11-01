using Microsoft.EntityFrameworkCore;
using Xarajat.Data.Context;
using Xarajat.Data.Entities;

namespace Xarajat.Bot.Repositories;

public class RoomRepository
{
    private readonly XarajatDbContext _context;

    public RoomRepository(XarajatDbContext context)
    {
        _context = context;
    }

    public Task<Room?> GetRoomById(int id)
    {
        var rooms = _context.Rooms
            .Include(room => room.Users)
            .ToList();

        return Task.FromResult(rooms.FirstOrDefault(r=>r.Id == id));
    }

    public async Task AddRoomAsync(Room room)
    {
        await _context.Rooms.AddAsync(room);
        await _context.SaveChangesAsync();
    }

    public async Task<Room?> GetRoomByKey(string key)
    {
        return await _context.Rooms.FirstOrDefaultAsync(r=>r.Key == key);
    }

    public async Task UpdateRoom(Room room)
    {
        _context.Update(room);
        await _context.SaveChangesAsync();
    }
}