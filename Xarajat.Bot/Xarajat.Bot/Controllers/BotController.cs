using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xarajat.Bot.Repositories;
using Xarajat.Bot.Services;
using Xarajat.Data.Entities;
using User = Xarajat.Data.Entities.User;

namespace Xarajat.Bot.Controllers;

[ApiController]
[Route("bot")]
public class BotController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly TelegramBotService _botService;
    private readonly RoomRepository _roomRepository;

    public BotController(UserRepository userRepository, TelegramBotService botService, RoomRepository roomRepository)
    {
        _userRepository = userRepository;
        _botService = botService;
        _roomRepository = roomRepository;
    }

    [HttpGet]
    public IActionResult GetBot() => Ok("HttpGet GetBot ...");

    [HttpPost]
    public async Task PostUpdate(Update update)
    {
        if (update.Type != UpdateType.Message)
            return;

        var (chatId, message, username) = GetValues(update);
        var user = await FilterUser(chatId, username);

        if (user.Step == 0)
        {
            if (message == "Create room")
            {
                user.Step = 1;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, "Enter room name?");
            }
            else if (message == "Join room")
            {
                user.Step = 2;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, "Enter room key?");
            }
            else
            {
                var menu = new List<string> { "Create room", "Join room" };

                _botService.SendMessage(user.ChatId, "Menu", reply: _botService.GetKeyboard(menu));
            }
        }
        else if (user.Step == 1)
        {
            var room = new Room
            {
                Name = message,
                Key = Guid.NewGuid().ToString("N")[..10],
                Status = RoomStatus.Created
            };

            await _roomRepository.AddRoomAsync(room);

            user.RoomId = room.Id;
            user.IsAdmin = true;
            user.Step = 3;

            await _userRepository.UpdateUser(user);

            var menu = new List<string> { "Add outlay", "Calculate" };

            _botService.SendMessage(user.ChatId, "Menu", reply: _botService.GetKeyboard(menu));
        }
        else if (user.Step == 3)
        {
            if (message == "Add outlay")
            {
                user.Step = 1;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, "Enter outlay details?");
            }
            else if (message == "Calculate")
            {
                user.Step = 2;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, "room statistics?");
            }
            else
            {
                var menu = new List<string> { "Add outlay", "Calculate" };

                _botService.SendMessage(user.ChatId, "Menu", reply: _botService.GetKeyboard(menu));
            }
        }
    }

    private Tuple<long, string, string> GetValues(Update update)
    {
        var chatId = update.Message!.From!.Id;
        var message = update.Message!.Text!;
        var name = update.Message.From.Username ?? update.Message.From.FirstName;

        return new(chatId, message, name);
    }

    public async Task<User> FilterUser(long chatId, string username)
    {
        var user = await _userRepository.GetUserByChatId(chatId);
        if (user is null)
        {
            user = new User
            {
                ChatId = chatId,
                Name = username,
            };

            await _userRepository.AddUserAsync(user);
        }
        return user;
    }
}