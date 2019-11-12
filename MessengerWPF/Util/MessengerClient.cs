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

        public UdpClient Client;
        public Person Person;
        public MessengerClient(Person person)
        {
            this.Person = person;
            // FOR RELEASE
            // Client = new UdpClient(LOCALPORT);
            // FOR DEBUG
            Client = new UdpClient(new Random().Next(30000, 50000));
        }

        public bool GetBoolCode(string jsonString)
        {
            SendMessage(jsonString);
            dynamic jsonResponse;
            IPEndPoint remoteIp = null; // адрес входящего подключения
            try
            {
                byte[] data = Client.Receive(ref remoteIp); // получаем данные

                string response = Encoding.Unicode.GetString(data);
                jsonResponse = JsonConvert.DeserializeObject(response);
            }
            catch (Exception ex)
            {
                ErrorAlert(ex.Message);
                return false;
            }
            if (Convert.ToInt32(jsonResponse.Code) == 0)
            {
                Console.WriteLine(jsonResponse.Content);
                ErrorAlert((string)jsonResponse.Content);
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool Authorize()
        {
            DefaultJSON jSON = new DefaultJSON{ Code = (int) Codes.Authorization, Content = JsonConvert.SerializeObject(Person) };
            string jsonString = JsonConvert.SerializeObject(jSON);

            return GetBoolCode(jsonString);
        }

        public bool Register()
        {
            DefaultJSON jSON = new DefaultJSON { Code = (int) Codes.Registraion, Content = JsonConvert.SerializeObject(Person) };
            string jsonString = JsonConvert.SerializeObject(jSON);

            return GetBoolCode(jsonString);
        }
        /// <summary>
        /// Отправляет сообщение на сервер
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        public void SendMessage(string message)
        {
            try
            {
                byte[] data = Encoding.Unicode.GetBytes(message);
                Client.Send(data, data.Length, SERVERADDRESS, SERVERPORT); // отправка
            }
            catch (Exception ex)
            {
                ErrorAlert(ex.Message);
            }
        }

        /// <summary>
        /// Прослушивание сообщений в бесконечном цикле
        /// </summary>
        public void ReceiveMessage()
        {
            IPEndPoint remoteIp = null; // адрес входящего подключения
            try
            {
                while (true)
                {
                    byte[] data = Client.Receive(ref remoteIp); // получаем данные
                    string response = Encoding.Unicode.GetString(data);
                }
            }
            catch (Exception ex)
            {
                ErrorAlert(ex.Message);
            }
        }

        public class DefaultJSON
        {
            public int Code { get; set; }
            public string Content { get; set; }
        }

        public enum Codes : int
        {
            False = 0,
            True = 1,
            Confirmation = 2,
            Registraion = 3,
            Authorization = 4,
            NewMessage = 5,
            NewConversation = 6,
            NewMember = 7,
            RemoveMember = 8,
            LeaveMember = 9,
        }

        private void ErrorAlert(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
