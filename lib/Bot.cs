using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using tradeBot.Models;

namespace tradeBot.lib
{
    public interface IBot
    {
        BotSettingModel LoadSetting();
        Task<bool> Execute();
    }
    public class Bot : IBot
    {
        public readonly IConfigurationRoot _configuration;
        public readonly IHostingEnvironment _env;
        public readonly IRestApi _restApi;
        private readonly string configFilePath;

        public Bot(IConfigurationRoot configuration, IHostingEnvironment env)
        {
            _configuration = configuration;
            _env = env;
            _restApi = new RestApi(configuration, env, this);
            //configFilePath = string.Format("{0}\\botSetting.json", _env.WebRootPath);
            configFilePath = Path.Combine(_env.WebRootPath, "botSetting.json");
        }

        public async Task<bool> Execute()
        {
            string botToken = _configuration.GetValue<string>("Bot:Token");
            Telegram.Bot.TelegramBotClient botClient = new Telegram.Bot.TelegramBotClient(botToken);
            BotSettingModel setting = LoadSetting();
            Telegram.Bot.Types.Update[] updates = await botClient.GetUpdatesAsync(setting.LastChatID + 1);

            string RegisterKey = _configuration.GetValue<string>("Bot:RegisterKey");
            string DeleteAccountKey = _configuration.GetValue<string>("Bot:DeleteAccountKey");
            string GetResponseKey = _configuration.GetValue<string>("Bot:GetResponseKey");

            foreach (Telegram.Bot.Types.Update update in updates)
            {
                string chatText = string.Empty;
                chatText = update?.Message?.Text ?? string.Empty;
                long chatId = update.Message.Chat.Id;

                if (chatText == RegisterKey)
                {
                    if (!setting.Receivers.Any(x => x == chatId.ToString()))
                    {
                        setting.Receivers.Add(chatId.ToString());
                        await botClient.SendTextMessageAsync(chatId, "به دریافت کنندگان اضافه شدی", replyMarkup: getKeyboard());
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "قبلا اضافه شدی", replyMarkup: getKeyboard());
                    }
                }
                else if (chatText == DeleteAccountKey)
                {
                    if (setting.Receivers.Any(x => x == chatId.ToString()))
                    {
                        setting.Receivers.Remove(chatId.ToString());
                        await botClient.SendTextMessageAsync(chatId, "از دریافت کنندگان حذف شدی", replyMarkup: getKeyboard());
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "توی لیست نیستی", replyMarkup: getKeyboard());
                    }
                }
                else if (chatText == "/start")
                {
                    await botClient.SendTextMessageAsync(chatId, "سلام، چیکار کنم برات ؟!", replyMarkup: getKeyboard());
                }
                else if (chatText == GetResponseKey)
                {
                    await _restApi.GetResponse(chatId);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "نمیفهمم چی میگی", replyMarkup: getKeyboard());
                }
                setting.LastChatID = update.Id;
            }
            SaveSetting(setting);
            return true;
        }
        private Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup getKeyboard()
        {
            string RegisterKey = _configuration.GetValue<string>("Bot:RegisterKey");
            string DeleteAccountKey = _configuration.GetValue<string>("Bot:DeleteAccountKey");
            string GetResponseKey = _configuration.GetValue<string>("Bot:GetResponseKey");

            return new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup()
            {
                Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                {
                    new Telegram.Bot.Types.KeyboardButton[]
                    {
                        new Telegram.Bot.Types.KeyboardButton(GetResponseKey)
                    },
                    new Telegram.Bot.Types.KeyboardButton[]
                    {
                        new Telegram.Bot.Types.KeyboardButton(RegisterKey)
                    },
                    new Telegram.Bot.Types.KeyboardButton[]
                    {
                        new Telegram.Bot.Types.KeyboardButton(DeleteAccountKey)
                    }

                }
            };
        }
        public BotSettingModel LoadSetting()
        {            
            if (!File.Exists(configFilePath))
            {
                SaveSetting(new BotSettingModel()
                {
                    LastChatID = 0,
                    Receivers = new List<string>() { }
                });
            }
            using (FileStream settingFile = File.OpenRead(configFilePath))
            {
                StreamReader logReader = new StreamReader(settingFile);
                string strSetting = logReader.ReadToEnd();
                BotSettingModel setting = new BotSettingModel()
                {
                    LastChatID = 0,
                    Receivers = new List<string>() { }
                };
                try
                {
                    setting = JsonConvert.DeserializeObject<BotSettingModel>(strSetting);
                }
                catch { }
                logReader.Dispose();
                return setting;
            }
        }
        private void SaveSetting(BotSettingModel model)
        {
            using (FileStream settingFile = File.Create(configFilePath))
            {
                StreamWriter logWriter = new StreamWriter(settingFile);
                logWriter.WriteLine(JsonConvert.SerializeObject(model));
                logWriter.Dispose();
            }
        }
    }
}
