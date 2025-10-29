using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common;
using Newtonsoft.Json;
using static Common.Snakes;

namespace Snake
{
    public class Program
    {
        public static List<Leaders> Leaders = new List<Leaders>();
        public static List<ViewModelUserSettings> remoteIPAdress = new List<ViewModelUserSettings>();
        public static List<ViewModelGames> viewModelGames = new List<ViewModelGames>();
        public static int localPort = 5001;
        public static int MaxSpeed = 15;

        private static void Send()
        {
            foreach (var user in remoteIPAdress)
            {
                UdpClient sender = new UdpClient();

                IPEndPoint endPoint = new IPEndPoint(
                    IPAddress.Parse(user.IPAddress),
                    int.Parse(user.Port) 
                    );

                try
                {
                    byte[] bytes = Encoding.UTF8
                        .GetBytes(JsonConvert
                        .SerializeObject(viewModelGames
                        .Find(x => x.IdSnake == user.IdSnake)));

                    sender.Send(bytes, bytes.Length, endPoint);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Отправил данные пользователю: {user.IPAddress}:{user.Port}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Возникло исключение: " + ex.ToString() + "\n " + ex.Message);
                }
                finally
                {
                    sender.Close();
                }
            }
        }

        public static void Receiver()
        {
            UdpClient receivingUdpClient = new UdpClient(localPort);
            IPEndPoint RemoteIpEndPoint = null;

            try
            {
                Console.WriteLine("Команды сервера:");

                while (true)
                {
                    byte[] receiveBytes = receivingUdpClient.Receive(
                        ref RemoteIpEndPoint);

                    string returnData = Encoding.UTF8.GetString(receiveBytes);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Получил команду: " + returnData.ToString());

                    if (returnData.ToString().Contains("/start")) {
                        string[] dataMessage = returnData.ToString().Split('|');
                        ViewModelUserSettings viesModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Подключияся пользователь: (viewModelUserSettings.IPAddress): (viewModelUserSettings.Port)");

                        remoteIPAdress.Add(viesModelUserSettings);
                        viesModelUserSettings.IdSnake = AddSnake();
                        // связываем змею и игрока
                        viewModelGames[viesModelUserSettings.IdSnake].IdSnake = viesModelUserSettings.IdSnake;
                    }
                    else
                    {
                        string[] dataMessage = returnData.ToString().Split('|');
                        // Конвертируем данные в модель
                        ViewModelUserSettings vienModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        // Получаем ID игрока
                        int IdPlayer = -1;
                        // В случае если нёртвый игрок присылает команду
                        // Находим ID игрока, ица его в списке по IP адресу и Порту
                        IdPlayer = remoteIPAdress.FindIndex(x => x.IPAddress == vienModelUserSettings.IPAddress
                            && x.Port == vienModelUserSettings.Port);
                        // Если игрок найден
                        if (IdPlayer != -1)
                        {
                            // Если команда яверх, и если змен не ползйт иниз
                            if (dataMessage[0] == "Up" &&
                                viewModelGames[IdPlayer].SnakesPlayer.direction != Snakes.Direction.Down)

                                viewModelGames[IdPlayer].SnakesPlayer.direction = Snakes.Direction.Up;

                            else if (dataMessage[0] == "Down" &
                            viewModelGames[IdPlayer].SnakesPlayer.direction != Snakes.Direction.Up)
                                // Змее игрока указываем команду вверх
                                viewModelGames[IdPlayer].SnakesPlayer.direction = Snakes.Direction.Down;

                            else if (dataMessage[0] == "Left" &&
                                    viewModelGames[IdPlayer].SnakesPlayer.direction != Snakes.Direction.Right)
                                // Змее игрока указываем команду влево
                                viewModelGames[IdPlayer].SnakesPlayer.direction = Snakes.Direction.Left;
                            // Если конанда вправо и эмея не ползёт влево
                            else if (dataMessage[0] == "Right" &&
                                    viewModelGames[IdPlayer].SnakesPlayer.direction != Snakes.Direction.Left)
                                // Змее игрока указываем команду вправо
                                viewModelGames[IdPlayer].SnakesPlayer.direction = Snakes.Direction.Right;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red; 
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n" + ex.Message);
            }
        }

        public static int AddSnake()
        {
            ViewModelGames viewModelGamesPlayer = new ViewModelGames();

            viewModelGamesPlayer.SnakesPlayer = new Snakes()
            {
                points = new List<Snakes.Point>()
                {
                    new Snakes.Point() { X = 30, Y = 10},
                    new Snakes.Point() { X = 20, Y = 10},
                    new Snakes.Point() { X = 10, Y = 10},
                },

                direction = Snakes.Direction.Start,
            };

            viewModelGamesPlayer.Points = new Snakes.Point(new Random().Next(10, 783), new Random().Next(10, 410));

            return viewModelGames.FindIndex(x => x == viewModelGamesPlayer);
        }

        public static void Timer()
        {
            while (true)
            {
                Thread.Sleep(100);

                List<ViewModelGames> RemoteSnakes = viewModelGames.FindAll(x => x.SnakesPlayer.GameOver);

                if (RemoteSnakes.Count > 0)
                {
                    foreach (var deadSnake in RemoteSnakes)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Отключил пользователя: {remoteIPAdress.Find(x => x.IdSnake == deadSnake.IdSnake).IPAddress}" +
                            $"{remoteIPAdress.Find(x => x.IdSnake == deadSnake.IdSnake).Port}");

                        remoteIPAdress.RemoveAll(x => x.IdSnake == deadSnake.IdSnake);
                    }

                    viewModelGames.RemoveAll(x => x.SnakesPlayer.GameOver);

                    foreach (var user in remoteIPAdress)
                    {
                        Snakes snake = viewModelGames.Find(x => x.IdSnake == user.IdSnake).SnakesPlayer;

                        for (int i = snake.points.Count - 1; i >= 0; i--)
                        {
                            if (i != 0)
                                snake.points[i] = snake.points[i - 1];
                        }
                    }
                }
            }
        }
    }
}
