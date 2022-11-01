using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xarajat.Bot.Repositories;
using Xarajat.Bot.Services;
using Xarajat.Data.Entities;
using User = Xarajat.Data.Entities.User;

namespace Xarajat.Bot.Controllers;

[Route("bot")]
[ApiController]
public class BotController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly TelegramBotService _botService;
    private readonly RoomRepository _roomRepository;
    private readonly OutlayRepository _outlayRepository;

    public BotController(UserRepository userRepository, TelegramBotService botService, RoomRepository roomRepository,
        OutlayRepository outlayRepository)
    {
        _userRepository = userRepository;
        _botService = botService;
        _roomRepository = roomRepository;
        _outlayRepository = outlayRepository;
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

        if (message == "start")
        {
            _botService.SendMessage(user.ChatId, "Menu", reply: _botService.GetKeyboard(ShowMenu(user)));
            return;
        }

        if (user.Step == 0)
        {
            user.Step = 100;

            await _userRepository.UpdateUser(user);

            _botService.SendMessage(user.ChatId, "Menu", reply: _botService.GetKeyboard(ShowMenu(user)));
        }
        else if (user.Step == 100)
        {
            if (message == "Create room")
            {
                user.Step = 1;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, "Enter room name.");
            }
            else if (message == "Join room")
            {
                user.Step = 2;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, "Enter key to join room.");
            }
            else if (message == "Add outlay")
            {
                user.Step = 4;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, "Enter outlay details!\n\nBanana 20000");
            }
            else if (message == "Calculate room outlays")
            {
                user.Step = 5;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, "Room outlays.");

                user.Step = 100;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, $"{await _outlayRepository.OutlaysByRoomId(user.RoomId!.Value)}", reply: _botService.GetKeyboard(ShowMenu(user)));
            }
            else if (message == "My room")
            {
                var room = await _roomRepository.GetRoomById(user.RoomId!.Value);

                var msg = "";

                msg += $"Name:\t{room.Name}\n";
                if(user.IsAdmin)
                    msg += $"Key:\t{room.Key}\n";
                msg += $"Users:\n";
                foreach (var roomUser in room.Users!)
                {
                    msg += $"\t{roomUser.FullName}\n";
                }

                user.Step = 100;

                await _userRepository.UpdateUser(user);

                _botService.SendMessage(user.ChatId, msg, reply: _botService.GetKeyboard(ShowMenu(user)));
            }
        }
        else if (user.Step == 1) // Create room
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
            user.Step = 100;

            await _userRepository.UpdateUser(user);

            _botService.SendMessage(user.ChatId, "Opened new room", reply: _botService.GetKeyboard(ShowMenu(user)));
        }
        else if (user.Step == 2) //Join room
        {
            var room = await _roomRepository.GetRoomByKey(message);
            var msg = "";

            if (room == null)
                msg += "Room key is false.";
            else
                msg += $"Successfully added to {room.Name}.";

            user.RoomId = room!.Id;
            user.Step = 100;

            await _userRepository.UpdateUser(user);

            _botService.SendMessage(user.ChatId, msg, reply: _botService.GetKeyboard(ShowMenu(user)));
        }
        else if (user.Step == 4) //Add outlay
        {
            var product = message.Split(' ').ToList();
            var msg = "";

            if (product.Count < 2)
                msg += "Product input error!";
            else
            {
                bool success = int.TryParse(product[1], out int productPrice);
                var productName = product[0];

                if (!success)
                    msg += "Product input error!";
                else
                {
                    var outlay = new Outlay()
                    {
                        Cost = productPrice,
                        Description = productName,
                        RoomId = user.RoomId!.Value,
                        UserId = user.Id
                    };

                    await _outlayRepository.AddOutlay(outlay);

                    msg += "Successfully added!";
                }
            }
            
            user.Step = 100;

            await _userRepository.UpdateUser(user);

            _botService.SendMessage(user.ChatId, msg, reply: _botService.GetKeyboard(ShowMenu(user)));
        }
    }

    private List<string> ShowMenu(User user)
    {
        var menu = new List<string> { "Create room", "Join room" };

        if (user.RoomId != null)
        {
            menu = new List<string>()
            {
                "My room",
                "Add outlay",
                "Calculate room outlays",
                "Create room",
                "Join room"
            };
        }

        return menu;
    }

    private Tuple<long, string, string> GetValues(Update update)
    {
        var chatId = update.Message!.From!.Id;
        var message = update.Message!.Text!;
        var name = update.Message.From.Username ?? update.Message.From.FirstName + update.Message.From.LastName;

        return new Tuple<long, string, string>(chatId, message, name);
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