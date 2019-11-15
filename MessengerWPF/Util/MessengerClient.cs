﻿using ContextLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MessengerWPF
{
    public class MessengerClient
    {
        public static readonly int SERVERPORT = 8005;
        public static readonly string SERVERADDRESS = "127.0.0.1";
        public static readonly int LOCALPORT = 1800;

        private static MessengerClient _instant;

        public UdpClient Client;
        public Person Person;

        private MessengerClient() { }

        public static MessengerClient GetInstant(Person person)
        {
            if(_instant == null)
            {
                _instant = new MessengerClient();
            }
            
            _instant.Person = person;
            // FOR RELEASE
            // Client = new UdpClient(LOCALPORT);
            // FOR DEBUG
            _instant.Client = new UdpClient(new Random().Next(30000, 50000));

            return _instant;
        }

        public static MessengerClient GetInstant()
        {
            return _instant;
        }
        
        public static async void DisposeInstant()
        {
            await _instant.LogOut();
            _instant = null;
        }

        public async void SetStatus(Status status)
        {
            if (status.Name == "Невидимка")
            {
                status.Name = "Не в сети";
            }
            DefaultJSON jSON = new DefaultJSON { Code = (int)Codes.SetStatus, Content = JsonConvert.SerializeObject(new { Person = Person, Status = status }) };
            string jsonString = JsonConvert.SerializeObject(jSON);

            try
            {
                SendMessage(jsonString);
            }
            catch (Exception ex)
            {
                ErrorAlert(ex.Message);
            }
        }

        /// <summary>
        /// Получение списка пользователей
        /// </summary>
        public async Task<List<Person>> GetPeople()
        {
            List<Person> people = new List<Person>();

            DefaultJSON jSON = new DefaultJSON { Code = (int)Codes.GetUsers, Content = "" };
            string jsonString = JsonConvert.SerializeObject(jSON);

            try
            {
                DefaultJSON response = await GetResponse(jsonString);
                people = JsonConvert.DeserializeObject<List<Person>>(response.Content);
            }catch(Exception ex)
            {
                ErrorAlert(ex.Message);
            }

            people.Remove(people.Where(o => o.ID == Person.ID).FirstOrDefault());

            return people;
        }

        /// <summary>
        /// Получение списка сообщений диалога
        /// </summary>
        /// <param name="conversation">Диалог</param>
        /// <returns>Список сообщений</returns>
        public async Task<List<Message>> GetMessages(Conversation conversation)
        {
            List<Message> messages = new List<Message>();

            DefaultJSON jSON = new DefaultJSON { Code = (int)Codes.GetMessages, Content = JsonConvert.SerializeObject(new { Person = Person, Conversation = conversation }) };
            string jsonString = JsonConvert.SerializeObject(jSON);

            try
            {
                DefaultJSON response = await GetResponse(jsonString);
                messages = JsonConvert.DeserializeObject<List<Message>>(response.Content);
            }catch(Exception ex)
            {
                ErrorAlert(ex.Message);
            }

            return messages;
        }

        /// <summary>
        /// Получение списка диалогов
        /// </summary>
        /// <returns>Список диалогов</returns>
        public async Task<List<Conversation>> GetConversations()
        {
            var conversations = new List<Conversation>();

            DefaultJSON jSON = new DefaultJSON { Code = (int)Codes.GetConversations, Content = JsonConvert.SerializeObject(Person) };
            string jsonString = JsonConvert.SerializeObject(jSON);

            DefaultJSON response = await GetResponse(jsonString);
            conversations = JsonConvert.DeserializeObject<List<Conversation>>(response.Content);

            return conversations;
        }

        /// <summary>
        /// Получение ответа с сервера
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        private async Task<DefaultJSON> GetResponse(string jsonString)
        {
            SendMessage(jsonString);
            dynamic jsonResponse;
            try
            {
                UdpReceiveResult result = await Client.ReceiveAsync();
                byte[] data = result.Buffer;

                string response = Encoding.Unicode.GetString(data);
                jsonResponse = JsonConvert.DeserializeObject(response);
            }
            catch (Exception ex)
            {
                ErrorAlert(ex.Message);
                return new DefaultJSON { Code = (int) Codes.False, Content = ex.Message };
            }
            if ((int)(jsonResponse.Code) == (int) Codes.False)
            {
                ErrorAlert((string)jsonResponse.Content);
            }
            return new DefaultJSON { Code = (int)jsonResponse.Code, Content = (string)jsonResponse.Content };
        }

        /// <summary>
        /// Авторизация на сервере
        /// </summary>
        public async Task<bool> Authorize()
        {
            DefaultJSON jSON = new DefaultJSON { Code = (int) Codes.Authorization, Content = JsonConvert.SerializeObject(Person) };
            string jsonString = JsonConvert.SerializeObject(jSON);

            DefaultJSON response = await GetResponse(jsonString);
            if(response.Code == (int)Codes.True)
            {
                dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);
                Person = new Person {
                    ID = jsonResponse.ID,
                    Login = jsonResponse.Login,
                    Password = jsonResponse.Password,
                    Name = jsonResponse.Name,
                    SurName = jsonResponse.SurName
                };
                // TODO: Photo
            }

            return response.Code == (int)Codes.True;
        }

        /// <summary>
        /// Выход с сервера
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LogOut()
        {
            DefaultJSON jSON = new DefaultJSON { Code = (int)Codes.LogOut, Content = JsonConvert.SerializeObject(Person) };
            string jsonString = JsonConvert.SerializeObject(jSON);

            DefaultJSON response = await GetResponse(jsonString);
            
            return response.Code == (int)Codes.True;
        }

        /// <summary>
        /// Регистрация на сервере
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Register()
        {
            DefaultJSON jSON = new DefaultJSON { Code = (int) Codes.Registraion, Content = JsonConvert.SerializeObject(Person) };
            string jsonString = JsonConvert.SerializeObject(jSON);

            DefaultJSON response = await GetResponse(jsonString);
            return response.Code == (int)Codes.True;
        }

        /// <summary>
        /// Отправляет сообщение на сервер
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        private async void SendMessage(string message)
        {
            try
            {
                byte[] data = Encoding.Unicode.GetBytes(message);
                await Client.SendAsync(data, data.Length, SERVERADDRESS, SERVERPORT); // отправка
            }
            catch (Exception ex)
            {
                ErrorAlert(ex.Message);
            }
        }

        /// <summary>
        /// Прослушивание сообщений с сервера в бесконечном цикле
        /// </summary>
        private async void ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    UdpReceiveResult result = await Client.ReceiveAsync();
                    byte[] data = result.Buffer; // получаем данные
                    string response = Encoding.Unicode.GetString(data);
                }
            }
            catch (Exception ex)
            {
                ErrorAlert(ex.Message);
            }
        }

        /// <summary>
        /// Уведомление об ошибке
        /// </summary>
        private void ErrorAlert(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
